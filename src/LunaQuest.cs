// LunaQuest - PC VR en USB pour Meta Quest, propulsé par ALVR.
// Compiler avec build.ps1 (utilise le csc.exe fourni avec Windows, C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LunaQuest
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ================================================================ Thème

    static class Theme
    {
        public static readonly Color NightTop = Color.FromArgb(8, 13, 36);
        public static readonly Color NightBottom = Color.FromArgb(24, 40, 94);
        public static readonly Color Field = Color.FromArgb(11, 19, 50);
        public static readonly Color CardFill = Color.FromArgb(125, 13, 22, 58);
        public static readonly Color CardBorder = Color.FromArgb(60, 150, 182, 255);
        public static readonly Color Text = Color.FromArgb(242, 245, 255);
        public static readonly Color Soft = Color.FromArgb(162, 184, 236);
        public static readonly Color Dim = Color.FromArgb(112, 132, 188);
        public static readonly Color Accent = Color.FromArgb(76, 141, 255);
        public static readonly Color AccentLight = Color.FromArgb(132, 186, 255);
        public static readonly Color AccentDark = Color.FromArgb(44, 92, 212);
        public static readonly Color Green = Color.FromArgb(94, 227, 176);
        public static readonly Color Orange = Color.FromArgb(255, 196, 108);
        public static readonly Color Red = Color.FromArgb(255, 122, 133);

        public static readonly Font Title = new Font("Segoe UI Semibold", 26f);
        public static readonly Font Body = new Font("Segoe UI", 10f);
        public static readonly Font BodyBold = new Font("Segoe UI Semibold", 10.5f);
        public static readonly Font Small = new Font("Segoe UI", 9f);
        public static readonly Font CardTitle = new Font("Segoe UI Semibold", 9f);
        public static readonly Font Button = new Font("Segoe UI Semibold", 10f);
        public static readonly Font ButtonBig = new Font("Segoe UI Semibold", 14f);
        public static readonly Font StepTitle = new Font("Segoe UI Semibold", 11.5f);
        public static readonly Font Mono = new Font("Consolas", 9f);

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 1) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color Alpha(Color c, int a) { return Color.FromArgb(a, c); }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        public static void Glow(Graphics g, RectangleF r, Color c)
        {
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(r);
                using (var b = new PathGradientBrush(p))
                {
                    b.CenterColor = c;
                    b.SurroundColors = new[] { Color.FromArgb(0, c) };
                    g.FillPath(b, p);
                }
            }
        }
    }

    // ================================================================ Décor : ciel étoilé et lune

    static class SpaceArt
    {
        public static Bitmap Render(Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Theme.Smooth(g);
                var full = new Rectangle(Point.Empty, size);
                using (var b = new LinearGradientBrush(full, Theme.NightTop, Theme.NightBottom, 75f)) g.FillRectangle(b, full);

                // Nébuleuses très douces.
                Theme.Glow(g, new RectangleF(-220, size.Height - 330, 720, 520), Color.FromArgb(55, 70, 110, 230));
                Theme.Glow(g, new RectangleF(size.Width - 600, -200, 680, 480), Color.FromArgb(40, 120, 150, 255));
                Theme.Glow(g, new RectangleF(size.Width * 0.38f, size.Height * 0.42f, 540, 400), Color.FromArgb(22, 150, 120, 255));

                // Étoiles (graine fixe : le ciel est toujours le même).
                var rnd = new Random(7);
                for (int i = 0; i < 280; i++)
                {
                    float x = (float)rnd.NextDouble() * size.Width;
                    float y = (float)rnd.NextDouble() * size.Height;
                    float s = (float)(0.6 + Math.Pow(rnd.NextDouble(), 3) * 2.3);
                    int a = 80 + rnd.Next(175);
                    Color c = rnd.Next(4) == 0 ? Color.FromArgb(a, 176, 204, 255) : Color.FromArgb(a, 255, 255, 255);
                    using (var br = new SolidBrush(c)) g.FillEllipse(br, x - s / 2, y - s / 2, s, s);
                    if (s > 2.3f)
                    {
                        Theme.Glow(g, new RectangleF(x - s * 3, y - s * 3, s * 6, s * 6), Color.FromArgb(a / 3, 190, 215, 255));
                        using (var pen = new Pen(Color.FromArgb(a / 3, 210, 225, 255), 1f))
                        {
                            g.DrawLine(pen, x - s * 3, y, x + s * 3, y);
                            g.DrawLine(pen, x, y - s * 3, x, y + s * 3);
                        }
                    }
                }

                Moon(g, new PointF(size.Width - 128, 92), 52);
            }
            return bmp;
        }

        public static void Moon(Graphics g, PointF c, float r)
        {
            Theme.Glow(g, new RectangleF(c.X - r * 2.5f, c.Y - r * 2.5f, r * 5f, r * 5f), Color.FromArgb(75, 150, 186, 255));

            // Orbite inclinée, dessinée avant la lune pour passer derrière.
            var state = g.Save();
            g.TranslateTransform(c.X, c.Y);
            g.RotateTransform(-16);
            using (var pen = new Pen(Color.FromArgb(55, 170, 200, 255), 1.2f))
                g.DrawEllipse(pen, -r * 2.15f, -r * 0.5f, r * 4.3f, r * 1.0f);
            g.Restore(state);

            var body = new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r);
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(body);
                using (var b = new PathGradientBrush(p))
                {
                    b.CenterPoint = new PointF(c.X - r * 0.35f, c.Y - r * 0.35f);
                    b.CenterColor = Color.FromArgb(252, 253, 255);
                    b.SurroundColors = new[] { Color.FromArgb(176, 194, 232) };
                    g.FillPath(b, p);
                }

                var clip = g.Clip;
                g.SetClip(p, CombineMode.Intersect);
                Crater(g, c.X - r * 0.36f, c.Y + r * 0.12f, r * 0.27f);
                Crater(g, c.X + r * 0.26f, c.Y - r * 0.40f, r * 0.17f);
                Crater(g, c.X + r * 0.08f, c.Y + r * 0.46f, r * 0.14f);
                Crater(g, c.X - r * 0.06f, c.Y - r * 0.16f, r * 0.09f);
                Crater(g, c.X + r * 0.52f, c.Y + r * 0.10f, r * 0.11f);
                Crater(g, c.X - r * 0.55f, c.Y - r * 0.38f, r * 0.08f);
                // Ombre côté droit : effet de croissant.
                using (var sp = new GraphicsPath())
                {
                    sp.AddEllipse(c.X - r * 0.35f, c.Y - r * 1.3f, r * 2.6f, r * 2.6f);
                    using (var sb = new PathGradientBrush(sp))
                    {
                        sb.CenterColor = Color.FromArgb(165, 10, 18, 52);
                        sb.SurroundColors = new[] { Color.FromArgb(0, 10, 18, 52) };
                        sb.FocusScales = new PointF(0.55f, 0.55f);
                        g.FillPath(sb, sp);
                    }
                }
                g.Clip = clip;
            }
            using (var pen = new Pen(Color.FromArgb(70, 225, 235, 255), 1.2f)) g.DrawEllipse(pen, body);

            // Petit satellite sur l'avant de l'orbite.
            double ang = 200 * Math.PI / 180, tilt = -16 * Math.PI / 180;
            float ox = (float)(Math.Cos(ang) * r * 2.15), oy = (float)(Math.Sin(ang) * -r * 0.5) + r * 0.02f;
            float sx = c.X + (float)(ox * Math.Cos(tilt) - oy * Math.Sin(tilt));
            float sy = c.Y + (float)(ox * Math.Sin(tilt) + oy * Math.Cos(tilt));
            Theme.Glow(g, new RectangleF(sx - 9, sy - 9, 18, 18), Color.FromArgb(150, 170, 205, 255));
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, sx - 2.5f, sy - 2.5f, 5, 5);
        }

        static void Crater(Graphics g, float x, float y, float r)
        {
            using (var b = new SolidBrush(Color.FromArgb(40, 78, 98, 150))) g.FillEllipse(b, x - r, y - r, 2 * r, 2 * r);
            using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1f)) g.DrawArc(pen, x - r, y - r, 2 * r, 2 * r, 20, 130);
        }
    }

    // ================================================================ Contrôles dessinés à la main

    class SpaceControl : Control
    {
        protected bool Hover, Pressed;

        public SpaceControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Hover = false; Pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { Pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
    }

    class MoonButton : SpaceControl
    {
        readonly bool primary;
        bool danger;

        public MoonButton(string text, bool primary)
        {
            Text = text;
            this.primary = primary;
            Font = primary ? Theme.ButtonBig : Theme.Button;
        }

        // Variante rouge, pour une action qui arrête tout (Déconnecter).
        public bool Danger { get { return danger; } set { if (danger == value) return; danger = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            float radius = primary ? Height / 2f : 12f;
            using (var path = Theme.Round(r, radius))
            {
                if (primary)
                {
                    Color top = Theme.AccentLight, bottom = Theme.AccentDark;
                    if (danger) { top = Color.FromArgb(255, 140, 150); bottom = Color.FromArgb(196, 46, 72); }
                    if (!Enabled) { top = Theme.Alpha(top, 80); bottom = Theme.Alpha(bottom, 80); }
                    else if (Pressed) { top = danger ? Color.FromArgb(230, 90, 105) : Theme.Accent; bottom = danger ? Color.FromArgb(160, 30, 55) : Color.FromArgb(34, 72, 176); }
                    else if (Hover) { top = danger ? Color.FromArgb(255, 170, 178) : Color.FromArgb(160, 204, 255); bottom = danger ? Color.FromArgb(220, 60, 85) : Theme.Accent; }
                    using (var b = new LinearGradientBrush(r, top, bottom, 90f)) g.FillPath(b, path);
                    var gloss = new RectangleF(r.X + 3, r.Y + 2, r.Width - 6, r.Height * 0.46f);
                    using (var gp = Theme.Round(gloss, radius))
                    using (var gb = new LinearGradientBrush(gloss, Color.FromArgb(75, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                        g.FillPath(gb, gp);
                    using (var pen = new Pen(Color.FromArgb(Enabled ? 130 : 50, 205, 228, 255), 1f)) g.DrawPath(pen, path);
                }
                else
                {
                    int fill = !Enabled ? 16 : Pressed ? 85 : Hover ? 62 : 34;
                    using (var b = new SolidBrush(Color.FromArgb(fill, 120, 165, 255))) g.FillPath(b, path);
                    using (var pen = new Pen(Color.FromArgb(!Enabled ? 40 : Hover ? 170 : 100, 145, 185, 255), 1f)) g.DrawPath(pen, path);
                }
            }
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var tb = new SolidBrush(Enabled ? Theme.Text : Theme.Alpha(Theme.Text, 110)))
                g.DrawString(Text, Font, tb, new RectangleF(0, 0, Width, Height), sf);
        }
    }

    class ToggleSwitch : SpaceControl
    {
        bool on;
        public event EventHandler CheckedChanged;

        public ToggleSwitch(string text) { Text = text; Height = 28; }

        public bool Checked
        {
            get { return on; }
            set
            {
                if (on == value) return;
                on = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e) { if (Enabled) Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var track = new RectangleF(0.5f, (Height - 24) / 2f, 46, 24);
            int a = Enabled ? 255 : 110;
            using (var path = Theme.Round(track, 12))
            {
                if (on)
                    using (var b = new LinearGradientBrush(track, Theme.Alpha(Theme.AccentLight, a), Theme.Alpha(Theme.AccentDark, a), 90f)) g.FillPath(b, path);
                else
                    using (var b = new SolidBrush(Color.FromArgb(Hover ? 70 : 45, 140, 170, 230))) g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(on ? 120 : 90, 170, 200, 255), 1f)) g.DrawPath(pen, path);
            }
            float kx = on ? track.Right - 21 : track.X + 3;
            var knob = new RectangleF(kx, track.Y + 3, 18, 18);
            using (var b = new SolidBrush(Color.FromArgb(60, 0, 0, 30))) g.FillEllipse(b, knob.X, knob.Y + 1, 18, 18);
            using (var b = new SolidBrush(Theme.Alpha(Color.White, a))) g.FillEllipse(b, knob);
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
            using (var tb = new SolidBrush(Theme.Alpha(Theme.Text, a)))
                g.DrawString(Text, Font, tb, new RectangleF(58, 0, Width - 58, Height), sf);
        }
    }

    class Segmented : SpaceControl
    {
        public string[] Items;
        int selected = -1, hoverIndex = -1;
        public event EventHandler SelectionChanged;

        public Segmented(params string[] items) { Items = items; Height = 38; Font = Theme.Button; }

        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                if (selected == value) return;
                selected = value;
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
        }

        int IndexAt(int x)
        {
            int i = x * Items.Length / Math.Max(1, Width);
            return Math.Max(0, Math.Min(Items.Length - 1, i));
        }

        protected override void OnMouseMove(MouseEventArgs e) { int i = IndexAt(e.X); if (i != hoverIndex) { hoverIndex = i; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hoverIndex = -1; base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) SelectedIndex = IndexAt(e.X); base.OnMouseClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            int a = Enabled ? 255 : 110;
            var outer = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Theme.Round(outer, outer.Height / 2))
            {
                using (var b = new SolidBrush(Color.FromArgb(30, 120, 165, 255))) g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(80, 145, 185, 255), 1f)) g.DrawPath(pen, path);
            }
            float w = (Width - 6f) / Items.Length;
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    var seg = new RectangleF(3 + i * w, 3, w, Height - 6.5f);
                    using (var sp = Theme.Round(seg, seg.Height / 2))
                    {
                        if (i == selected)
                        {
                            using (var b = new LinearGradientBrush(seg, Theme.Alpha(Theme.AccentLight, a), Theme.Alpha(Theme.AccentDark, a), 90f)) g.FillPath(b, sp);
                            using (var pen = new Pen(Color.FromArgb(110, 205, 228, 255), 1f)) g.DrawPath(pen, sp);
                        }
                        else if (i == hoverIndex && Enabled)
                            using (var b = new SolidBrush(Color.FromArgb(34, 160, 195, 255))) g.FillPath(b, sp);
                    }
                    using (var tb = new SolidBrush(Theme.Alpha(i == selected ? Theme.Text : Theme.Soft, a)))
                        g.DrawString(Items[i], Font, tb, seg, sf);
                }
            }
        }
    }

    class MoonSlider : SpaceControl
    {
        int min, max = 100, step = 1, val;
        bool dragging;
        const int Thumb = 20;
        public event EventHandler ValueChanged;

        public MoonSlider(int min, int max, int step) { this.min = min; this.max = max; this.step = step; val = min; Height = 30; }

        public int Value
        {
            get { return val; }
            set
            {
                int v = Math.Max(min, Math.Min(max, (int)Math.Round((double)(value - min) / step) * step + min));
                if (v == val) return;
                val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        void SetFromX(int x)
        {
            float t = (x - Thumb / 2f) / Math.Max(1f, Width - Thumb);
            t = Math.Max(0, Math.Min(1, t));
            Value = (int)Math.Round(min + t * (max - min));
        }

        protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) { dragging = true; SetFromX(e.X); } base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (dragging) SetFromX(e.X); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            int a = Enabled ? 255 : 110;
            float cy = Height / 2f;
            var track = new RectangleF(Thumb / 2f, cy - 3, Width - Thumb, 6);
            using (var p = Theme.Round(track, 3)) using (var b = new SolidBrush(Color.FromArgb(50, 150, 180, 255))) g.FillPath(b, p);
            float t = max > min ? (float)(val - min) / (max - min) : 0;
            float x = track.X + t * track.Width;
            if (x - track.X > 1)
            {
                var fill = new RectangleF(track.X, track.Y, x - track.X, track.Height);
                using (var p = Theme.Round(fill, 3))
                using (var b = new LinearGradientBrush(new RectangleF(track.X, track.Y, track.Width, track.Height), Theme.Alpha(Theme.AccentDark, a), Theme.Alpha(Theme.AccentLight, a), 0f))
                    g.FillPath(b, p);
            }
            if (Hover || dragging) Theme.Glow(g, new RectangleF(x - 15, cy - 15, 30, 30), Color.FromArgb(90, 120, 170, 255));
            using (var b = new SolidBrush(Theme.Alpha(Color.White, a))) g.FillEllipse(b, x - 9, cy - 9, 18, 18);
            using (var pen = new Pen(Theme.Alpha(Theme.Accent, a), 2f)) g.DrawEllipse(pen, x - 9, cy - 9, 18, 18);
        }
    }

    class StatusDot : SpaceControl
    {
        Color dot = Theme.Dim;

        public StatusDot() { Size = new Size(20, 20); Cursor = Cursors.Default; }

        public Color DotColor { get { return dot; } set { if (dot == value) return; dot = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            Theme.Glow(g, new RectangleF(0, 0, Width, Height), Theme.Alpha(dot, 120));
            float d = 9;
            using (var b = new SolidBrush(dot)) g.FillEllipse(b, (Width - d) / 2, (Height - d) / 2, d, d);
        }
    }

    // ================================================================ Config de LunaQuest

    class Config
    {
        readonly string path;
        public string AlvrFolder = "";
        public bool AutoConnect = true;
        public bool KeepAwake = true;
        public bool DesktopViewer = true;

        public Config(string path)
        {
            this.path = path;
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path))
            {
                int i = line.IndexOf('=');
                if (i < 0) continue;
                string key = line.Substring(0, i).Trim(), val = line.Substring(i + 1).Trim();
                if (key == "alvr") AlvrFolder = val;
                if (key == "autoconnect") AutoConnect = val == "1";
                if (key == "keepawake") KeepAwake = val == "1";
                if (key == "desktop") DesktopViewer = val == "1";
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllLines(path, new[] {
                    "alvr=" + AlvrFolder,
                    "autoconnect=" + (AutoConnect ? "1" : "0"),
                    "keepawake=" + (KeepAwake ? "1" : "0"),
                    "desktop=" + (DesktopViewer ? "1" : "0")
                });
            }
            catch { }
        }
    }

    // ================================================================ ADB

    class CmdResult
    {
        public int Code; public string Out; public string Err;
        public bool Ok { get { return Code == 0; } }
        public string All { get { return (Out + "\n" + Err).Trim(); } }
    }

    static class Adb
    {
        public static string Path;

        static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string LocalDir = System.IO.Path.Combine(AppDir, "platform-tools");

        public static string Find()
        {
            var candidates = new List<string>();
            candidates.Add(System.IO.Path.Combine(LocalDir, "adb.exe"));
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                if (dir.Trim().Length > 0) candidates.Add(System.IO.Path.Combine(dir.Trim(), "adb.exe"));
            candidates.Add(Environment.ExpandEnvironmentVariables(@"%AppData%\SideQuest\platform-tools\adb.exe"));
            candidates.Add(Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Android\Sdk\platform-tools\adb.exe"));
            foreach (var c in candidates)
            {
                try { if (File.Exists(c)) return c; } catch { }
            }
            return null;
        }

        public static CmdResult Run(string args, int timeoutMs = 8000)
        {
            if (Path == null) return new CmdResult { Code = -1, Out = "", Err = "ADB introuvable" };
            var psi = new ProcessStartInfo(Path, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    var outTask = p.StandardOutput.ReadToEndAsync();
                    var errTask = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        return new CmdResult { Code = -1, Out = "", Err = "délai dépassé" };
                    }
                    // Le démon adb lancé au premier appel peut garder les flux ouverts : on n'attend pas indéfiniment.
                    Task.WaitAll(new Task[] { outTask, errTask }, 2000);
                    return new CmdResult
                    {
                        Code = p.ExitCode,
                        Out = outTask.IsCompleted ? outTask.Result : "",
                        Err = errTask.IsCompleted ? errTask.Result : ""
                    };
                }
            }
            catch (Exception e)
            {
                return new CmdResult { Code = -1, Out = "", Err = e.Message };
            }
        }
    }

    // ================================================================ ALVR : config (session.json) et driver SteamVR

    class AlvrSession
    {
        public readonly string FilePath;
        Dictionary<string, object> root;

        AlvrSession(string path) { FilePath = path; }

        static JavaScriptSerializer Json()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 512 };
        }

        public static AlvrSession Load(string path)
        {
            var s = new AlvrSession(path);
            s.root = Json().DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
            if (s.root == null) throw new InvalidDataException("session.json illisible");
            return s;
        }

        public void Save()
        {
            string json = Json().Serialize(root);
            Json().DeserializeObject(json); // vérifie que le résultat se relit avant d'écraser le fichier
            File.Copy(FilePath, FilePath + ".lunaquest.bak", true);
            File.WriteAllText(FilePath, json, new UTF8Encoding(false));
        }

        object Get(string path)
        {
            object cur = root;
            foreach (var part in path.Split('.'))
            {
                var d = cur as Dictionary<string, object>;
                if (d == null || !d.TryGetValue(part, out cur)) return null;
            }
            return cur;
        }

        void Set(string path, object value)
        {
            var parts = path.Split('.');
            var d = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                object next;
                if (!d.TryGetValue(parts[i], out next) || !(next is Dictionary<string, object>))
                    throw new InvalidOperationException("Réglage ALVR introuvable : " + path);
                d = (Dictionary<string, object>)next;
            }
            d[parts[parts.Length - 1]] = value;
        }

        bool GetBool(string path) { var o = Get(path); return o is bool && (bool)o; }
        string GetString(string path) { return Get(path) as string ?? ""; }
        double GetNumber(string path, double fallback)
        {
            var o = Get(path);
            if (o == null || o is bool || o is string) return fallback;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); } catch { return fallback; }
        }

        const string S = "session_settings.";

        public double Fps
        {
            get { return GetNumber(S + "video.preferred_fps", 72); }
            set { Set(S + "video.preferred_fps", decimal.Parse(value.ToString("0.0", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)); }
        }

        public int BitrateMbps
        {
            get { return (int)GetNumber(S + "video.bitrate.mode.ConstantMbps", 100); }
            set { Set(S + "video.bitrate.mode.variant", "ConstantMbps"); Set(S + "video.bitrate.mode.ConstantMbps", value); }
        }

        public string Codec
        {
            get { return GetString(S + "video.preferred_codec.variant"); }
            set { Set(S + "video.preferred_codec.variant", value); }
        }

        public bool GameAudio
        {
            get { return GetBool(S + "audio.game_audio.enabled"); }
            set { Set(S + "audio.game_audio.enabled", value); }
        }

        public bool Microphone
        {
            get { return GetBool(S + "audio.microphone.enabled"); }
            set
            {
                Set(S + "audio.microphone.enabled", value);
                if (!value) return;
                // Les VB-CABLE récents ajoutent « CABLE In 16ch » : en automatique ALVR peut le choisir,
                // et le casque refuse alors d'enregistrer sur 16 canaux, ce qui bloque la connexion.
                const string D = S + "audio.microphone.content.devices.";
                Set(D + "variant", "Custom");
                Set(D + "Custom.sink.variant", "NameSubstring");
                Set(D + "Custom.sink.NameSubstring", "CABLE Input");
                Set(D + "Custom.source.variant", "NameSubstring");
                Set(D + "Custom.source.NameSubstring", "CABLE Output");
            }
        }

        public bool HandSkeleton
        {
            get { return GetBool(S + "headset.controllers.content.hand_skeleton.enabled"); }
            set { Set(S + "headset.controllers.content.hand_skeleton.enabled", value); }
        }

        public string Protocol
        {
            get { return GetString(S + "connection.stream_protocol.variant"); }
            set { Set(S + "connection.stream_protocol.variant", value); }
        }

        // Largeur par œil en pixels (la hauteur suit les proportions du casque), 0 si ALVR est en mode « Scale ».
        // « Scale 1.0 » vaut la résolution recommandée par Meta (≈ 1680 px sur Quest 3), bien moins que l'écran.
        public int EyeWidth
        {
            get
            {
                if (GetString(S + "video.transcoding_view_resolution.variant") != "Absolute") return 0;
                return (int)GetNumber(S + "video.transcoding_view_resolution.Absolute.width", 0);
            }
            set
            {
                foreach (var key in new[] { "transcoding_view_resolution", "emulated_headset_view_resolution" })
                {
                    Set(S + "video." + key + ".variant", "Absolute");
                    Set(S + "video." + key + ".Absolute.width", value);
                    Set(S + "video." + key + ".Absolute.height.set", false);
                }
            }
        }

        // Mémoire de zone : sans recentrage, ALVR prend le guardian du Quest comme repère fixe, donc la calibration
        // des trackers (Space Calibrator) reste valable d'une session à l'autre. Avec recentrage, la pièce suit la tête.
        public bool ZoneMemory
        {
            get
            {
                return GetString(S + "headset.position_recentering_mode.variant") == "Disabled"
                    && GetString(S + "headset.rotation_recentering_mode.variant") == "Disabled";
            }
            set
            {
                Set(S + "headset.position_recentering_mode.variant", value ? "Disabled" : "LocalFloor");
                Set(S + "headset.rotation_recentering_mode.variant", value ? "Disabled" : "Yaw");
            }
        }

        // La fovéation envoie les bords de l'image 4 à 5 fois moins détaillés : plus léger, mais l'image paraît baveuse.
        public bool Foveation
        {
            get { return GetBool(S + "video.foveated_encoding.enabled"); }
            set { Set(S + "video.foveated_encoding.enabled", value); }
        }

        // La quantification adaptative « Spatial » de NVENC (défaut d'ALVR) retire des bits aux zones chargées
        // de détails : ce sont justement les objets lointains, qui deviennent flous alors que le proche reste net.
        public string NvencAdaptiveQuantization
        {
            get { return GetString(S + "video.encoder_config.nvenc.adaptive_quantization_mode.variant"); }
            set { Set(S + "video.encoder_config.nvenc.adaptive_quantization_mode.variant", value); }
        }

        // Largeur de l'image envoyée à l'encodeur : les deux yeux côte à côte, réduite par la fovéation.
        public int EncodedWidth
        {
            get
            {
                int eye = (EyeWidth + 31) / 32 * 32;
                if (eye == 0) return 0;
                double ratio = 1;
                if (Foveation)
                {
                    double center = GetNumber(S + "video.foveated_encoding.content.center_size_x", 0.6);
                    double edge = Math.Max(1, GetNumber(S + "video.foveated_encoding.content.edge_ratio_x", 2));
                    ratio = center + (1 - center) / edge;
                }
                return 2 * (int)Math.Ceiling(eye * ratio);
            }
        }

        // NVENC refuse le H264 au-delà de 4096 px de large : ALVR bascule alors en douce sur un encodeur
        // logiciel en mode « ultrafast », et l'image devient toute pixelisée. Le HEVC monte à 8192 px.
        public const int H264MaxWidth = 4096;
        public bool FixCodecForWidth()
        {
            if (Codec != "H264" || EncodedWidth <= H264MaxWidth) return false;
            Codec = "Hevc";
            return true;
        }

        Dictionary<string, object> Clients
        {
            get
            {
                var c = Get("client_connections") as Dictionary<string, object>;
                if (c == null) { c = new Dictionary<string, object>(); root["client_connections"] = c; }
                return c;
            }
        }

        static bool HasLocalIp(object entry)
        {
            var d = entry as Dictionary<string, object>;
            object ips;
            if (d == null || !d.TryGetValue("manual_ips", out ips) || !(ips is object[])) return false;
            return Array.IndexOf((object[])ips, "127.0.0.1") >= 0;
        }

        // Casques configurés pour passer par le câble (adresse 127.0.0.1).
        public List<string> WiredClients
        {
            get
            {
                var list = new List<string>();
                foreach (var kv in Clients) if (HasLocalIp(kv.Value)) list.Add(kv.Key);
                return list;
            }
        }

        // Plusieurs casques peuvent pointer sur 127.0.0.1 (ALVR officiel et LunaQuest Client) : ALVR essaierait
        // alors les deux et couperait en boucle. On ne garde « de confiance » que celui de l'appli lancée.
        // Renvoie true si quelque chose a changé.
        public bool SetActiveWiredClient(string hostname)
        {
            bool changed = false;
            foreach (var kv in Clients)
            {
                var entry = kv.Value as Dictionary<string, object>;
                if (entry == null || !HasLocalIp(entry)) continue;
                bool trusted = kv.Key == hostname;
                object current;
                if (!entry.TryGetValue("trusted", out current) || !(current is bool) || (bool)current != trusted)
                {
                    entry["trusted"] = trusted;
                    changed = true;
                }
            }
            return changed;
        }

        public void AddWiredClient(string hostname)
        {
            object o;
            Dictionary<string, object> entry;
            if (Clients.TryGetValue(hostname, out o) && o is Dictionary<string, object>)
                entry = (Dictionary<string, object>)o;
            else
            {
                entry = new Dictionary<string, object>();
                entry["display_name"] = "Unknown";
                entry["current_ip"] = null;
                entry["manual_ips"] = new object[0];
                entry["trusted"] = true;
                entry["connection_state"] = "Disconnected";
                Clients[hostname] = entry;
            }
            var ips = new List<object>();
            object current;
            if (entry.TryGetValue("manual_ips", out current) && current is object[]) ips.AddRange((object[])current);
            if (!ips.Contains("127.0.0.1")) ips.Add("127.0.0.1");
            entry["manual_ips"] = ips.ToArray();
            entry["trusted"] = true;
        }
    }

    static class AlvrInstall
    {
        static readonly string VrPathFile = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\openvr\openvrpaths.vrpath");

        public static bool IsValidFolder(string folder)
        {
            return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "driver.vrdrivermanifest"));
        }

        public static string SessionPath(string folder) { return Path.Combine(folder, "session.json"); }

        public static string DashboardPath(string folder) { return Path.Combine(folder, "ALVR Dashboard.exe"); }

        public static string Find()
        {
            var roots = new List<string> {
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\ALVR"),
                Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Programs\ALVR"),
                Environment.ExpandEnvironmentVariables(@"%LocalAppData%\ALVR")
            };
            foreach (var r in roots) if (IsValidFolder(r)) return r;

            // Version portable : dossier « alvr* » dans les emplacements habituels (2 niveaux).
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (var place in new[] { "Documents", "Downloads", "Desktop", "Téléchargements" })
            {
                try
                {
                    var dir = Path.Combine(profile, place);
                    if (!Directory.Exists(dir)) continue;
                    foreach (var sub in Directory.GetDirectories(dir, "*alvr*"))
                    {
                        if (IsValidFolder(sub)) return sub;
                        foreach (var sub2 in Directory.GetDirectories(sub))
                            if (IsValidFolder(sub2)) return sub2;
                    }
                }
                catch { }
            }
            return null;
        }

        static string Norm(string p)
        {
            try { return Path.GetFullPath(p).TrimEnd('\\', '/').ToLowerInvariant(); } catch { return p.ToLowerInvariant(); }
        }

        static Dictionary<string, object> ReadVrPaths()
        {
            if (!File.Exists(VrPathFile)) return null;
            return new JavaScriptSerializer().DeserializeObject(File.ReadAllText(VrPathFile)) as Dictionary<string, object>;
        }

        public static bool IsRegistered(string folder)
        {
            try
            {
                var d = ReadVrPaths();
                object list;
                if (d == null || !d.TryGetValue("external_drivers", out list) || !(list is object[])) return false;
                foreach (var item in (object[])list)
                    if (item is string && Norm((string)item) == Norm(folder)) return true;
            }
            catch { }
            return false;
        }

        // Ajoute ALVR aux drivers SteamVR (à faire SteamVR fermé, sinon il réécrit le fichier).
        public static void Register(string folder)
        {
            var d = ReadVrPaths();
            if (d == null) throw new FileNotFoundException("SteamVR n'a jamais été lancé sur ce PC (openvrpaths.vrpath absent).");
            var drivers = new List<object>();
            object list;
            if (d.TryGetValue("external_drivers", out list) && list is object[]) drivers.AddRange((object[])list);
            drivers.Add(folder);
            d["external_drivers"] = drivers.ToArray();
            File.Copy(VrPathFile, VrPathFile + ".lunaquest.bak", true);
            File.WriteAllText(VrPathFile, new JavaScriptSerializer().Serialize(d), new UTF8Encoding(false));
        }
    }

    // ================================================================ État

    class Snapshot
    {
        public bool AdbFound;
        public string Serial;      // null si aucun casque
        public string State;       // device / unauthorized / offline
        public string Model;
        public int Battery = -1;
        public string AlvrPackage; // null si ALVR non installé sur le casque
        public bool Forwarded;
        public bool ClientRunning; // appli ALVR ouverte dans le casque
        public bool SteamVrRunning;
        public string AlvrFolder;  // null si introuvable
        public bool SessionFound;
        public bool DriverRegistered;
        public bool HasWiredClient;
        public bool Ready { get { return Serial != null && State == "device"; } }
    }

    // Réglages saisis dans l'interface, appliqués sur une session fraîchement relue.
    class AlvrValues
    {
        public double Fps;
        public int Bitrate;
        public string Codec;
        public bool GameAudio, Mic, Skeleton, Foveation, ZoneMemory;
        public int EyeWidth; // 0 : on ne touche pas à la résolution
        public string NewClient;

        public void ApplyTo(AlvrSession s)
        {
            s.ZoneMemory = ZoneMemory;
            if (EyeWidth > 0) s.EyeWidth = EyeWidth;
            s.Fps = Fps;
            s.BitrateMbps = Bitrate;
            s.Codec = Codec;
            s.GameAudio = GameAudio;
            s.Microphone = Mic;
            s.HandSkeleton = Skeleton;
            s.Foveation = Foveation;
            s.Protocol = "Tcp"; // seul protocole qui passe par le tunnel USB
            s.NvencAdaptiveQuantization = "Disabled";
            if (!string.IsNullOrEmpty(NewClient)) s.AddWiredClient(NewClient);
        }
    }

    // Résolution de rendu de SteamVR : en automatique, il la baisse en douce (jusqu'à moitié moins de pixels).
    static class SteamVrConfig
    {
        static string FilePath()
        {
            string steam = null;
            try { steam = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string; } catch { }
            if (string.IsNullOrEmpty(steam)) steam = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Steam");
            return Path.Combine(steam.Replace('/', '\\'), @"config\steamvr.vrsettings");
        }

        // Fixe SteamVR à 100 % pour qu'il rende exactement la résolution choisie dans ALVR (SteamVR doit être fermé).
        public static void LockRenderScale()
        {
            string path = FilePath();
            if (!File.Exists(path)) return;
            var json = new JavaScriptSerializer();
            var root = json.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
            if (root == null) return;
            object o;
            var steamvr = root.TryGetValue("steamvr", out o) ? o as Dictionary<string, object> : null;
            if (steamvr == null) { steamvr = new Dictionary<string, object>(); root["steamvr"] = steamvr; }
            steamvr["supersampleManualOverride"] = true;
            steamvr["supersampleScale"] = 1.0m;
            File.Copy(path, path + ".lunaquest.bak", true);
            File.WriteAllText(path, json.Serialize(root), new UTF8Encoding(false));
        }
    }

    // Préréglages créés par l'utilisateur, une ligne par préréglage : nom=fps;débit;codec;son;micro;mains;fovéation;largeur
    class PresetStore
    {
        readonly string path;
        readonly SortedDictionary<string, AlvrValues> items = new SortedDictionary<string, AlvrValues>(StringComparer.CurrentCultureIgnoreCase);

        public PresetStore(string path)
        {
            this.path = path;
            if (!File.Exists(path))
            {
                // Préréglages fournis au premier lancement : chacun peut les modifier ou les supprimer ensuite.
                items["Beat Saber"] = new AlvrValues { Fps = 120, Bitrate = 70, Codec = "H264", EyeWidth = 2064, GameAudio = true, Mic = true, Skeleton = true, Foveation = true };
                items["VRChat"] = new AlvrValues { Fps = 90, Bitrate = 125, Codec = "Hevc", EyeWidth = 2448, GameAudio = true, Mic = true, Skeleton = true, Foveation = false };
                Write();
                return;
            }
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                int i = line.LastIndexOf('=');
                if (i <= 0) continue;
                var f = line.Substring(i + 1).Split(';');
                if (f.Length < 7) continue;
                double fps; int bitrate;
                if (!double.TryParse(f[0], NumberStyles.Float, CultureInfo.InvariantCulture, out fps) || !int.TryParse(f[1], out bitrate)) continue;
                items[line.Substring(0, i)] = new AlvrValues
                {
                    Fps = fps, Bitrate = bitrate, Codec = f[2],
                    GameAudio = f[3] == "1", Mic = f[4] == "1", Skeleton = f[5] == "1", Foveation = f[6] == "1"
                };
                int width;
                if (f.Length >= 8 && int.TryParse(f[7], out width)) items[line.Substring(0, i)].EyeWidth = width;
            }
        }

        public IEnumerable<string> Names { get { return items.Keys; } }

        public AlvrValues Get(string name)
        {
            AlvrValues v;
            return items.TryGetValue(name ?? "", out v) ? v : null;
        }

        public void Save(string name, AlvrValues v)
        {
            items[name.Replace("=", "-").Replace("\r", " ").Replace("\n", " ")] = v;
            Write();
        }

        public void Delete(string name) { items.Remove(name); Write(); }

        void Write()
        {
            var lines = new List<string>();
            foreach (var kv in items)
            {
                var v = kv.Value;
                lines.Add(kv.Key + "=" + string.Join(";", new[] {
                    v.Fps.ToString(CultureInfo.InvariantCulture), v.Bitrate.ToString(CultureInfo.InvariantCulture), v.Codec,
                    v.GameAudio ? "1" : "0", v.Mic ? "1" : "0", v.Skeleton ? "1" : "0", v.Foveation ? "1" : "0",
                    v.EyeWidth.ToString(CultureInfo.InvariantCulture) }));
            }
            try { File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false)); } catch { }
        }
    }

    // ================================================================ Fenêtre

    class MainForm : Form
    {
        const int AlvrControlPort = 9943;
        const int AlvrStreamPort = 9944;
        // Client ALVR au thème LunaQuest (compilé à part) : il s'installe à côté de l'ALVR officiel.
        const string LunaClientPackage = "alvr.client.lunaquest";
        const string LunaClientHostname = "lunaquest.client";
        // L'USB 2.0 plafonne vers 35-40 Mo/s en pratique ; au-dessus, le casque est en USB 3.
        const double UsbThreeMBps = 45;
        static readonly string[] SteamVrProcesses = { "vrserver", "vrmonitor", "vrcompositor", "vrdashboard", "vrwebhelper", "vrstartup", "ALVR Dashboard" };
        static readonly int[] FpsChoices = { 72, 80, 90, 120 };
        static readonly string[] CodecValues = { "H264", "Hevc", "AV1" };
        // Largeur par œil : recommandé Meta, écran du Quest 3 (2064), puis suréchantillonnage. 3840 est le maximum :
        // deux yeux côte à côte font 7680 px de large, la plus grande vidéo (8K) que le décodeur du casque accepte.
        static readonly int[] EyeWidths = { 1680, 2064, 2448, 2880, 3264, 3840 };

        readonly string appDir = AppDomain.CurrentDomain.BaseDirectory;
        readonly Config config;
        readonly object cacheLock = new object();
        readonly Dictionary<string, string> modelCache = new Dictionary<string, string>();
        readonly Dictionary<string, string> packageCache = new Dictionary<string, string>();
        DateTime sessionStamp;
        bool sessionHasClient;

        class Card
        {
            public Rectangle Bounds; public string Title;
            public Card(int x, int y, int w, int h, string title) { Bounds = new Rectangle(x, y, w, h); Title = title; }
        }

        readonly List<Control>[] pages = { new List<Control>(), new List<Control>(), new List<Control>() };
        readonly List<Card>[] cards = { new List<Card>(), new List<Card>(), new List<Card>() };
        readonly List<Control> fields = new List<Control>();
        int page;
        Bitmap space;

        // Connexion
        Segmented tabs;
        readonly StatusDot[] dots = new StatusDot[6];
        readonly Label[] values = new Label[6];
        Label battery;
        MoonButton btnConnect, btnAdb, btnApk, btnFolder, btnRestartVr;
        bool connectedState; // casque branché, tunnel USB ouvert et SteamVR lancé : le gros bouton devient « Déconnecter »
        bool clientWasRunning; // appli ALVR / LunaQuest Client ouverte dans le casque au dernier relevé
        ToggleSwitch tglAuto, tglAwake, tglDesktop;
        DesktopViewer desktopViewer;
        TextBox log;

        // Réglages ALVR
        Segmented segFps, segCodec, segRes;
        MoonSlider sldBitrate;
        Label lblBitrate, lblMicHint, lblClients, lblSettingsStatus;
        ToggleSwitch tglGameAudio, tglMic, tglSkeleton;
        TextBox txtHostname;
        MoonButton btnSave, btnOpenAlvr, btnPresetLoad, btnPresetSave, btnPresetDelete;
        ToggleSwitch tglFoveation, tglZone;
        ComboBox cmbPresets;
        PresetStore presets;
        bool loadingSettings, settingsDirty;
        bool vbCable;

        Timer timer;
        bool polling, busy;
        Snapshot last = new Snapshot();
        string autoConnectedSerial;
        double usbMBps; // vitesse du câble mesurée à la dernière connexion, 0 si inconnue

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public MainForm()
        {
            config = new Config(Path.Combine(appDir, "LunaQuest.ini"));
            desktopViewer = new DesktopViewer(appDir, Log);

            Text = "LunaQuest";
            ClientSize = new Size(960, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.NightTop;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            space = SpaceArt.Render(ClientSize);
            BuildHeader();
            BuildConnectionPage();
            BuildSettingsPage();
            BuildGuidePage();
            ShowPage(0);

            if (!AlvrInstall.IsValidFolder(config.AlvrFolder))
            {
                config.AlvrFolder = AlvrInstall.Find() ?? "";
                config.Save();
            }

            Adb.Path = Adb.Find();
            Log(Adb.Path != null ? "ADB trouvé : " + Adb.Path : "ADB introuvable : clique sur « Installer ADB ».");
            Log(config.AlvrFolder != "" ? "ALVR trouvé : " + config.AlvrFolder : "ALVR introuvable : clique sur « Dossier ALVR… ».");

            Task.Run(() => DetectVbCable());
            LoadSettingsUi();

            timer = new Timer { Interval = 2000 };
            timer.Tick += delegate { Poll(); };
            timer.Start();
            Poll();

            if (config.DesktopViewer) desktopViewer.Start();

            // On rend au casque sa mise en veille normale en quittant.
            FormClosing += delegate
            {
                desktopViewer.Stop();
                if (config.KeepAwake && last.Ready) SetKeepAwake(last.Serial, false);
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                // Barre de titre sombre assortie (Windows 10/11).
                int on = 1;
                DwmSetWindowAttribute(Handle, 20, ref on, 4);
                int caption = ColorTranslator.ToWin32(Theme.NightTop);
                DwmSetWindowAttribute(Handle, 35, ref caption, 4);
                int text = ColorTranslator.ToWin32(Theme.Text);
                DwmSetWindowAttribute(Handle, 36, ref text, 4);
            }
            catch { }
        }

        // ---------------------------------------------------------------- Construction de l'interface

        Label AddLabel(int pageIndex, string text, Font font, Color color, int x, int y, int width = 0)
        {
            var l = new Label { Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent, Location = new Point(x, y) };
            if (width > 0) { l.AutoSize = false; l.Size = new Size(width, font.Height + 4); }
            else l.AutoSize = true;
            AddTo(pageIndex, l);
            return l;
        }

        T AddTo<T>(int pageIndex, T c) where T : Control
        {
            Controls.Add(c);
            if (pageIndex >= 0) pages[pageIndex].Add(c);
            return c;
        }

        MoonButton AddButton(int pageIndex, string text, bool primary, int x, int y, int w, int h)
        {
            return AddTo(pageIndex, new MoonButton(text, primary) { Location = new Point(x, y), Size = new Size(w, h) });
        }

        void BuildHeader()
        {
            AddLabel(-1, "LunaQuest", Theme.Title, Theme.Text, 28, 20);
            AddLabel(-1, "PC VR en USB pour Meta Quest · propulsé par ALVR", Theme.Body, Theme.Soft, 34, 70);
            tabs = AddTo(-1, new Segmented("Connexion", "Réglages ALVR", "Guide") { Location = new Point(32, 110), Size = new Size(430, 40) });
            tabs.SelectedIndex = 0;
            tabs.SelectionChanged += delegate { ShowPage(tabs.SelectedIndex); };
        }

        void BuildConnectionPage()
        {
            cards[0].Add(new Card(32, 170, 440, 320, "État"));
            cards[0].Add(new Card(492, 170, 436, 320, "Actions"));
            cards[0].Add(new Card(32, 506, 896, 174, "Journal"));

            battery = AddLabel(0, "", Theme.Small, Theme.Soft, 302, 184, 150);
            battery.TextAlign = ContentAlignment.MiddleRight;

            string[] names = { "ADB", "Casque", "ALVR (casque)", "Tunnel USB", "ALVR (PC)", "SteamVR" };
            for (int i = 0; i < names.Length; i++)
            {
                int y = 216 + i * 40;
                dots[i] = AddTo(0, new StatusDot { Location = new Point(50, y) });
                AddLabel(0, names[i], Theme.Body, Theme.Soft, 78, y);
                values[i] = AddLabel(0, "…", Theme.BodyBold, Theme.Text, 196, y - 1, 266);
            }

            btnConnect = AddButton(0, "Connecter", true, 512, 212, 396, 58);
            btnConnect.Click += delegate { if (connectedState) RunBusy(FullDisconnect); else RunBusy(Connect); };

            btnAdb = AddButton(0, "Installer ADB", false, 512, 286, 192, 42);
            btnAdb.Click += delegate { RunBusy(InstallAdb); };
            btnApk = AddButton(0, "Installer l'appli casque", false, 716, 286, 192, 42);
            btnApk.Click += delegate { InstallApk(); };
            btnFolder = AddButton(0, "Dossier ALVR…", false, 512, 338, 192, 42);
            btnFolder.Click += delegate { PickAlvrFolder(); };
            btnRestartVr = AddButton(0, "Relancer SteamVR", false, 716, 338, 192, 42);
            btnRestartVr.Click += delegate { RunBusy(RestartSteamVr); };

            tglAuto = AddTo(0, new ToggleSwitch("Connexion automatique au branchement") { Location = new Point(514, 392), Size = new Size(394, 28), Checked = config.AutoConnect });
            tglAuto.CheckedChanged += delegate { config.AutoConnect = tglAuto.Checked; config.Save(); };
            tglDesktop = AddTo(0, new ToggleSwitch("Écran du PC dans le casque (2× menu gauche)") { Location = new Point(514, 456), Size = new Size(394, 28), Checked = config.DesktopViewer });
            tglDesktop.CheckedChanged += delegate
            {
                config.DesktopViewer = tglDesktop.Checked;
                config.Save();
                if (tglDesktop.Checked) desktopViewer.Start();
                else Task.Run(() => { desktopViewer.Stop(); Log("Écran PC désactivé."); });
            };
            tglAwake = AddTo(0, new ToggleSwitch("Garder le casque éveillé (évite les jeux figés)") { Location = new Point(514, 424), Size = new Size(394, 28), Checked = config.KeepAwake });
            tglAwake.CheckedChanged += delegate
            {
                config.KeepAwake = tglAwake.Checked;
                config.Save();
                if (last.Ready) { string serial = last.Serial; bool on = tglAwake.Checked; Task.Run(() => SetKeepAwake(serial, on)); }
            };

            log = AddTo(0, new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.None, BorderStyle = BorderStyle.None,
                BackColor = Theme.Field, ForeColor = Theme.Soft, Font = Theme.Mono,
                Location = new Point(52, 546), Size = new Size(856, 118)
            });
            fields.Add(log);
        }

        void BuildSettingsPage()
        {
            cards[1].Add(new Card(32, 170, 440, 322, "Image"));
            cards[1].Add(new Card(492, 170, 436, 322, "Son, manettes et image"));
            cards[1].Add(new Card(32, 508, 440, 108, "Casque filaire"));
            cards[1].Add(new Card(492, 508, 436, 108, "Mes préréglages"));

            AddLabel(1, "Images par seconde (Hz)", Theme.Body, Theme.Soft, 52, 204);
            segFps = AddTo(1, new Segmented("72", "80", "90", "120") { Location = new Point(52, 228), Size = new Size(400, 38) });

            AddLabel(1, "Débit vidéo", Theme.Body, Theme.Soft, 52, 282);
            lblBitrate = AddLabel(1, "", Theme.BodyBold, Theme.Text, 302, 281, 150);
            lblBitrate.TextAlign = ContentAlignment.MiddleRight;
            sldBitrate = AddTo(1, new MoonSlider(20, 300, 5) { Location = new Point(48, 306), Size = new Size(408, 30) });

            AddLabel(1, "Codec", Theme.Body, Theme.Soft, 52, 350);
            segCodec = AddTo(1, new Segmented("H.264", "HEVC", "AV1") { Location = new Point(52, 374), Size = new Size(400, 38) });

            AddLabel(1, "Résolution par œil (largeur en pixels)", Theme.Body, Theme.Soft, 52, 422);
            segRes = AddTo(1, new Segmented("1680", "2064", "2448", "2880", "3264", "3840") { Location = new Point(52, 446), Size = new Size(400, 38) });

            tglGameAudio = AddTo(1, new ToggleSwitch("Son du jeu dans le casque") { Location = new Point(512, 210), Size = new Size(396, 28) });
            tglMic = AddTo(1, new ToggleSwitch("Micro du casque") { Location = new Point(512, 254), Size = new Size(396, 28) });
            lblMicHint = AddLabel(1, "", Theme.Small, Theme.Dim, 570, 284, 340);
            tglSkeleton = AddTo(1, new ToggleSwitch("Mains squelettiques (suivi des mains)") { Location = new Point(512, 320), Size = new Size(396, 28) });
            AddLabel(1, "À laisser activé : sans lui, une manette peut manquer.", Theme.Small, Theme.Dim, 570, 350, 340);
            tglFoveation = AddTo(1, new ToggleSwitch("Fovéation (bords de l'image moins nets)") { Location = new Point(512, 380), Size = new Size(396, 28) });
            AddLabel(1, "Plus léger pour le câble, mais l'image paraît baveuse.", Theme.Small, Theme.Dim, 570, 408, 340);
            tglZone = AddTo(1, new ToggleSwitch("Mémoire de zone (trackers)") { Location = new Point(512, 436), Size = new Size(396, 28) });
            AddLabel(1, "Garde la calibration des trackers entre les sessions.", Theme.Small, Theme.Dim, 570, 464, 340);

            AddLabel(1, "Nom du casque (ex. 1234.client.alvr)", Theme.Body, Theme.Soft, 52, 542);
            txtHostname = AddTo(1, new TextBox
            {
                BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text, Font = new Font("Segoe UI", 10.5f),
                Location = new Point(56, 576), Size = new Size(190, 24)
            });
            fields.Add(txtHostname);
            lblClients = AddLabel(1, "", Theme.Small, Theme.Soft, 268, 578, 194);

            // Mes préréglages : on choisit un nom dans la liste ou on en tape un nouveau.
            presets = new PresetStore(Path.Combine(appDir, "LunaQuest.presets"));
            cmbPresets = AddTo(1, new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, BackColor = Theme.Field, ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 10f), Location = new Point(516, 542), Size = new Size(388, 26)
            });
            fields.Add(cmbPresets);
            btnPresetLoad = AddButton(1, "Charger", false, 512, 578, 124, 30);
            btnPresetLoad.Click += delegate { LoadPreset(); };
            btnPresetSave = AddButton(1, "Sauvegarder", false, 644, 578, 140, 30);
            btnPresetSave.Click += delegate { SavePreset(); };
            btnPresetDelete = AddButton(1, "Supprimer", false, 792, 578, 116, 30);
            btnPresetDelete.Click += delegate { DeletePreset(); };
            RefreshPresetList(null);

            lblSettingsStatus = AddLabel(1, "", Theme.Small, Theme.Soft, 34, 646, 490);
            btnOpenAlvr = AddButton(1, "Ouvrir ALVR (avancé)", false, 540, 632, 180, 44);
            btnOpenAlvr.Click += delegate { OpenAlvrDashboard(); };
            btnSave = AddButton(1, "Enregistrer", true, 734, 632, 194, 46);
            btnSave.Font = Theme.BodyBold;
            btnSave.Click += delegate { SaveSettings(); };

            EventHandler dirty = delegate { MarkDirty(); };
            segFps.SelectionChanged += dirty;
            segCodec.SelectionChanged += dirty;
            segRes.SelectionChanged += dirty;
            tglGameAudio.CheckedChanged += dirty;
            tglMic.CheckedChanged += delegate { MarkDirty(); UpdateMicHint(); };
            tglSkeleton.CheckedChanged += dirty;
            tglFoveation.CheckedChanged += dirty;
            tglZone.CheckedChanged += dirty;
            sldBitrate.ValueChanged += delegate { lblBitrate.Text = sldBitrate.Value + " Mbit/s"; MarkDirty(); };
            txtHostname.TextChanged += dirty;
        }

        void BuildGuidePage()
        {
            cards[2].Add(new Card(32, 170, 896, 510, "Premiers pas"));
            var open = AddButton(2, "Ouvrir le tutoriel complet", false, 704, 624, 204, 40);
            open.Click += delegate
            {
                string tuto = Path.Combine(appDir, "Tutoriel.html");
                if (File.Exists(tuto)) Process.Start(tuto);
                else MessageBox.Show(this, "Le fichier Tutoriel.html n'est pas à côté de LunaQuest.exe.", "LunaQuest");
            };
        }

        static readonly string[,] GuideSteps =
        {
            { "Active le mode développeur", "Appli Meta Horizon sur ton téléphone › Appareils › ton casque › Mode développeur." },
            { "Installe ALVR sur le PC", "Sur github.com/alvr-org/ALVR, version 20.14.1, prends alvr_streamer_windows.zip. Extrais-le, par exemple dans Documents, et lance ALVR Dashboard une seule fois." },
            { "Prépare le casque", "Onglet Connexion : Installer ADB, branche le casque avec le câble USB, puis accepte le débogage USB qui s'affiche dans le casque." },
            { "Installe l'appli du casque", "Clique sur Installer l'appli casque : LunaQuest Client, fourni avec LunaQuest, s'installe et s'enregistre tout seul. Rien à recopier." },
            { "Joue !", "Clique sur Connecter : LunaQuest ouvre le tunnel USB, SteamVR et l'appli du casque. Ensuite, il suffit de brancher le casque." },
            { "En cas de souci", "Jeu figé : garde le casque éveillé. Saccades : baisse le débit. Pas de son : active Son du jeu. Une manette absente : active les mains squelettiques." }
        };

        void PaintGuide(Graphics g)
        {
            for (int i = 0; i < GuideSteps.GetLength(0); i++)
            {
                int col = i % 2, row = i / 2;
                int x = 56 + col * 444, y = 214 + row * 118;
                var circle = new RectangleF(x, y, 34, 34);
                Theme.Glow(g, new RectangleF(x - 10, y - 10, 54, 54), Color.FromArgb(70, 110, 160, 255));
                using (var b = new LinearGradientBrush(circle, Theme.AccentLight, Theme.AccentDark, 90f)) g.FillEllipse(b, circle);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var tb = new SolidBrush(Theme.Text))
                    g.DrawString((i + 1).ToString(), Theme.BodyBold, tb, circle, sf);
                using (var tb = new SolidBrush(Theme.Text)) g.DrawString(GuideSteps[i, 0], Theme.StepTitle, tb, x + 48, y + 4);
                using (var tb = new SolidBrush(Theme.Soft)) g.DrawString(GuideSteps[i, 1], Theme.Body, tb, new RectangleF(x + 48, y + 30, 368, 78));
            }
        }

        void ShowPage(int index)
        {
            page = index;
            SuspendLayout();
            for (int i = 0; i < pages.Length; i++)
                foreach (var c in pages[i]) c.Visible = i == index;
            ResumeLayout();
            if (index == 1 && !settingsDirty) LoadSettingsUi();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.DrawImageUnscaled(space, 0, 0);
            Theme.Smooth(g);
            foreach (var card in cards[page])
            {
                using (var path = Theme.Round(card.Bounds, 18))
                {
                    using (var b = new SolidBrush(Theme.CardFill)) g.FillPath(b, path);
                    using (var pen = new Pen(Theme.CardBorder, 1f)) g.DrawPath(pen, path);
                }
                using (var tb = new SolidBrush(Theme.Dim)) g.DrawString(card.Title.ToUpperInvariant(), Theme.CardTitle, tb, card.Bounds.X + 20, card.Bounds.Y + 14);
            }
            foreach (var f in fields)
            {
                if (!f.Visible) continue;
                var r = new RectangleF(f.Left - 10, f.Top - 7, f.Width + 20, f.Height + 14);
                using (var path = Theme.Round(r, 10))
                {
                    using (var b = new SolidBrush(Theme.Field)) g.FillPath(b, path);
                    using (var pen = new Pen(Color.FromArgb(70, 145, 185, 255), 1f)) g.DrawPath(pen, path);
                }
            }
            if (page == 2) PaintGuide(g);
        }

        void SetRow(int i, Color c, string text)
        {
            dots[i].DotColor = c;
            if (values[i].Text != text) values[i].Text = text;
        }

        void Log(string msg)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Log), msg); return; }
            log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine);
        }

        // Exécute une action longue hors du thread de l'interface pour qu'elle reste fluide.
        void RunBusy(Action action, Action thenOnUi = null)
        {
            if (busy) return;
            busy = true;
            SetButtonsEnabled(false);
            Task.Run(action).ContinueWith(t =>
            {
                if (t.Exception != null) Log("Erreur : " + t.Exception.GetBaseException().Message);
                BeginInvoke(new Action(() =>
                {
                    busy = false;
                    SetButtonsEnabled(true);
                    if (thenOnUi != null) thenOnUi();
                    Poll();
                }));
            });
        }

        void SetButtonsEnabled(bool on)
        {
            foreach (var b in new[] { btnConnect, btnAdb, btnApk, btnFolder, btnRestartVr, btnSave, btnOpenAlvr }) b.Enabled = on;
            if (on) UpdateConnectButton();
            else btnConnect.Text = "Patiente…";
        }

        void UpdateConnectButton()
        {
            btnConnect.Text = connectedState ? "Déconnecter" : "Connecter";
            btnConnect.Danger = connectedState;
        }

        // ---------------------------------------------------------------- Surveillance

        void Poll()
        {
            if (polling || IsDisposed) return;
            polling = true;
            Task.Run(() => TakeSnapshot()).ContinueWith(t =>
            {
                polling = false;
                if (t.Exception != null || IsDisposed) return;
                try { BeginInvoke(new Action<Snapshot>(Apply), t.Result); } catch (InvalidOperationException) { }
            });
        }

        Snapshot TakeSnapshot()
        {
            var s = new Snapshot();
            s.SteamVrRunning = IsRunning("vrserver");
            s.AlvrFolder = AlvrInstall.IsValidFolder(config.AlvrFolder) ? config.AlvrFolder : null;
            if (s.AlvrFolder != null)
            {
                string session = AlvrInstall.SessionPath(s.AlvrFolder);
                s.SessionFound = File.Exists(session);
                s.DriverRegistered = AlvrInstall.IsRegistered(s.AlvrFolder);
                if (s.SessionFound) s.HasWiredClient = SessionHasWiredClient(session);
            }

            s.AdbFound = Adb.Path != null;
            if (!s.AdbFound) return s;

            foreach (var raw in Adb.Run("devices").Out.Split('\n'))
            {
                var parts = raw.Trim().Split('\t');
                if (parts.Length == 2 && !raw.StartsWith("List"))
                {
                    s.Serial = parts[0];
                    s.State = parts[1];
                    if (s.State == "device") break;
                }
            }
            if (!s.Ready) return s;

            string ser = "-s " + s.Serial + " ";
            string model, pkg;
            lock (cacheLock)
            {
                modelCache.TryGetValue(s.Serial, out model);
                packageCache.TryGetValue(s.Serial, out pkg);
            }
            if (model == null)
            {
                model = Adb.Run(ser + "shell getprop ro.product.model").Out.Trim();
                lock (cacheLock) modelCache[s.Serial] = model;
            }
            if (pkg == null)
            {
                pkg = FindAlvrPackage(Adb.Run(ser + "shell pm list packages alvr").Out) ?? "";
                lock (cacheLock) packageCache[s.Serial] = pkg;
            }
            s.Model = model;
            s.AlvrPackage = pkg == "" ? null : pkg;

            foreach (var line in Adb.Run(ser + "shell dumpsys battery").Out.Split('\n'))
            {
                var l = line.Trim();
                int lvl;
                if (l.StartsWith("level:") && int.TryParse(l.Substring(6).Trim(), out lvl)) { s.Battery = lvl; break; }
            }

            s.Forwarded = Adb.Run(ser + "forward --list").Out.Contains("tcp:" + AlvrControlPort);
            if (s.AlvrPackage != null) s.ClientRunning = Adb.Run(ser + "shell pidof " + s.AlvrPackage).Out.Trim().Length > 0;
            return s;
        }

        bool SessionHasWiredClient(string path)
        {
            try
            {
                var stamp = File.GetLastWriteTimeUtc(path);
                lock (cacheLock) { if (stamp == sessionStamp) return sessionHasClient; }
                bool has = AlvrSession.Load(path).WiredClients.Count > 0;
                lock (cacheLock) { sessionStamp = stamp; sessionHasClient = has; }
                return has;
            }
            catch { return false; }
        }

        static string FindAlvrPackage(string pmOutput)
        {
            // Ordre de préférence : LunaQuest Client (ALVR au thème lune), puis ALVR stable, puis nightly/dev.
            string[] preferred = { LunaClientPackage, "alvr.client.stable", "alvr.client", "alvr.client.nightly", "alvr.client.dev" };
            var found = new List<string>();
            foreach (var line in pmOutput.Split('\n'))
            {
                var l = line.Trim();
                if (l.StartsWith("package:")) found.Add(l.Substring(8));
            }
            foreach (var p in preferred) if (found.Contains(p)) return p;
            foreach (var p in found) if (p.StartsWith("alvr.")) return p;
            return null;
        }

        static bool IsRunning(string name)
        {
            var procs = Process.GetProcessesByName(name);
            bool any = procs.Length > 0;
            foreach (var p in procs) p.Dispose();
            return any;
        }

        void Apply(Snapshot s)
        {
            last = s;

            SetRow(0, s.AdbFound ? Theme.Green : Theme.Red, s.AdbFound ? "Prêt" : "Non installé");

            if (s.Serial == null) SetRow(1, Theme.Dim, "Non branché");
            else if (s.State == "unauthorized") SetRow(1, Theme.Orange, "Autorise le débogage USB");
            else if (s.State != "device") SetRow(1, Theme.Orange, "État : " + s.State);
            else SetRow(1, Theme.Green, string.IsNullOrEmpty(s.Model) ? s.Serial : s.Model);

            if (!s.Ready) SetRow(2, Theme.Dim, "—");
            else if (s.AlvrPackage == null) SetRow(2, Theme.Red, "Non installé (Installer l'appli casque)");
            else SetRow(2, Theme.Green, "Installé");

            if (!s.Ready) { SetRow(3, Theme.Dim, "—"); usbMBps = 0; }
            else if (!s.Forwarded) SetRow(3, Theme.Dim, "Inactif");
            else if (usbMBps > 0 && usbMBps < UsbThreeMBps) SetRow(3, Theme.Orange, "Actif · USB 2.0 (image floue)");
            else if (usbMBps > 0) SetRow(3, Theme.Green, "Actif · USB 3");
            else SetRow(3, Theme.Green, "Actif");

            if (s.AlvrFolder == null) SetRow(4, Theme.Red, "Introuvable (Dossier ALVR…)");
            else if (!s.SessionFound) SetRow(4, Theme.Orange, "Lance ALVR une fois (Réglages)");
            else if (!s.HasWiredClient) SetRow(4, Theme.Orange, "Ajoute ton casque (Réglages)");
            else if (!s.DriverRegistered) SetRow(4, Theme.Orange, "Prêt (driver ajouté à la connexion)");
            else SetRow(4, Theme.Green, "Prêt");

            SetRow(5, s.SteamVrRunning ? Theme.Green : Theme.Dim, s.SteamVrRunning ? "Lancé" : "Fermé");

            bool connected = s.Ready && s.Forwarded && s.SteamVrRunning;
            // Quitter l'appli dans le casque pendant une session = se déconnecter depuis le casque.
            if (connectedState && connected && clientWasRunning && !s.ClientRunning && !busy)
            {
                Log("Appli quittée dans le casque : déconnexion.");
                clientWasRunning = false;
                RunBusy(FullDisconnect);
                return;
            }
            clientWasRunning = s.ClientRunning;
            if (connected != connectedState)
            {
                connectedState = connected;
                if (!busy) UpdateConnectButton();
            }

            string bat = s.Battery >= 0 ? "Batterie casque " + s.Battery + " %" : "";
            if (battery.Text != bat) battery.Text = bat;
            battery.ForeColor = s.Battery >= 0 && s.Battery < 20 ? Theme.Red : Theme.Soft;

            if (!s.Ready) autoConnectedSerial = null;
            else if (config.AutoConnect && !busy && autoConnectedSerial != s.Serial && s.AlvrPackage != null)
            {
                autoConnectedSerial = s.Serial;
                Log("Casque détecté, connexion automatique…");
                RunBusy(Connect);
            }
        }

        // ---------------------------------------------------------------- Actions de connexion

        void Connect()
        {
            var s = TakeSnapshot();
            if (!s.AdbFound) { Log("Installe d'abord ADB."); return; }
            if (s.Serial == null) { Log("Aucun casque détecté. Branche-le et vérifie que le mode développeur est activé."); return; }
            if (!s.Ready) { Log("Accepte « Autoriser le débogage USB » dans le casque."); return; }
            if (s.AlvrFolder == null) { Log("ALVR introuvable sur le PC : indique son dossier avec « Dossier ALVR… »."); return; }
            if (!s.SessionFound) { Log("Lance ALVR une fois (Réglages ALVR › Ouvrir ALVR) pour créer sa configuration."); return; }

            string ser = "-s " + s.Serial + " ";
            foreach (int port in new[] { AlvrControlPort, AlvrStreamPort })
            {
                var r = Adb.Run(ser + "forward tcp:" + port + " tcp:" + port);
                if (!r.Ok) { Log("Échec du tunnel USB (port " + port + ") : " + r.All); return; }
            }
            Log("Tunnel USB ouvert.");
            MeasureUsbSpeed(s.Serial);

            if (config.KeepAwake) SetKeepAwake(s.Serial, true);

            if (!s.SteamVrRunning)
            {
                // Ces fichiers ne doivent être modifiés que SteamVR fermé.
                if (!s.DriverRegistered)
                {
                    try { AlvrInstall.Register(s.AlvrFolder); Log("Driver ALVR ajouté à SteamVR."); }
                    catch (Exception e) { Log("Impossible d'ajouter ALVR à SteamVR : " + e.Message); }
                }
                try
                {
                    var session = AlvrSession.Load(AlvrInstall.SessionPath(s.AlvrFolder));
                    bool changed = false;
                    if (session.Protocol != "Tcp") { session.Protocol = "Tcp"; changed = true; Log("ALVR passé en TCP (obligatoire en USB)."); }
                    // LunaQuest Client a un nom de casque fixe : on l'enregistre pour le câble sans rien demander.
                    if (s.AlvrPackage == LunaClientPackage && !session.WiredClients.Contains(LunaClientHostname))
                    {
                        session.AddWiredClient(LunaClientHostname);
                        changed = true;
                        Log("Casque « " + LunaClientHostname + " » enregistré pour le câble.");
                    }
                    // Un seul casque actif sur le câble : celui de l'appli qui va être lancée dans le casque.
                    string active = s.AlvrPackage == LunaClientPackage ? LunaClientHostname
                        : session.WiredClients.Find(h => h != LunaClientHostname);
                    if (active != null && session.SetActiveWiredClient(active))
                    {
                        changed = true;
                        Log("Casque actif pour le câble : " + active + ".");
                    }
                    if (changed) session.Save();
                }
                catch (Exception e) { Log("Config ALVR illisible : " + e.Message); }
            }
            if (!s.HasWiredClient && s.AlvrPackage != LunaClientPackage) Log("Attention : aucun casque enregistré pour le câble. Va dans Réglages ALVR › Casque filaire.");

            // Le serveur ALVR tourne dans SteamVR : pas besoin d'ouvrir ALVR Dashboard.
            if (!s.SteamVrRunning)
            {
                try { Process.Start("steam://rungameid/250820"); Log("Lancement de SteamVR…"); }
                catch (Exception e) { Log("Impossible de lancer SteamVR : " + e.Message); }
            }

            if (s.AlvrPackage == null) { Log("L'appli du casque n'est pas installée : clique sur « Installer l'appli casque »."); return; }
            var launch = Adb.Run(ser + "shell monkey -p " + s.AlvrPackage + " -c android.intent.category.LAUNCHER 1");
            if (!launch.Ok || launch.All.Contains("No activities found") || launch.All.Contains("aborted"))
                launch = Adb.Run(ser + "shell am start -n " + s.AlvrPackage + "/android.app.NativeActivity");
            Log(launch.Ok ? "ALVR lancé dans le casque. Bon jeu !" : "Lance ALVR à la main dans le casque (" + launch.All + ")");
        }

        // Ferme le jeu VR, SteamVR et ALVR dans le casque ; LunaQuest reste ouvert pour se reconnecter.
        void FullDisconnect()
        {
            Log("Déconnexion…");
            int games = CloseVrGames();
            if (games > 0) Log(games == 1 ? "Jeu VR fermé." : games + " jeux VR fermés.");
            StopSteamVr();
            Log("SteamVR fermé.");

            var s = TakeSnapshot();
            if (s.Ready)
            {
                string ser = "-s " + s.Serial + " ";
                Adb.Run(ser + "forward --remove tcp:" + AlvrControlPort);
                Adb.Run(ser + "forward --remove tcp:" + AlvrStreamPort);
                if (s.AlvrPackage != null) Adb.Run(ser + "shell am force-stop " + s.AlvrPackage);
                SetKeepAwake(s.Serial, false);
            }
            Log("Casque déconnecté. Clique sur Connecter pour rejouer.");
        }

        void RestartSteamVr()
        {
            Log("Redémarrage de SteamVR…");
            StopSteamVr();
            System.Threading.Thread.Sleep(2000);
            try { Process.Start("steam://rungameid/250820"); } catch (Exception e) { Log("Impossible de lancer SteamVR : " + e.Message); return; }
            // On relance aussi ALVR dans le casque pour qu'il se reconnecte tout de suite.
            var s = TakeSnapshot();
            if (s.Ready && s.AlvrPackage != null)
            {
                System.Threading.Thread.Sleep(6000);
                Adb.Run("-s " + s.Serial + " shell monkey -p " + s.AlvrPackage + " -c android.intent.category.LAUNCHER 1");
            }
            Log("SteamVR relancé.");
        }

        // Outils qui chargent aussi les bibliothèques VR mais qu'il ne faut pas fermer (overlays, enregistrement, Steam…).
        static readonly string[] NotGames =
        {
            "vrserver", "vrmonitor", "vrcompositor", "vrdashboard", "vrwebhelper", "vrstartup", "vrcmd", "ALVR Dashboard",
            "SpaceCalibrator", "OVR Toolkit", "OVR Toolkit Settings", "AdvancedSettings", "Standable", "steam", "steamwebhelper",
            "obs64", "obs32", "Discord", "explorer", "LunaQuest"
        };

        // Un jeu VR est un programme qui a chargé OpenVR ou OpenXR (VRChat, Beat Saber lancé par BSManager, etc.).
        static int CloseVrGames()
        {
            int closed = 0, self = Process.GetCurrentProcess().Id;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id == self || Array.IndexOf(NotGames, p.ProcessName) >= 0) continue;
                    bool vr = false;
                    foreach (ProcessModule m in p.Modules)
                    {
                        string n = m.ModuleName.ToLowerInvariant();
                        if (n == "openvr_api.dll" || n == "openxr_loader.dll") { vr = true; break; }
                    }
                    if (!vr) continue;
                    p.CloseMainWindow();
                    if (!p.WaitForExit(4000)) p.Kill();
                    closed++;
                }
                catch { } // processus système ou protégé : on ne peut pas lire ses modules, ce n'est pas un jeu
                finally { p.Dispose(); }
            }
            return closed;
        }

        // Envoie un petit fichier au casque pour savoir si le câble est en USB 2.0 (principale cause d'image floue).
        void MeasureUsbSpeed(string serial)
        {
            string local = Path.Combine(Path.GetTempPath(), "lunaquest-usbtest.bin");
            const string remote = "/data/local/tmp/lunaquest-usbtest.bin";
            try
            {
                var data = new byte[24 * 1024 * 1024];
                new Random().NextBytes(data); // données aléatoires : impossibles à compresser pendant l'envoi
                File.WriteAllBytes(local, data);
                var sw = Stopwatch.StartNew();
                var r = Adb.Run("-s " + serial + " push \"" + local + "\" " + remote, 60000);
                sw.Stop();
                Adb.Run("-s " + serial + " shell rm -f " + remote);
                if (!r.Ok) { Log("Test du câble impossible : " + r.All); return; }
                usbMBps = data.Length / 1048576.0 / Math.Max(0.05, sw.Elapsed.TotalSeconds);
                if (usbMBps < UsbThreeMBps)
                    Log(string.Format(CultureInfo.InvariantCulture,
                        "Câble : {0:0} Mo/s, soit de l'USB 2.0. L'image sera floue : branche le casque directement sur un port USB 3 du PC (sans hub ni rallonge), avec un câble USB 3.", usbMBps));
                else
                    Log(string.Format(CultureInfo.InvariantCulture, "Câble : {0:0} Mo/s, USB 3. Tu peux monter le débit vidéo au-dessus de 200 Mbit/s.", usbMBps));
            }
            catch (Exception e) { Log("Test du câble impossible : " + e.Message); }
            finally { try { File.Delete(local); } catch { } }
        }

        // Simule le capteur de présence : le casque ne se met plus en veille quand on l'enlève.
        void SetKeepAwake(string serial, bool on)
        {
            string action = on ? "com.oculus.vrpowermanager.prox_close" : "com.oculus.vrpowermanager.automation_disable";
            var r = Adb.Run("-s " + serial + " shell am broadcast -a " + action);
            if (!r.Ok) Log("Impossible de régler la veille du casque : " + r.All);
            else Log(on ? "Casque maintenu éveillé." : "Mise en veille normale du casque rétablie.");
        }

        void InstallAdb()
        {
            const string url = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";
            string zip = Path.Combine(Path.GetTempPath(), "lunaquest-platform-tools.zip");
            try
            {
                Log("Téléchargement d'ADB depuis Google…");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var wc = new WebClient()) wc.DownloadFile(url, zip);

                Adb.Run("kill-server", 3000);
                if (Directory.Exists(Adb.LocalDir)) Directory.Delete(Adb.LocalDir, true);
                // L'archive contient déjà un dossier « platform-tools ».
                ZipFile.ExtractToDirectory(zip, appDir);

                Adb.Path = Adb.Find();
                Log(Adb.Path != null ? "ADB installé." : "ADB téléchargé mais introuvable après extraction.");
            }
            catch (Exception e) { Log("Échec de l'installation d'ADB : " + e.Message); }
            finally { try { File.Delete(zip); } catch { } }
        }

        void InstallApk()
        {
            if (!last.Ready) { Log("Branche et autorise le casque avant d'installer l'appli du casque."); return; }
            // L'installateur fournit LunaQuest Client à côté de l'exe ; sinon on demande l'APK à installer.
            string apk = Path.Combine(appDir, "LunaQuest-Client.apk");
            if (!File.Exists(apk))
            {
                using (var dlg = new OpenFileDialog { Title = "Choisis l'APK : LunaQuest-Client.apk ou alvr_client_android.apk", Filter = "APK Android (*.apk)|*.apk", InitialDirectory = appDir })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    apk = dlg.FileName;
                }
            }
            string serial = last.Serial;
            RunBusy(() =>
            {
                Log("Installation de " + Path.GetFileName(apk) + " sur le casque…");
                var r = Adb.Run("-s " + serial + " install -r \"" + apk + "\"", 180000);
                Log(r.Ok && r.All.Contains("Success") ? "Appli du casque installée : elle est dans Bibliothèque › Sources inconnues." : "Échec de l'installation : " + r.All);
                lock (cacheLock) packageCache.Remove(serial);
            });
        }

        void PickAlvrFolder()
        {
            using (var dlg = new OpenFileDialog { Title = "Choisis « ALVR Dashboard.exe » dans le dossier d'ALVR", Filter = "ALVR Dashboard|ALVR Dashboard.exe|Programme (*.exe)|*.exe" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string folder = Path.GetDirectoryName(dlg.FileName);
                if (!AlvrInstall.IsValidFolder(folder)) { Log("Ce dossier ne contient pas ALVR (driver.vrdrivermanifest manquant)."); return; }
                config.AlvrFolder = folder;
                config.Save();
                Log("Dossier ALVR : " + folder);
                LoadSettingsUi();
                Poll();
            }
        }

        // ---------------------------------------------------------------- Réglages ALVR

        void DetectVbCable()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_SoundDevice"))
                    foreach (ManagementObject o in searcher.Get())
                    {
                        string name = (o["Name"] as string) ?? "";
                        if (name.Contains("VB-Audio") || name.Contains("VoiceMeeter") || name.Contains("Virtual Audio Cable")) vbCable = true;
                    }
            }
            catch { }
            try { BeginInvoke(new Action(UpdateMicHint)); } catch { }
        }

        void UpdateMicHint()
        {
            if (vbCable) { lblMicHint.Text = "VB-CABLE détecté : le micro peut fonctionner."; lblMicHint.ForeColor = Theme.Dim; }
            else if (tglMic.Checked) { lblMicHint.Text = "VB-CABLE absent : ALVR refusera la connexion !"; lblMicHint.ForeColor = Theme.Orange; }
            else { lblMicHint.Text = "Nécessite VB-CABLE (vb-audio.com)."; lblMicHint.ForeColor = Theme.Dim; }
        }

        void MarkDirty()
        {
            if (loadingSettings) return;
            settingsDirty = true;
            lblSettingsStatus.Text = "Modifications non enregistrées.";
            lblSettingsStatus.ForeColor = Theme.Orange;
        }

        void SetSettingsEnabled(bool on)
        {
            foreach (Control c in new Control[] { segFps, sldBitrate, segCodec, segRes, tglGameAudio, tglMic, tglSkeleton, tglFoveation, tglZone, txtHostname, btnSave, cmbPresets, btnPresetLoad, btnPresetSave, btnPresetDelete }) c.Enabled = on;
        }

        void LoadSettingsUi()
        {
            loadingSettings = true;
            try
            {
                if (!AlvrInstall.IsValidFolder(config.AlvrFolder))
                {
                    SetSettingsEnabled(false);
                    lblClients.Text = "";
                    lblSettingsStatus.Text = "ALVR introuvable : indique son dossier dans l'onglet Connexion.";
                    lblSettingsStatus.ForeColor = Theme.Orange;
                    return;
                }
                string path = AlvrInstall.SessionPath(config.AlvrFolder);
                if (!File.Exists(path))
                {
                    SetSettingsEnabled(false);
                    lblClients.Text = "";
                    lblSettingsStatus.Text = "Lance ALVR une fois (bouton ci-contre) pour créer sa configuration.";
                    lblSettingsStatus.ForeColor = Theme.Orange;
                    return;
                }

                var s = AlvrSession.Load(path);
                SetSettingsEnabled(true);
                segFps.SelectedIndex = NearestFps(s.Fps);
                sldBitrate.Value = s.BitrateMbps;
                lblBitrate.Text = sldBitrate.Value + " Mbit/s";
                segCodec.SelectedIndex = Math.Max(0, Array.IndexOf(CodecValues, s.Codec));
                segRes.SelectedIndex = NearestWidth(s.EyeWidth);
                tglGameAudio.Checked = s.GameAudio;
                tglMic.Checked = s.Microphone;
                tglSkeleton.Checked = s.HandSkeleton;
                tglFoveation.Checked = s.Foveation;
                tglZone.Checked = s.ZoneMemory;
                txtHostname.Text = "";
                var clients = s.WiredClients;
                lblClients.Text = clients.Count > 0 ? "Enregistré : " + string.Join(", ", clients.ToArray()) : "Aucun casque enregistré pour le câble.";
                lblClients.ForeColor = clients.Count > 0 ? Theme.Soft : Theme.Orange;
                settingsDirty = false;
                lblSettingsStatus.Text = "Appliqué au prochain lancement de SteamVR.";
                lblSettingsStatus.ForeColor = Theme.Dim;
                UpdateMicHint();
            }
            catch (Exception e)
            {
                SetSettingsEnabled(false);
                lblSettingsStatus.Text = "Config ALVR illisible : " + e.Message;
                lblSettingsStatus.ForeColor = Theme.Red;
            }
            finally { loadingSettings = false; }
        }

        // ---------------------------------------------------------------- Mes préréglages

        AlvrValues CaptureUi()
        {
            return new AlvrValues
            {
                Fps = FpsChoices[Math.Max(0, segFps.SelectedIndex)],
                Bitrate = sldBitrate.Value,
                Codec = CodecValues[Math.Max(0, segCodec.SelectedIndex)],
                GameAudio = tglGameAudio.Checked,
                Mic = tglMic.Checked,
                Skeleton = tglSkeleton.Checked,
                Foveation = tglFoveation.Checked,
                ZoneMemory = tglZone.Checked,
                EyeWidth = segRes.SelectedIndex >= 0 ? EyeWidths[segRes.SelectedIndex] : 0,
                NewClient = txtHostname.Text.Trim()
            };
        }

        void RefreshPresetList(string select)
        {
            cmbPresets.Items.Clear();
            foreach (var name in presets.Names) cmbPresets.Items.Add(name);
            cmbPresets.Text = select ?? "";
        }

        void SetPresetStatus(string text, Color color)
        {
            lblSettingsStatus.Text = text;
            lblSettingsStatus.ForeColor = color;
        }

        void SavePreset()
        {
            string name = cmbPresets.Text.Trim();
            if (name == "") { SetPresetStatus("Tape un nom de préréglage avant de sauvegarder.", Theme.Orange); return; }
            presets.Save(name, CaptureUi());
            RefreshPresetList(name);
            SetPresetStatus("Préréglage « " + name + " » sauvegardé.", Theme.Green);
        }

        void LoadPreset()
        {
            string name = cmbPresets.Text.Trim();
            AlvrValues v = presets.Get(name);
            if (v == null) { SetPresetStatus("Choisis un préréglage dans la liste.", Theme.Orange); return; }
            segFps.SelectedIndex = NearestFps(v.Fps);
            sldBitrate.Value = v.Bitrate;
            segCodec.SelectedIndex = Math.Max(0, Array.IndexOf(CodecValues, v.Codec));
            if (v.EyeWidth > 0) segRes.SelectedIndex = NearestWidth(v.EyeWidth);
            tglGameAudio.Checked = v.GameAudio;
            tglMic.Checked = v.Mic;
            tglSkeleton.Checked = v.Skeleton;
            tglFoveation.Checked = v.Foveation;
            MarkDirty();
            SetPresetStatus("Préréglage « " + name + " » chargé : clique sur Enregistrer pour l'appliquer.", Theme.AccentLight);
        }

        void DeletePreset()
        {
            string name = cmbPresets.Text.Trim();
            if (presets.Get(name) == null) { SetPresetStatus("Choisis un préréglage dans la liste.", Theme.Orange); return; }
            presets.Delete(name);
            RefreshPresetList(null);
            SetPresetStatus("Préréglage « " + name + " » supprimé.", Theme.Dim);
        }

        // -1 (aucune case) quand ALVR est réglé autrement qu'en largeur fixe : on ne touchera pas à sa résolution.
        static int NearestWidth(int width)
        {
            if (width <= 0) return -1;
            int best = 0;
            for (int i = 1; i < EyeWidths.Length; i++)
                if (Math.Abs(EyeWidths[i] - width) < Math.Abs(EyeWidths[best] - width)) best = i;
            return best;
        }

        static int NearestFps(double fps)
        {
            int best = 0;
            for (int i = 1; i < FpsChoices.Length; i++)
                if (Math.Abs(FpsChoices[i] - fps) < Math.Abs(FpsChoices[best] - fps)) best = i;
            return best;
        }

        void SaveSettings()
        {
            if (!AlvrInstall.IsValidFolder(config.AlvrFolder)) return;
            var v = CaptureUi();
            if (v.Mic && !vbCable &&
                MessageBox.Show(this, "VB-CABLE n'est pas installé : avec le micro activé, ALVR refusera de se connecter.\n\nEnregistrer quand même ?",
                    "LunaQuest", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // SteamVR et ALVR Dashboard réécrivent la config : il faut les fermer avant.
            bool vrRunning = IsRunning("vrserver");
            bool dashRunning = IsRunning("ALVR Dashboard");
            if ((vrRunning || dashRunning) &&
                MessageBox.Show(this, "SteamVR doit redémarrer pour appliquer les réglages.\n\nRedémarrer maintenant ?",
                    "LunaQuest", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            string path = AlvrInstall.SessionPath(config.AlvrFolder);
            RunBusy(() =>
            {
                if (vrRunning || dashRunning) { StopSteamVr(); Log("SteamVR fermé pour appliquer les réglages."); }
                var s = AlvrSession.Load(path);
                v.ApplyTo(s);
                if (s.FixCodecForWidth())
                    Log("Image de " + s.EncodedWidth + " px de large : trop pour le H264 (" + AlvrSession.H264MaxWidth + " px max), codec passé en HEVC.");
                s.Save();
                Log("Réglages ALVR enregistrés.");
                if (v.EyeWidth > 0)
                {
                    try { SteamVrConfig.LockRenderScale(); Log("SteamVR réglé à 100 % pour rendre exactement " + v.EyeWidth + " px par œil."); }
                    catch (Exception e) { Log("Impossible de régler la résolution de SteamVR : " + e.Message); }
                }
                if (vrRunning)
                {
                    Process.Start("steam://rungameid/250820");
                    Log("Redémarrage de SteamVR…");
                }
            }, () =>
            {
                settingsDirty = false;
                LoadSettingsUi();
                lblSettingsStatus.Text = vrRunning ? "Enregistré, SteamVR redémarre." : "Enregistré. Appliqué au prochain lancement.";
                lblSettingsStatus.ForeColor = Theme.Green;
            });
        }

        static void StopSteamVr()
        {
            foreach (var name in SteamVrProcesses)
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); p.WaitForExit(5000); } catch { }
                    p.Dispose();
                }
        }

        void OpenAlvrDashboard()
        {
            if (!AlvrInstall.IsValidFolder(config.AlvrFolder)) { Log("ALVR introuvable : indique son dossier dans l'onglet Connexion."); return; }
            string exe = AlvrInstall.DashboardPath(config.AlvrFolder);
            if (!File.Exists(exe)) { Log("ALVR Dashboard.exe introuvable dans " + config.AlvrFolder); return; }
            Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = config.AlvrFolder });
            Log("ALVR Dashboard ouvert. Ferme-le avant d'enregistrer des réglages ici.");
        }
    }
}
