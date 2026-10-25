// Lien avec SteamVR de LunaQuest :
// - écran du PC dans le casque : une fenêtre SteamVR (overlay) qui affiche le bureau en direct,
//   affichée ou masquée par un double appui sur le bouton menu de la manette gauche ;
// - perfs en direct dans le casque, affichées ou masquées en enfonçant les deux joysticks en même temps ;
// - alertes (batterie faible) affichées quelques secondes devant les yeux ;
// - relevé en direct des appareils (casque, manettes, trackers) pour l'aperçu de la page Corps.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Valve.VR;

namespace LunaQuest
{
    class DesktopViewer
    {
        const string OverlayKey = "lunaquest.desktop";
        const string DashboardKey = "lunaquest.desktop.dashboard";
        const int MaxWidth = 2560;          // pleine résolution jusqu'au 1440p ; au-delà l'image est réduite pour rester fluide
        const float WidthMeters = 2.6f;     // grand écran : le texte doit couvrir assez de pixels du casque pour rester lisible
        const float DistanceMeters = 1.4f;
        const float Curvature = 0.15f;      // légèrement incurvé, les bords restent à la même distance des yeux
        const int DoublePressMs = 450;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetDllDirectory(string path);
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint flags, int dx, int dy, int data, UIntPtr extra);
        const uint LeftDown = 0x02, LeftUp = 0x04, RightDown = 0x08, RightUp = 0x10, MiddleDown = 0x20, MiddleUp = 0x40, Wheel = 0x0800;

        readonly string appDir;
        readonly Action<string> log;
        Thread thread;
        volatile bool running;

        // Ce que LunaQuest demande au lien : l'écran PC, l'aperçu des trackers, ou les deux.
        public volatile bool ScreenEnabled;
        public volatile bool MonitorEnabled;
        // Dernier relevé des appareils SteamVR (null tant que SteamVR n'est pas joint).
        public static volatile VrDevice[] Devices;
        DateTime lastSample = DateTime.MinValue;

        ulong overlay, dashboard, thumbnail;
        bool visible;
        DateTime lastToggle = DateTime.MinValue, lastMenuPress = DateTime.MinValue, dashboardOpened = DateTime.MinValue;
        // Avec ALVR, le bouton menu gauche est réservé au tableau de bord SteamVR : un double appui l'ouvre puis le referme
        // aussitôt. On reconnaît ce « ouvert puis refermé » rapide comme un double appui.
        const int DashboardFlickMs = 900;
        Bitmap frame;
        byte[] pixels;

        // Perfs et alertes : petites fenêtres qui suivent la tête.
        const string PerfKey = "lunaquest.perf", AlertKey = "lunaquest.alert";
        ulong perf, alert;
        bool perfVisible;
        DateTime perfDrawn = DateTime.MinValue, alertUntil = DateTime.MinValue, lastCombo = DateTime.MinValue;
        volatile string pendingAlert;
        // Raccourci des deux joysticks : actions SteamVR Input propres à LunaQuest (lues même pendant un jeu).
        ulong actionSet, actLeftStick, actRightStick;
        bool inputReady, inputWarned, leftDown, rightDown;

        const string ActionManifest = @"{
  ""default_bindings"": [ { ""controller_type"": ""oculus_touch"", ""binding_url"": ""bindings_oculus_touch.json"" } ],
  ""actions"": [
    { ""name"": ""/actions/lunaquest/in/left_stick"", ""type"": ""boolean"", ""requirement"": ""optional"" },
    { ""name"": ""/actions/lunaquest/in/right_stick"", ""type"": ""boolean"", ""requirement"": ""optional"" }
  ],
  ""action_sets"": [ { ""name"": ""/actions/lunaquest"", ""usage"": ""leftright"" } ],
  ""localization"": [ { ""language_tag"": ""fr_FR"", ""/actions/lunaquest"": ""LunaQuest"",
    ""/actions/lunaquest/in/left_stick"": ""Joystick gauche (perfs)"", ""/actions/lunaquest/in/right_stick"": ""Joystick droit (perfs)"" } ]
}";
        const string TouchBindings = @"{
  ""controller_type"": ""oculus_touch"",
  ""bindings"": { ""/actions/lunaquest"": { ""sources"": [
    { ""path"": ""/user/hand/left/input/joystick"", ""mode"": ""button"", ""inputs"": { ""click"": { ""output"": ""/actions/lunaquest/in/left_stick"" } } },
    { ""path"": ""/user/hand/right/input/joystick"", ""mode"": ""button"", ""inputs"": { ""click"": { ""output"": ""/actions/lunaquest/in/right_stick"" } } }
  ] } }
}";

        // Affiche un message quelques secondes dans le casque (appelable depuis n'importe quel fil).
        public void Flash(string text) { pendingAlert = text; }

        public DesktopViewer(string appDir, Action<string> log)
        {
            this.appDir = appDir;
            this.log = log;
        }

        public bool IsRunning { get { return running; } }

        public void Start()
        {
            if (running) return;
            running = true;
            thread = new Thread(Loop) { IsBackground = true, Name = "LunaQuest écran PC" };
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            if (thread != null && thread != Thread.CurrentThread) thread.Join(3000);
            thread = null;
        }

        // ---------------------------------------------------------------- Boucle principale

        void Loop()
        {
            if (!LoadOpenVr()) { running = false; return; }
            bool waitingLogged = false;
            while (running)
            {
                if (Process.GetProcessesByName("vrserver").Length == 0)
                {
                    Devices = null;
                    if (!waitingLogged && ScreenEnabled) { log("Écran PC : en attente de SteamVR…"); waitingLogged = true; }
                    Thread.Sleep(2000);
                    continue;
                }
                waitingLogged = false;
                // On attend un casque connecté et stable : s'enregistrer pendant que SteamVR démarre ou redémarre
                // peut bloquer le jeu lancé au même moment.
                if (!WaitForStableHmd()) continue;
                var err = EVRInitError.None;
                OpenVR.Init(ref err, EVRApplicationType.VRApplication_Overlay);
                if (err != EVRInitError.None) { Thread.Sleep(3000); continue; }
                try
                {
                    SetupInput();
                    if (!ScreenEnabled || CreateOverlays())
                    {
                        if (ScreenEnabled) log("Écran PC prêt : double appui sur le bouton menu gauche pour l'afficher.");
                        RunSession();
                    }
                }
                catch (Exception e) { log("Écran PC : erreur " + e.Message); Thread.Sleep(2000); }
                finally
                {
                    visible = perfVisible = false;
                    overlay = dashboard = thumbnail = perf = alert = 0;
                    alertUntil = DateTime.MinValue;
                    Devices = null;
                    try { OpenVR.Shutdown(); } catch { }
                }
            }
        }

        bool WaitForStableHmd()
        {
            int stableSeconds = 0;
            while (running && Process.GetProcessesByName("vrserver").Length > 0)
            {
                stableSeconds = OpenVR.IsHmdPresent() ? stableSeconds + 1 : 0;
                if (stableSeconds >= (ScreenEnabled ? 10 : 4)) return true;
                Thread.Sleep(1000);
            }
            return false;
        }

        bool LoadOpenVr()
        {
            // La bibliothèque openvr_api.dll est fournie avec SteamVR : on l'utilise directement.
            try
            {
                string vrpath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\openvr\openvrpaths.vrpath");
                var root = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(vrpath)) as Dictionary<string, object>;
                object runtimes;
                if (root != null && root.TryGetValue("runtime", out runtimes) && runtimes is object[])
                    foreach (var r in (object[])runtimes)
                    {
                        string bin = Path.Combine(r as string ?? "", @"bin\win64");
                        if (File.Exists(Path.Combine(bin, "openvr_api.dll"))) { SetDllDirectory(bin); return true; }
                    }
            }
            catch { }
            log("Écran PC : SteamVR introuvable (openvr_api.dll).");
            return false;
        }

        bool CreateOverlays()
        {
            var ov = OpenVR.Overlay;
            if (ov == null) return false;
            ov.FindOverlay(OverlayKey, ref overlay);
            if (overlay == OpenVR.k_ulOverlayHandleInvalid)
            {
                var e = ov.CreateOverlay(OverlayKey, "LunaQuest – Écran PC", ref overlay);
                if (e != EVROverlayError.None) { log("Écran PC : création impossible (" + e + ")."); return false; }
            }
            ov.SetOverlayWidthInMeters(overlay, WidthMeters);
            ov.SetOverlayCurvature(overlay, Curvature);
            // Pointeur laser : on vise avec la manette, la gâchette clique, le joystick fait défiler.
            ov.SetOverlayInputMethod(overlay, VROverlayInputMethod.Mouse);
            ov.SetOverlayFlag(overlay, VROverlayFlags.MakeOverlaysInteractiveIfVisible, true);
            ov.SetOverlayFlag(overlay, VROverlayFlags.SendVRDiscreteScrollEvents, true);
            ov.HideOverlay(overlay);

            // Entrée « Écran PC » dans le tableau de bord SteamVR, en secours.
            if (ov.CreateDashboardOverlay(DashboardKey, "Écran PC", ref dashboard, ref thumbnail) == EVROverlayError.None)
            {
                ov.SetOverlayWidthInMeters(dashboard, 2.5f);
                ov.SetOverlayInputMethod(dashboard, VROverlayInputMethod.Mouse);
                ov.SetOverlayFlag(dashboard, VROverlayFlags.SendVRDiscreteScrollEvents, true);
                using (var icon = new Bitmap(96, 96))
                {
                    using (var g = Graphics.FromImage(icon))
                    {
                        Theme.Smooth(g);
                        g.Clear(Theme.NightTop);
                        SpaceArt.Moon(g, new PointF(48, 48), 26);
                    }
                    Upload(thumbnail, icon);
                }
            }
            return true;
        }

        void RunSession()
        {
            var ev = new VREvent_t();
            uint evSize = (uint)Marshal.SizeOf(typeof(VREvent_t));
            var sw = new Stopwatch();

            while (running)
            {
                sw.Restart();
                while (OpenVR.System.PollNextEvent(ref ev, evSize))
                {
                    var type = (EVREventType)ev.eventType;
                    if (type == EVREventType.VREvent_Quit)
                    {
                        OpenVR.System.AcknowledgeQuit_Exiting();
                        return;
                    }
                    if (type == EVREventType.VREvent_DashboardActivated) dashboardOpened = DateTime.UtcNow;
                    if (type == EVREventType.VREvent_DashboardDeactivated)
                    {
                        if ((DateTime.UtcNow - dashboardOpened).TotalMilliseconds < DashboardFlickMs && ScreenEnabled) Toggle();
                        dashboardOpened = DateTime.MinValue;
                    }
                    // Casques où le bouton menu gauche n'est pas réservé au tableau de bord : double appui brut.
                    if (type == EVREventType.VREvent_ButtonPress && ev.data.controller.button == (uint)EVRButtonId.k_EButton_ApplicationMenu
                        && ev.trackedDeviceIndex == OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand))
                    {
                        var now = DateTime.UtcNow;
                        if ((now - lastMenuPress).TotalMilliseconds < DoublePressMs) { if (ScreenEnabled) Toggle(); lastMenuPress = DateTime.MinValue; }
                        else lastMenuPress = now;
                    }
                }

                // L'écran PC peut être activé ou coupé pendant que le lien tourne.
                if (ScreenEnabled && overlay == 0 && !CreateOverlays()) ScreenEnabled = false;
                if (!ScreenEnabled && overlay != 0)
                {
                    OpenVR.Overlay.DestroyOverlay(overlay);
                    if (dashboard != 0) OpenVR.Overlay.DestroyOverlay(dashboard);
                    overlay = dashboard = thumbnail = 0;
                    visible = false;
                }
                // Relevé rapide pour l'aperçu de la page Corps, lent (batteries) le reste du temps.
                double sinceSample = (DateTime.UtcNow - lastSample).TotalMilliseconds;
                if ((MonitorEnabled && sinceSample >= 30) || sinceSample >= 3000) { lastSample = DateTime.UtcNow; SampleDevices(); }
                PollSticks();
                UpdateHud();

                var ov = OpenVR.Overlay;
                if (visible) PumpMouse(overlay, ref ev, evSize);
                bool dashVisible = dashboard != 0 && ov.IsOverlayVisible(dashboard);
                if (dashVisible) PumpMouse(dashboard, ref ev, evSize);
                if (visible || dashVisible)
                {
                    Capture();
                    if (visible) Upload(overlay, frame);
                    if (dashVisible) Upload(dashboard, frame);
                }

                int wait = (MonitorEnabled ? 15 : visible || dashVisible ? 33 : 30) - (int)sw.ElapsedMilliseconds;
                if (wait > 0) Thread.Sleep(wait);
            }
            if (overlay != 0) OpenVR.Overlay.HideOverlay(overlay);
        }

        // ---------------------------------------------------------------- Perfs et alertes dans le casque

        void SetupInput()
        {
            inputReady = false;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LunaQuest", "input");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "actions.json"), ActionManifest);
                File.WriteAllText(Path.Combine(dir, "bindings_oculus_touch.json"), TouchBindings);
                var input = OpenVR.Input;
                if (input != null && input.SetActionManifestPath(Path.Combine(dir, "actions.json")) == EVRInputError.None)
                {
                    input.GetActionSetHandle("/actions/lunaquest", ref actionSet);
                    input.GetActionHandle("/actions/lunaquest/in/left_stick", ref actLeftStick);
                    input.GetActionHandle("/actions/lunaquest/in/right_stick", ref actRightStick);
                    inputReady = actionSet != 0 && actLeftStick != 0 && actRightStick != 0;
                }
            }
            catch { }
            if (!inputReady && !inputWarned) { log("Perfs dans le casque : raccourci des joysticks indisponible."); inputWarned = true; }
        }

        // Les deux joysticks enfoncés ensemble (le second dans la foulée du premier) : perfs affichées ou masquées.
        void PollSticks()
        {
            if (!inputReady) return;
            var sets = new[] { new VRActiveActionSet_t { ulActionSet = actionSet } };
            if (OpenVR.Input.UpdateActionState(sets, (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t))) != EVRInputError.None) return;
            var data = new InputDigitalActionData_t();
            uint size = (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t));
            bool l = OpenVR.Input.GetDigitalActionData(actLeftStick, ref data, size, 0) == EVRInputError.None && data.bState;
            bool r = OpenVR.Input.GetDigitalActionData(actRightStick, ref data, size, 0) == EVRInputError.None && data.bState;
            bool pressedNow = (l && !leftDown) || (r && !rightDown);
            leftDown = l;
            rightDown = r;
            if (l && r && pressedNow && (DateTime.UtcNow - lastCombo).TotalMilliseconds > 600)
            {
                lastCombo = DateTime.UtcNow;
                TogglePerf();
            }
        }

        // Petite fenêtre accrochée à la tête : x à droite, y en haut, z vers l'arrière (mètres).
        ulong EnsureHud(ref ulong handle, string key, string name, float width, float x, float y, float z)
        {
            if (handle != 0) return handle;
            var ov = OpenVR.Overlay;
            if (ov == null || ov.CreateOverlay(key, name, ref handle) != EVROverlayError.None) { handle = 0; return 0; }
            ov.SetOverlayWidthInMeters(handle, width);
            var t = new HmdMatrix34_t { m0 = 1, m5 = 1, m10 = 1, m3 = x, m7 = y, m11 = z };
            ov.SetOverlayTransformTrackedDeviceRelative(handle, OpenVR.k_unTrackedDeviceIndex_Hmd, ref t);
            return handle;
        }

        void TogglePerf()
        {
            perfVisible = !perfVisible;
            if (perfVisible)
            {
                if (EnsureHud(ref perf, PerfKey, "LunaQuest – Perfs", 0.3f, -0.16f, -0.12f, -0.75f) == 0) { perfVisible = false; return; }
                DrawPerf();
                OpenVR.Overlay.ShowOverlay(perf);
            }
            else if (perf != 0) OpenVR.Overlay.HideOverlay(perf);
        }

        void UpdateHud()
        {
            if (perfVisible && (DateTime.UtcNow - perfDrawn).TotalMilliseconds >= 500) DrawPerf();
            string message = pendingAlert;
            if (message != null)
            {
                pendingAlert = null;
                if (EnsureHud(ref alert, AlertKey, "LunaQuest – Alerte", 0.36f, 0f, 0.1f, -0.8f) != 0)
                {
                    using (var bmp = RenderAlert(message)) Upload(alert, bmp, true);
                    OpenVR.Overlay.ShowOverlay(alert);
                    alertUntil = DateTime.UtcNow.AddSeconds(6);
                }
            }
            if (alert != 0 && alertUntil != DateTime.MinValue && DateTime.UtcNow > alertUntil)
            {
                OpenVR.Overlay.HideOverlay(alert);
                alertUntil = DateTime.MinValue;
            }
        }

        void DrawPerf()
        {
            perfDrawn = DateTime.UtcNow;
            using (var bmp = RenderPerf(AlvrLink.Latest)) Upload(perf, bmp, true);
        }

        // Police des panneaux : avec les caractères chinois ou japonais si besoin.
        static string HudFont { get { return Lang.Code == "zh" ? "Microsoft YaHei UI" : Lang.Code == "ja" ? "Yu Gothic UI" : "Segoe UI"; } }

        static Bitmap HudPanel(int w, int h, Color edge, out Graphics g)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using (var path = Theme.Round(new RectangleF(2, 2, w - 4, h - 4), 24))
            {
                using (var b = new SolidBrush(Color.FromArgb(225, 6, 8, 24))) g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(200, edge), 3f)) g.DrawPath(pen, path);
            }
            return bmp;
        }

        static Bitmap RenderPerf(AlvrStats s)
        {
            Graphics g;
            var bmp = HudPanel(600, 330, Theme.Electric, out g);
            using (g)
            using (var title = new Font(HudFont, 24, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var label = new Font(HudFont, 26, GraphicsUnit.Pixel))
            using (var value = new Font(HudFont, 30, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var small = new Font(HudFont, 20, GraphicsUnit.Pixel))
            using (var soft = new SolidBrush(Theme.Soft))
            using (var dim = new SolidBrush(Theme.Dim))
            {
                using (var b = new SolidBrush(Theme.MapleSoft)) g.DrawString(Lang.T("LUNAQUEST · PERFS"), title, b, 28, 20);
                if (s == null)
                {
                    g.DrawString(Lang.T("En attente du flux d'ALVR…"), label, soft, 28, 140);
                }
                else
                {
                    Action<int, string, string, Color> row = (y, name, text, color) =>
                    {
                        g.DrawString(Lang.T(name), label, soft, 28, y);
                        using (var b = new SolidBrush(color)) g.DrawString(text, value, b, 230, y - 3);
                    };
                    row(70, "Images/s", s.ClientFps.ToString("0") + " / " + s.ServerFps.ToString("0"),
                        s.ServerFps > 0 && s.ClientFps < s.ServerFps * 0.95f ? Theme.Orange : Theme.Green);
                    row(118, "Latence", s.LatencyMs.ToString("0") + " ms",
                        s.LatencyMs < 60 ? Theme.Green : s.LatencyMs < 90 ? Theme.Orange : Theme.Maple);
                    g.DrawString(Lang.T("réseau ") + s.NetworkMs.ToString("0") + Lang.T(" · encodage ") + s.EncodeMs.ToString("0") + Lang.T(" · décodage ") + s.DecodeMs.ToString("0") + " ms",
                        small, dim, 230, 158);
                    row(196, "Débit", s.Mbps.ToString("0") + " Mbit/s", Theme.Text);
                    row(244, "Pertes", s.LostPerSec + " /s", s.LostPerSec == 0 ? Theme.Green : Theme.Maple);
                }
                g.DrawString(Lang.T("Les deux joysticks enfoncés : masquer"), small, dim, 28, 292);
            }
            return bmp;
        }

        static Bitmap RenderAlert(string text)
        {
            Graphics g;
            var bmp = HudPanel(720, 150, Theme.Maple, out g);
            using (g)
            using (var title = new Font(HudFont, 24, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var body = new Font(HudFont, 28, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var red = new SolidBrush(Theme.MapleSoft))
            using (var white = new SolidBrush(Theme.Text))
            {
                g.DrawString("LUNAQUEST", title, red, 28, 18);
                g.DrawString(Lang.T(text), body, white, new RectangleF(28, 56, 664, 84));
            }
            return bmp;
        }

        // ---------------------------------------------------------------- Relevé des appareils

        void SampleDevices()
        {
            var sys = OpenVR.System;
            if (sys == null) return;
            var poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            sys.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);
            var list = new List<VrDevice>();
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                var cls = sys.GetTrackedDeviceClass(i);
                if (cls == ETrackedDeviceClass.Invalid) continue;
                var d = new VrDevice { Index = i, Class = cls };
                d.Serial = GetString(sys, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                d.System = GetString(sys, i, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
                d.Type = GetString(sys, i, ETrackedDeviceProperty.Prop_RegisteredDeviceType_String);
                d.ControllerType = GetString(sys, i, ETrackedDeviceProperty.Prop_ControllerType_String);
                d.Connected = sys.IsTrackedDeviceConnected(i);
                d.Tracking = poses[i].bPoseIsValid && poses[i].eTrackingResult == ETrackingResult.Running_OK;
                d.Role = cls == ETrackedDeviceClass.Controller ? sys.GetControllerRoleForTrackedDeviceIndex(i) : ETrackedControllerRole.Invalid;
                var err = ETrackedPropertyError.TrackedProp_Success;
                if (sys.GetBoolTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_DeviceProvidesBatteryStatus_Bool, ref err))
                {
                    d.Battery = sys.GetFloatTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref err);
                    d.Charging = sys.GetBoolTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool, ref err);
                }
                var m = poses[i].mDeviceToAbsoluteTracking;
                d.X = m.m3; d.Y = m.m7; d.Z = m.m11;
                if (cls == ETrackedDeviceClass.HMD) { d.ForwardX = -m.m2; d.ForwardZ = -m.m10; }
                list.Add(d);
            }
            Devices = list.ToArray();
        }

        static string GetString(CVRSystem sys, uint index, ETrackedDeviceProperty prop)
        {
            var err = ETrackedPropertyError.TrackedProp_Success;
            var sb = new StringBuilder(128);
            sys.GetStringTrackedDeviceProperty(index, prop, sb, (uint)sb.Capacity, ref err);
            return err == ETrackedPropertyError.TrackedProp_Success ? sb.ToString() : "";
        }

        // ---------------------------------------------------------------- Souris

        // Le laser SteamVR envoie des positions dans l'image (origine en bas à gauche) : on déplace la vraie souris de Windows.
        void PumpMouse(ulong handle, ref VREvent_t ev, uint size)
        {
            while (OpenVR.Overlay.PollNextOverlayEvent(handle, ref ev, size))
            {
                var type = (EVREventType)ev.eventType;
                if (type == EVREventType.VREvent_MouseMove || type == EVREventType.VREvent_MouseButtonDown || type == EVREventType.VREvent_MouseButtonUp)
                    MoveCursor(ev.data.mouse.x, ev.data.mouse.y);
                if (type == EVREventType.VREvent_MouseButtonDown || type == EVREventType.VREvent_MouseButtonUp)
                {
                    bool down = type == EVREventType.VREvent_MouseButtonDown;
                    var button = (EVRMouseButton)ev.data.mouse.button;
                    if (button == EVRMouseButton.Left) mouse_event(down ? LeftDown : LeftUp, 0, 0, 0, UIntPtr.Zero);
                    else if (button == EVRMouseButton.Right) mouse_event(down ? RightDown : RightUp, 0, 0, 0, UIntPtr.Zero);
                    else if (button == EVRMouseButton.Middle) mouse_event(down ? MiddleDown : MiddleUp, 0, 0, 0, UIntPtr.Zero);
                }
                if (type == EVREventType.VREvent_ScrollDiscrete && ev.data.scroll.ydelta != 0)
                    mouse_event(Wheel, 0, 0, (int)(ev.data.scroll.ydelta * 120), UIntPtr.Zero);
            }
        }

        void MoveCursor(float x, float y)
        {
            var f = frame;
            if (f == null) return;
            Rectangle src = Screen.PrimaryScreen.Bounds;
            int sx = src.X + (int)(x / f.Width * src.Width);
            int sy = src.Y + (int)((f.Height - y) / f.Height * src.Height);
            SetCursorPos(Math.Max(src.Left, Math.Min(src.Right - 1, sx)), Math.Max(src.Top, Math.Min(src.Bottom - 1, sy)));
        }

        // ---------------------------------------------------------------- Affichage

        void Toggle()
        {
            if (overlay == 0 || OpenVR.Overlay == null) return;
            var now = DateTime.UtcNow;
            if ((now - lastToggle).TotalMilliseconds < 300) return; // évite un double déclenchement par les deux méthodes
            lastToggle = now;
            visible = !visible;
            if (visible)
            {
                PlaceInFront();
                Capture();
                Upload(overlay, frame);
                OpenVR.Overlay.ShowOverlay(overlay);
            }
            else OpenVR.Overlay.HideOverlay(overlay);
        }

        // Place la fenêtre droit devant le regard, à hauteur des yeux, face à l'utilisateur ; elle reste ensuite fixe dans la pièce.
        void PlaceInFront()
        {
            var poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);
            var m = poses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking;
            float fx = -m.m2, fz = -m.m10;
            float len = (float)Math.Sqrt(fx * fx + fz * fz);
            if (len < 0.001f) { fx = 0; fz = -1; len = 1; }
            fx /= len; fz /= len;
            // Axe Z de la fenêtre tourné vers l'utilisateur, X à sa droite.
            float zx = -fx, zz = -fz;
            var t = new HmdMatrix34_t
            {
                m0 = zz, m1 = 0, m2 = zx, m3 = m.m3 + fx * DistanceMeters,
                m4 = 0, m5 = 1, m6 = 0, m7 = m.m7 - 0.1f,
                m8 = -zx, m9 = 0, m10 = zz, m11 = m.m11 + fz * DistanceMeters
            };
            OpenVR.Overlay.SetOverlayTransformAbsolute(overlay, ETrackingUniverseOrigin.TrackingUniverseStanding, ref t);
        }

        void Capture()
        {
            Rectangle src = Screen.PrimaryScreen.Bounds;
            int w = Math.Min(src.Width, MaxWidth);
            int h = src.Height * w / src.Width;
            if (frame == null || frame.Width != w || frame.Height != h)
            {
                if (frame != null) frame.Dispose();
                frame = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            }
            using (var g = Graphics.FromImage(frame))
            {
                if (w == src.Width) g.CopyFromScreen(src.Location, Point.Empty, src.Size);
                else
                    using (var full = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
                    {
                        using (var fg = Graphics.FromImage(full)) fg.CopyFromScreen(src.Location, Point.Empty, src.Size);
                        g.InterpolationMode = InterpolationMode.Bilinear;
                        g.DrawImage(full, 0, 0, w, h);
                    }
                // Le curseur n'apparaît pas dans la capture : on le dessine nous-mêmes.
                var p = Cursor.Position;
                float k = (float)w / src.Width;
                var c = new PointF((p.X - src.X) * k, (p.Y - src.Y) * k);
                using (var b = new SolidBrush(Color.White)) using (var pen = new Pen(Color.Black, 1.5f))
                {
                    var arrow = new[] { c, new PointF(c.X, c.Y + 22), new PointF(c.X + 6, c.Y + 16), new PointF(c.X + 11, c.Y + 26), new PointF(c.X + 15, c.Y + 24), new PointF(c.X + 10, c.Y + 15), new PointF(c.X + 17, c.Y + 15) };
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillPolygon(b, arrow);
                    g.DrawPolygon(pen, arrow);
                }
            }
        }

        // Envoie une image à SteamVR (qui attend du RGBA, alors que Windows fournit du BGRA).
        // keepAlpha : garde la transparence (coins arrondis des perfs et alertes).
        void Upload(ulong handle, Bitmap bmp, bool keepAlpha = false)
        {
            if (handle == 0 || bmp == null) return;
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int size = bmp.Width * bmp.Height * 4;
                if (pixels == null || pixels.Length < size) pixels = new byte[size];
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * bmp.Width * 4, bmp.Width * 4);
                for (int i = 0; i < size; i += 4)
                {
                    byte bl = pixels[i];
                    pixels[i] = pixels[i + 2];
                    pixels[i + 2] = bl;
                    if (!keepAlpha) pixels[i + 3] = 255;
                }
                var pin = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try { OpenVR.Overlay.SetOverlayRaw(handle, pin.AddrOfPinnedObject(), (uint)bmp.Width, (uint)bmp.Height, 4); }
                finally { pin.Free(); }
                // Les positions du laser sont exprimées en pixels de cette image.
                var scale = new HmdVector2_t { v0 = bmp.Width, v1 = bmp.Height };
                OpenVR.Overlay.SetOverlayMouseScale(handle, ref scale);
            }
            finally { bmp.UnlockBits(data); }
        }
    }
}

namespace LunaQuest
{
    // Un appareil SteamVR relevé par le lien : identité, suivi, batterie et position (mètres, repère debout).
    class VrDevice
    {
        public uint Index;
        public ETrackedDeviceClass Class;
        public ETrackedControllerRole Role;
        public string Serial = "", System = "", Type = "", ControllerType = "";
        public bool Connected, Tracking, Charging;
        public float Battery = -1;
        public float X, Y, Z, ForwardX, ForwardZ;

        public bool IsVirtualTracker { get { return System.StartsWith("ALVR", StringComparison.OrdinalIgnoreCase) && Class == ETrackedDeviceClass.GenericTracker; } }

        static readonly Dictionary<string, string> Joints = new Dictionary<string, string>
        {
            { "chest", "Torse" }, { "waist", "Hanches" }, { "left_elbow", "Coude gauche" }, { "right_elbow", "Coude droit" },
            { "left_knee", "Genou gauche" }, { "right_knee", "Genou droit" }, { "left_foot", "Pied gauche" }, { "right_foot", "Pied droit" }
        };

        public string Label
        {
            get
            {
                switch (Class)
                {
                    case ETrackedDeviceClass.HMD: return "Casque";
                    case ETrackedDeviceClass.Controller:
                        return Role == ETrackedControllerRole.LeftHand ? "Manette gauche" : Role == ETrackedControllerRole.RightHand ? "Manette droite" : "Manette";
                    case ETrackedDeviceClass.TrackingReference: return "Base " + Serial;
                    case ETrackedDeviceClass.GenericTracker:
                        if (IsVirtualTracker)
                        {
                            string joint = Type.Substring(Type.LastIndexOf('/') + 1);
                            string name;
                            return (Joints.TryGetValue(joint, out name) ? name : joint) + " (virtuel)";
                        }
                        return "Tracker " + Serial;
                    default: return Serial;
                }
            }
        }
    }
}
