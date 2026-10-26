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
            // Un seul LunaQuest à la fois : un second lancement ramène la fenêtre déjà ouverte (ou cachée près de l'horloge).
            bool first;
            var single = new System.Threading.Mutex(true, @"Local\LunaQuest-instance", out first);
            if (!first)
            {
                try { System.Threading.EventWaitHandle.OpenExisting(@"Local\LunaQuest-show").Set(); } catch { }
                return;
            }
            GC.KeepAlive(single);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Thème et langue choisis dans Paramètres, appliqués avant de construire la fenêtre.
            var config = new Config(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LunaQuest.ini"));
            Theme.Apply(config.Theme);
            Lang.Load(config.Language);
            Theme.ApplyFonts(Lang.Code);
            Application.Run(new MainForm());
        }
    }

    // ================================================================ Thème (par défaut, les couleurs du site : nuit, rouge érable, bleu électrique)

    static class Theme
    {
        // Thèmes au choix (Paramètres) : nom enregistré, puis nom affiché.
        public static readonly string[] Names = { "noir", "blanc", "bleu", "rouge", "rose", "violet", "vert", "jaune" };
        public static readonly string[] Labels = { "Noir", "Blanc", "Bleu", "Rouge", "Rose", "Violet", "Vert", "Jaune" };
        public static string Current = "noir";
        public static bool IsLight;

        // « Maple » : couleur principale (boutons, titres), « Electric » : couleur secondaire (contours, dégradés).
        public static Color NightTop, NightBottom, Sidebar, Field, CardFill, CardBorder, Text, Soft, Dim;
        public static Color Maple, MapleSoft, MapleDark, Electric, ElectricSoft, MoonLight, Accent, AccentLight, AccentDark;
        public static Color Green, Orange, Red, OnAccent, SelectedText;

        public static Font Display, Tag, Eyebrow, Body, BodyBold, Small, CardTitle, Button, ButtonBig, StepTitle, Mono;

        static Theme()
        {
            Apply("noir");
            ApplyFonts("fr");
        }

        static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static void Apply(string name)
        {
            Current = Array.IndexOf(Names, name) >= 0 ? name : "noir";
            IsLight = Current == "blanc";
            Green = Color.FromArgb(94, 227, 176);
            Orange = Color.FromArgb(255, 196, 108);
            OnAccent = Color.FromArgb(244, 243, 255);
            switch (Current)
            {
                case "blanc":
                    NightTop = Color.FromArgb(246, 247, 252); NightBottom = Color.FromArgb(226, 231, 246);
                    Sidebar = Color.FromArgb(234, 237, 248); Field = Color.White;
                    CardFill = Color.FromArgb(230, 255, 255, 255); CardBorder = Color.FromArgb(70, 45, 91, 255);
                    Text = Color.FromArgb(20, 22, 44); Soft = Color.FromArgb(68, 76, 108); Dim = Color.FromArgb(118, 126, 150);
                    Maple = Color.FromArgb(232, 22, 42); MapleSoft = Color.FromArgb(208, 32, 54); MapleDark = Color.FromArgb(176, 12, 28);
                    Electric = Color.FromArgb(45, 91, 255); ElectricSoft = Color.FromArgb(48, 86, 214);
                    MoonLight = Color.FromArgb(45, 91, 255); SelectedText = Color.White; AccentDark = Color.FromArgb(30, 62, 190);
                    Green = Color.FromArgb(18, 150, 100); Orange = Color.FromArgb(200, 118, 8);
                    break;
                case "bleu": Dark(Color.FromArgb(1, 5, 16), Color.FromArgb(5, 14, 40), Color.FromArgb(0, 176, 255), Color.FromArgb(45, 91, 255)); break;
                case "rouge": Dark(Color.FromArgb(10, 2, 4), Color.FromArgb(28, 6, 10), Color.FromArgb(255, 31, 46), Color.FromArgb(255, 122, 40)); break;
                case "rose": Dark(Color.FromArgb(12, 3, 10), Color.FromArgb(32, 8, 26), Color.FromArgb(255, 64, 156), Color.FromArgb(200, 110, 255)); break;
                case "violet": Dark(Color.FromArgb(7, 3, 16), Color.FromArgb(20, 8, 42), Color.FromArgb(176, 88, 255), Color.FromArgb(96, 76, 255)); break;
                case "vert": Dark(Color.FromArgb(2, 9, 6), Color.FromArgb(6, 24, 16), Color.FromArgb(40, 214, 120), Color.FromArgb(0, 176, 196)); break;
                case "jaune":
                    Dark(Color.FromArgb(10, 8, 2), Color.FromArgb(26, 20, 5), Color.FromArgb(255, 196, 0), Color.FromArgb(255, 132, 0));
                    OnAccent = Color.FromArgb(24, 18, 2); // texte foncé sur les boutons jaunes
                    break;
                default: // noir : les couleurs du site
                    NightTop = Color.FromArgb(2, 3, 9); NightBottom = Color.FromArgb(7, 9, 26);
                    Sidebar = Color.FromArgb(4, 5, 14); Field = Color.FromArgb(5, 7, 20);
                    CardFill = Color.FromArgb(190, 9, 12, 32); CardBorder = Color.FromArgb(46, 130, 160, 255);
                    Text = Color.FromArgb(244, 243, 255); Soft = Color.FromArgb(168, 180, 220); Dim = Color.FromArgb(110, 122, 166);
                    Maple = Color.FromArgb(255, 31, 46); MapleSoft = Color.FromArgb(255, 123, 134); MapleDark = Color.FromArgb(205, 20, 34);
                    Electric = Color.FromArgb(45, 91, 255); ElectricSoft = Color.FromArgb(143, 178, 255);
                    MoonLight = Color.FromArgb(207, 224, 255); SelectedText = NightTop; AccentDark = Color.FromArgb(30, 62, 190);
                    break;
            }
            Accent = Electric;
            AccentLight = ElectricSoft;
            Red = MapleSoft;
        }

        // Couleurs d'un thème sans l'appliquer (aperçus de la page Paramètres).
        public static Color[] Preview(string name)
        {
            string current = Current;
            Apply(name);
            var colors = new[] { NightTop, NightBottom, Maple, Electric, Text, CardFill };
            Apply(current);
            return colors;
        }

        // Thème sombre coloré : fond nuit teinté, couleur principale et secondaire.
        static void Dark(Color top, Color bottom, Color main, Color second)
        {
            var white = Color.FromArgb(244, 243, 255);
            NightTop = top; NightBottom = bottom;
            Sidebar = Mix(top, bottom, 0.4f); Field = Mix(top, bottom, 0.55f);
            CardFill = Color.FromArgb(190, Mix(bottom, second, 0.04f)); CardBorder = Color.FromArgb(54, Mix(second, white, 0.4f));
            Text = white; Soft = Mix(Color.FromArgb(180, 186, 210), Mix(second, white, 0.5f), 0.35f); Dim = Mix(Color.FromArgb(112, 118, 146), second, 0.18f);
            Maple = main; MapleSoft = Mix(main, white, 0.42f); MapleDark = Mix(main, Color.Black, 0.22f);
            Electric = second; ElectricSoft = Mix(second, white, 0.5f);
            MoonLight = Mix(Mix(second, white, 0.5f), white, 0.55f); SelectedText = top; AccentDark = Mix(second, Color.Black, 0.35f);
        }

        // Polices : celles du site, ou des polices qui ont les caractères chinois / japonais.
        public static void ApplyFonts(string lang)
        {
            string ui = lang == "zh" ? "Microsoft YaHei UI" : lang == "ja" ? "Yu Gothic UI" : null;
            Display = new Font("Impact", 34f);
            Tag = new Font("Segoe Print", 13f, FontStyle.Bold);
            Eyebrow = new Font(ui ?? "Consolas", 8.5f, FontStyle.Bold);
            Body = new Font(ui ?? "Segoe UI", 10f);
            BodyBold = new Font(ui ?? "Segoe UI Semibold", 10.5f, ui != null ? FontStyle.Bold : FontStyle.Regular);
            Small = new Font(ui ?? "Segoe UI", 9f);
            CardTitle = new Font(ui ?? "Consolas", 8.5f, FontStyle.Bold);
            Button = new Font(ui ?? "Consolas", 9f, FontStyle.Bold);
            ButtonBig = new Font(ui ?? "Consolas", 13f, FontStyle.Bold);
            StepTitle = new Font(ui ?? "Segoe UI Semibold", 11.5f, ui != null ? FontStyle.Bold : FontStyle.Regular);
            Mono = new Font(ui ?? "Consolas", 9f);
        }

        // Feuille d'érable du site (repère -1..1, pointe vers le haut).
        public static readonly PointF[] Leaf =
        {
            new PointF(0, -1), new PointF(.12f, -.62f), new PointF(.38f, -.78f), new PointF(.3f, -.38f), new PointF(.72f, -.5f),
            new PointF(.6f, -.22f), new PointF(.95f, -.1f), new PointF(.55f, .12f), new PointF(.62f, .36f), new PointF(.2f, .25f),
            new PointF(.08f, .62f), new PointF(0, .42f), new PointF(-.08f, .62f), new PointF(-.2f, .25f), new PointF(-.62f, .36f),
            new PointF(-.55f, .12f), new PointF(-.95f, -.1f), new PointF(-.6f, -.22f), new PointF(-.72f, -.5f), new PointF(-.3f, -.38f),
            new PointF(-.38f, -.78f), new PointF(-.12f, -.62f)
        };

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

        // Texte espacé façon « eyebrow » du site (GDI+ ne sait pas espacer les lettres tout seul).
        public static float DrawSpaced(Graphics g, string text, Font font, Color color, float x, float y, float spacing)
        {
            using (var b = new SolidBrush(color))
            using (var sf = StringFormat.GenericTypographic)
            {
                foreach (char ch in text)
                {
                    string s = ch.ToString();
                    g.DrawString(s, font, b, x, y, sf);
                    x += (ch == ' ' ? font.Size * 0.6f : g.MeasureString(s, font, PointF.Empty, sf).Width) + spacing;
                }
            }
            return x;
        }

        public static void DrawLeaf(Graphics g, float x, float y, float size, float angle, Color c)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle);
            g.ScaleTransform(size, size);
            using (var b = new SolidBrush(c)) g.FillPolygon(b, Leaf);
            using (var pen = new Pen(c, 0.08f)) g.DrawLine(pen, 0, .42f, 0, 1.05f);
            g.Restore(state);
        }
    }

    // ================================================================ Décor : nuit étoilée, lueurs rouge et bleue, lune

    static class SpaceArt
    {
        public static Bitmap Render(Size size, int sidebarWidth)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Theme.Smooth(g);
                var full = new Rectangle(Point.Empty, size);
                using (var b = new LinearGradientBrush(full, Theme.NightTop, Theme.NightBottom, 80f)) g.FillRectangle(b, full);

                // Lueurs de gyrophare très douces, comme sur le site.
                Theme.Glow(g, new RectangleF(-260, size.Height - 360, 760, 560), Theme.Alpha(Theme.Maple, 34));
                Theme.Glow(g, new RectangleF(size.Width - 620, -240, 760, 520), Theme.Alpha(Theme.Electric, 38));

                // Étoiles (graine fixe : le ciel est toujours le même).
                var rnd = new Random(7);
                for (int i = 0; i < 300; i++)
                {
                    float x = (float)rnd.NextDouble() * size.Width;
                    float y = (float)rnd.NextDouble() * size.Height;
                    float s = (float)(0.5 + Math.Pow(rnd.NextDouble(), 3) * 2.0);
                    int a = 60 + rnd.Next(170);
                    double k = rnd.NextDouble();
                    Color c = k < .1 ? Theme.Alpha(Theme.MapleSoft, a) : k < .35 ? Theme.Alpha(Theme.ElectricSoft, a) : Theme.Alpha(Theme.Text, a);
                    using (var br = new SolidBrush(c)) g.FillEllipse(br, x - s / 2, y - s / 2, s, s);
                }

                Moon(g, new PointF(size.Width - 104, 74), 40);

                // Barre latérale : plus sombre, avec un filet rouge vers bleu sur le bord droit.
                var side = new Rectangle(0, 0, sidebarWidth, size.Height);
                using (var b = new SolidBrush(Color.FromArgb(235, Theme.Sidebar))) g.FillRectangle(b, side);
                using (var b = new LinearGradientBrush(new Rectangle(sidebarWidth - 1, 0, 2, size.Height), Theme.Maple, Theme.Electric, 90f))
                    g.FillRectangle(b, sidebarWidth - 1, 0, 1, size.Height);
                Theme.Glow(g, new RectangleF(sidebarWidth / 2f - 34, 8, 68, 68), Theme.Alpha(Theme.Maple, 70));
            }
            return bmp;
        }

        public static void Moon(Graphics g, PointF c, float r)
        {
            Theme.Glow(g, new RectangleF(c.X - r * 2.6f, c.Y - r * 2.6f, r * 5.2f, r * 5.2f), Theme.Alpha(Theme.MoonLight, 70));
            var body = new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r);
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(body);
                using (var b = new PathGradientBrush(p))
                {
                    b.CenterPoint = new PointF(c.X - r * 0.32f, c.Y - r * 0.4f);
                    b.CenterColor = Color.White;
                    b.SurroundColors = new[] { Color.FromArgb(142, 162, 212) };
                    g.FillPath(b, p);
                }
                var clip = g.Clip;
                g.SetClip(p, CombineMode.Intersect);
                Crater(g, c.X - r * 0.36f, c.Y + r * 0.12f, r * 0.27f);
                Crater(g, c.X + r * 0.26f, c.Y - r * 0.40f, r * 0.17f);
                Crater(g, c.X + r * 0.08f, c.Y + r * 0.46f, r * 0.14f);
                Crater(g, c.X + r * 0.52f, c.Y + r * 0.10f, r * 0.11f);
                // Ombre côté droit, comme la lune du site.
                using (var sp = new GraphicsPath())
                {
                    sp.AddEllipse(c.X - r * 0.1f, c.Y - r * 1.2f, r * 2.4f, r * 2.4f);
                    using (var sb = new PathGradientBrush(sp))
                    {
                        sb.CenterColor = Color.FromArgb(150, 8, 14, 46);
                        sb.SurroundColors = new[] { Color.FromArgb(0, 8, 14, 46) };
                        sb.FocusScales = new PointF(0.5f, 0.5f);
                        g.FillPath(sb, sp);
                    }
                }
                g.Clip = clip;
            }
        }

        static void Crater(Graphics g, float x, float y, float r)
        {
            using (var b = new SolidBrush(Color.FromArgb(56, 70, 90, 150))) g.FillEllipse(b, x - r, y - r, 2 * r, 2 * r);
        }
    }

    // ================================================================ Contrôles dessinés à la main (boutons carrés)

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

        // Textes traduits dans la langue choisie (le français sert de clé).
        public override string Text { get { return base.Text; } set { base.Text = Lang.T(value); } }
    }

    // Étiquette traduite automatiquement dans la langue choisie.
    class TLabel : Label
    {
        public override string Text { get { return base.Text; } set { base.Text = Lang.T(value); } }
    }

    // Bouton carré : rouge érable pour l'action principale, bleu translucide pour les autres (comme les boutons du site).
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

        // Variante sombre bordée de rouge, pour une action qui arrête tout (Déconnecter).
        public bool Danger { get { return danger; } set { if (danger == value) return; danger = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color text = Theme.Text;
            if (primary && !danger)
            {
                Color fill = !Enabled ? Color.FromArgb(70, Theme.Maple) : Pressed ? Theme.MapleDark : Hover ? Theme.Text : Theme.Maple;
                text = Hover && Enabled && !Pressed ? Theme.NightTop : Theme.OnAccent;
                if (Enabled && !Hover) Theme.Glow(g, new RectangleF(-20, -16, Width + 40, Height + 32), Theme.Alpha(Theme.Maple, 40));
                using (var b = new SolidBrush(fill)) g.FillRectangle(b, r);
            }
            else if (primary)
            {
                int fill = !Enabled ? 20 : Pressed ? 120 : Hover ? 80 : 40;
                using (var b = new SolidBrush(Theme.Alpha(Theme.Maple, fill))) g.FillRectangle(b, r);
                using (var pen = new Pen(Color.FromArgb(Enabled ? 220 : 80, Theme.MapleSoft), 1.5f)) g.DrawRectangle(pen, r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
            }
            else
            {
                Color fill = !Enabled ? Theme.Alpha(Theme.Electric, 18) : Pressed ? Theme.AccentDark : Hover ? Theme.Electric : Theme.Alpha(Theme.Electric, 56);
                if (Enabled && (Hover || Pressed)) text = Theme.OnAccent;
                using (var b = new SolidBrush(fill)) g.FillRectangle(b, r);
                using (var pen = new Pen(Color.FromArgb(Enabled ? 105 : 40, Theme.ElectricSoft), 1f)) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            }
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var tb = new SolidBrush(Enabled ? text : Theme.Alpha(Theme.Text, 110)))
                g.DrawString(Text.ToUpperInvariant(), Font, tb, new RectangleF(0, 0, Width, Height), sf);
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
            var track = new RectangleF(0.5f, (Height - 22) / 2f, 44, 22);
            int a = Enabled ? 255 : 110;
            if (on)
                using (var b = new LinearGradientBrush(track, Theme.Alpha(Theme.Maple, a), Theme.Alpha(Theme.Electric, a), 0f)) g.FillRectangle(b, track);
            else
                using (var b = new SolidBrush(Theme.Alpha(Theme.ElectricSoft, Hover ? 70 : 42))) g.FillRectangle(b, track);
            using (var pen = new Pen(Theme.Alpha(Theme.ElectricSoft, on ? 120 : 80), 1f)) g.DrawRectangle(pen, track.X, track.Y, track.Width, track.Height);
            float kx = on ? track.Right - 19 : track.X + 3;
            using (var b = new SolidBrush(Theme.Alpha(Theme.Text, a))) g.FillRectangle(b, kx, track.Y + 3, 16, 16);
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
            using (var tb = new SolidBrush(Theme.Alpha(Theme.Text, a)))
                g.DrawString(Text, Font, tb, new RectangleF(56, 0, Width - 56, Height), sf);
        }
    }

    class Segmented : SpaceControl
    {
        public string[] Items;
        int selected = -1, hoverIndex = -1;
        public event EventHandler SelectionChanged;

        public Segmented(params string[] items)
        {
            Items = new string[items.Length];
            for (int i = 0; i < items.Length; i++) Items[i] = Lang.T(items[i]);
            Height = 38;
            Font = Theme.Button;
        }

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

        // Onglets du site : choix actif couleur lune sur texte nuit, les autres en bleu translucide.
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            int a = Enabled ? 255 : 110;
            float gap = 4, w = (Width - gap * (Items.Length - 1)) / Items.Length;
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    var seg = new RectangleF(i * (w + gap), 0, w, Height - 1);
                    Color fill = i == selected ? Theme.Alpha(Theme.MoonLight, a)
                        : Theme.Alpha(Theme.Electric, i == hoverIndex && Enabled ? 80 : 32);
                    using (var b = new SolidBrush(fill)) g.FillRectangle(b, seg);
                    using (var tb = new SolidBrush(i == selected ? Theme.SelectedText : Theme.Alpha(i == hoverIndex ? Theme.Text : Theme.Soft, a)))
                        g.DrawString(Items[i], Font, tb, seg, sf);
                }
            }
        }
    }

    class MoonSlider : SpaceControl
    {
        int min, max = 100, step = 1, val;
        bool dragging;
        const int Thumb = 16;
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

        // Barre du site : piste sombre, remplissage rouge vers bleu, curseur carré.
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            int a = Enabled ? 255 : 110;
            float cy = Height / 2f;
            var track = new RectangleF(Thumb / 2f, cy - 4, Width - Thumb, 8);
            using (var b = new SolidBrush(Theme.Alpha(Theme.ElectricSoft, 32))) g.FillRectangle(b, track);
            float t = max > min ? (float)(val - min) / (max - min) : 0;
            float x = track.X + t * track.Width;
            if (x - track.X > 1)
                using (var b = new LinearGradientBrush(track, Theme.Alpha(Theme.Maple, a), Theme.Alpha(Theme.Electric, a), 0f))
                    g.FillRectangle(b, track.X, track.Y, x - track.X, track.Height);
            if (Hover || dragging) Theme.Glow(g, new RectangleF(x - 16, cy - 16, 32, 32), Theme.Alpha(Theme.Maple, 90));
            using (var b = new SolidBrush(Theme.Alpha(Theme.Text, a))) g.FillRectangle(b, x - 7, cy - 9, 14, 18);
        }
    }

    // Aperçu d'un thème (page Paramètres) : fond, couleurs principale et secondaire, nom.
    class ThemeSwatch : SpaceControl
    {
        public readonly string ThemeName;
        readonly Color[] colors;
        readonly string label;
        bool selected;

        public ThemeSwatch(string name, string label)
        {
            ThemeName = name;
            this.label = Lang.T(label);
            colors = Theme.Preview(name);
            Size = new Size(220, 96);
        }

        public bool Selected { get { return selected; } set { if (selected == value) return; selected = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new Rectangle(2, 2, Width - 5, Height - 5);
            using (var b = new LinearGradientBrush(r, colors[0], colors[1], 80f)) g.FillRectangle(b, r);
            // Mini fenêtre : une carte, un bouton principal et un trait dégradé.
            var card = new Rectangle(r.X + 12, r.Y + 34, 120, 44);
            using (var b = new SolidBrush(colors[5])) g.FillRectangle(b, card);
            using (var b = new SolidBrush(colors[2])) g.FillRectangle(b, card.X + 10, card.Y + 12, 56, 18);
            using (var b = new LinearGradientBrush(new Rectangle(card.X + 10, card.Y + 36, 61, 3), colors[2], colors[3], 0f)) g.FillRectangle(b, card.X + 10, card.Y + 36, 60, 3);
            Theme.Glow(g, new RectangleF(r.Right - 74, r.Y + 18, 64, 64), Color.FromArgb(120, colors[3]));
            using (var b = new SolidBrush(colors[3])) g.FillEllipse(b, r.Right - 52, r.Y + 40, 20, 20);
            using (var tb = new SolidBrush(colors[4])) g.DrawString(label, Theme.BodyBold, tb, r.X + 10, r.Y + 7);
            Color edge = selected ? Theme.Maple : Hover ? Theme.ElectricSoft : Theme.Alpha(Theme.ElectricSoft, 70);
            using (var pen = new Pen(edge, selected ? 3f : 1.2f)) g.DrawRectangle(pen, r);
            if (selected)
                using (var pen = new Pen(Theme.Maple, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLines(pen, new[] { new PointF(r.Right - 30, r.Y + 14), new PointF(r.Right - 23, r.Y + 21), new PointF(r.Right - 11, r.Y + 8) });
        }
    }

    // Témoin d'état : petit losange lumineux, comme les puces du site.
    class StatusDot : SpaceControl
    {
        Color dot = Theme.Dim;

        public StatusDot() { Size = new Size(20, 20); Cursor = Cursors.Default; }

        public Color DotColor { get { return dot; } set { if (dot == value) return; dot = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            Theme.Glow(g, new RectangleF(0, 0, Width, Height), Theme.Alpha(dot, 110));
            float cx = Width / 2f, cy = Height / 2f, d = 5;
            using (var b = new SolidBrush(dot))
                g.FillPolygon(b, new[] { new PointF(cx, cy - d), new PointF(cx + d, cy), new PointF(cx, cy + d), new PointF(cx - d, cy) });
        }
    }

    // Bouton carré de la barre de gauche, avec une icône au trait comme celles du site.
    class NavButton : SpaceControl
    {
        public readonly string Icon;
        bool selected;

        public NavButton(string icon, string label) { Icon = icon; Text = label; Size = new Size(52, 52); }

        public bool Selected { get { return selected; } set { if (selected == value) return; selected = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            if (selected)
            {
                using (var b = new LinearGradientBrush(r, Theme.Maple, Theme.Electric, 45f)) g.FillRectangle(b, r);
            }
            else if (Hover)
            {
                using (var b = new SolidBrush(Theme.Alpha(Theme.Electric, 70))) g.FillRectangle(b, r);
                using (var pen = new Pen(Color.FromArgb(110, Theme.ElectricSoft), 1f)) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            }
            Color stroke = selected ? Theme.OnAccent : Hover ? Theme.Text : Theme.Soft;
            var state = g.Save();
            float s = 26f / 24f;
            g.TranslateTransform((Width - 26) / 2f, (Height - 26) / 2f);
            g.ScaleTransform(s, s);
            using (var pen = new Pen(stroke, 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                DrawIcon(g, pen);
            g.Restore(state);
        }

        // Tracés dans un repère 24×24, repris des icônes SVG du site.
        void DrawIcon(Graphics g, Pen pen)
        {
            switch (Icon)
            {
                case "plug": // Connexion : une prise
                    g.DrawLine(pen, 9, 7, 9, 3);
                    g.DrawLine(pen, 15, 7, 15, 3);
                    using (var p = new GraphicsPath())
                    {
                        p.AddLine(7, 7, 17, 7);
                        p.AddLine(17, 7, 17, 12);
                        p.AddArc(7, 7, 10, 10, 0, 180);
                        p.CloseFigure();
                        g.DrawPath(pen, p);
                    }
                    g.DrawLine(pen, 12, 17, 12, 21);
                    break;
                case "sliders": // Réglages : des curseurs
                    g.DrawLine(pen, 4, 6, 20, 6);
                    g.DrawLine(pen, 4, 12, 14, 12);
                    g.DrawLine(pen, 4, 18, 11, 18);
                    g.DrawEllipse(pen, 15, 12, 6, 6);
                    break;
                case "body": // Corps : une silhouette
                    g.DrawEllipse(pen, 9.5f, 2.5f, 5, 5);
                    g.DrawLine(pen, 12, 8, 12, 15);
                    g.DrawLine(pen, 6, 10.5f, 18, 10.5f);
                    g.DrawLine(pen, 12, 15, 8, 21);
                    g.DrawLine(pen, 12, 15, 16, 21);
                    break;
                case "gear": // Paramètres : un engrenage
                    g.DrawEllipse(pen, 8.5f, 8.5f, 7, 7);
                    for (int k = 0; k < 8; k++)
                    {
                        double a = k * Math.PI / 4;
                        float c = (float)Math.Cos(a), sn = (float)Math.Sin(a);
                        g.DrawLine(pen, 12 + c * 6.2f, 12 + sn * 6.2f, 12 + c * 9f, 12 + sn * 9f);
                    }
                    g.DrawEllipse(pen, 5.8f, 5.8f, 12.4f, 12.4f);
                    break;
                case "book": // Guide : un livre ouvert
                    g.DrawLine(pen, 12, 7, 12, 20);
                    g.DrawLines(pen, new[] { new PointF(12, 7), new PointF(9, 5), new PointF(3, 5), new PointF(3, 18), new PointF(9, 18), new PointF(12, 20) });
                    g.DrawLines(pen, new[] { new PointF(12, 7), new PointF(15, 5), new PointF(21, 5), new PointF(21, 18), new PointF(15, 18), new PointF(12, 20) });
                    break;
            }
        }
    }

    // Aperçu en direct des appareils SteamVR : scène 3D (sol en grille, squelette lumineux) qu'on fait
    // tourner à la souris, puis la liste des appareils avec leur suivi et leur batterie.
    class TrackerPreview : Control
    {
        struct P3 { public float X, Y, Z; public P3(float x, float y, float z) { X = x; Y = y; Z = z; } }

        // Positions affichées, lissées vers le dernier relevé pour un mouvement fluide.
        readonly Dictionary<string, P3> shown = new Dictionary<string, P3>();
        float yawOffset = 200f, pitch = 26f, forwardYaw;
        bool haveForward, dragging;
        Point dragStart;
        float dragYaw, dragPitch;
        const int SceneHeight = 330;

        public TrackerPreview()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.SizeWE;
        }

        // Appelé ~60 fois par seconde par la fenêtre tant que la page Corps est affichée.
        public void Tick()
        {
            var devices = DesktopViewer.Devices;
            if (devices != null)
            {
                foreach (var d in devices)
                {
                    if (!d.Connected) continue;
                    string key = d.Index + d.Serial;
                    P3 p;
                    var target = new P3(d.X, d.Y, d.Z);
                    if (!shown.TryGetValue(key, out p) || Dist(p, target) > 1f) p = target;
                    else p = new P3(p.X + (target.X - p.X) * 0.35f, p.Y + (target.Y - p.Y) * 0.35f, p.Z + (target.Z - p.Z) * 0.35f);
                    shown[key] = p;
                    if (d.Class == Valve.VR.ETrackedDeviceClass.HMD && (d.ForwardX != 0 || d.ForwardZ != 0))
                    {
                        float yaw = (float)(Math.Atan2(d.ForwardX, -d.ForwardZ) * 180 / Math.PI);
                        if (!haveForward) forwardYaw = yaw;
                        float diff = ((yaw - forwardYaw + 540) % 360) - 180;
                        forwardYaw += diff * 0.04f; // la caméra suit le joueur doucement
                        haveForward = true;
                    }
                }
            }
            Invalidate();
        }

        static float Dist(P3 a, P3 b) { float x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z; return (float)Math.Sqrt(x * x + y * y + z * z); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Y > SceneHeight) return;
            dragging = true; dragStart = e.Location; dragYaw = yawOffset; dragPitch = pitch;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                yawOffset = dragYaw + (e.X - dragStart.X) * 0.5f;
                pitch = Math.Max(2, Math.Min(60, dragPitch + (e.Y - dragStart.Y) * 0.3f));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; base.OnMouseUp(e); }
        protected override void OnDoubleClick(EventArgs e) { yawOffset = 200f; pitch = 26f; base.OnDoubleClick(e); }

        static Color StatusColor(VrDevice d) { return !d.Connected ? Theme.Dim : d.Tracking ? Theme.Green : Theme.Orange; }
        static string StatusText(VrDevice d) { return !d.Connected ? "Éteint" : d.Tracking ? "Suivi OK" : "Perte de suivi"; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var scene = new Rectangle(0, 0, Width, SceneHeight);
            using (var b = new LinearGradientBrush(scene, Color.FromArgb(6, 9, 26), Color.FromArgb(2, 3, 10), 90f)) g.FillRectangle(b, scene);
            Theme.Glow(g, new RectangleF(Width / 2f - 170, SceneHeight - 150, 340, 190), Theme.Alpha(Theme.Electric, 40));

            var devices = DesktopViewer.Devices;
            VrDevice hmd = null;
            if (devices != null) foreach (var d in devices) if (d.Class == Valve.VR.ETrackedDeviceClass.HMD && d.Connected) hmd = d;
            if (hmd == null || !shown.ContainsKey(hmd.Index + hmd.Serial))
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var b = new SolidBrush(Theme.Soft))
                    g.DrawString(Lang.T("En attente de SteamVR…\nL'aperçu s'affiche quand le casque est connecté."), Theme.Small, b, scene, sf);
                PaintList(g, devices);
                return;
            }

            P3 head = shown[hmd.Index + hmd.Serial];
            // Caméra en orbite autour du joueur, qui tourne avec lui ; cible à hauteur de hanches.
            var target = new P3(head.X, 0.9f, head.Z);
            double yaw = (forwardYaw + yawOffset) * Math.PI / 180, pit = pitch * Math.PI / 180;
            float cy = (float)Math.Cos(yaw), sy = (float)Math.Sin(yaw), cp = (float)Math.Cos(pit), sp = (float)Math.Sin(pit);
            const float camDist = 3.6f;
            float focal = SceneHeight * 1.45f, ox = Width / 2f, oy = SceneHeight * 0.52f;
            Func<P3, PointF?> project = p =>
            {
                float x = p.X - target.X, y = p.Y - target.Y, z = p.Z - target.Z;
                float rx = x * cy - z * sy, rz = x * sy + z * cy;          // rotation autour de la verticale
                float ry = y * cp - rz * sp, rz2 = y * sp + rz * cp;       // inclinaison
                float depth = camDist + rz2;
                if (depth < 0.2f) return null;
                return new PointF(ox + focal * rx / depth, oy - focal * ry / depth);
            };

            // Sol : grille de 3 × 3 m autour du joueur, qui s'estompe sur les bords.
            for (int i = -6; i <= 6; i++)
            {
                float t = i * 0.25f;
                int alpha = 70 - Math.Abs(i) * 9;
                using (var pen = new Pen(Theme.Alpha(Theme.ElectricSoft, Math.Max(10, alpha)), 1f))
                {
                    DrawLine3(g, pen, project, new P3(head.X + t, 0, head.Z - 1.5f), new P3(head.X + t, 0, head.Z + 1.5f));
                    DrawLine3(g, pen, project, new P3(head.X - 1.5f, 0, head.Z + t), new P3(head.X + 1.5f, 0, head.Z + t));
                }
            }
            // Cercle lumineux sous le joueur.
            var ring = new List<PointF>();
            for (int a = 0; a <= 36; a++)
            {
                var q = project(new P3(head.X + 0.35f * (float)Math.Cos(a * Math.PI / 18), 0, head.Z + 0.35f * (float)Math.Sin(a * Math.PI / 18)));
                if (q.HasValue) ring.Add(q.Value);
            }
            if (ring.Count > 2) using (var pen = new Pen(Theme.Alpha(Theme.Maple, 120), 1.5f)) g.DrawLines(pen, ring.ToArray());

            // Squelette : chaque partie du corps, réelle, virtuelle ou estimée.
            var joints = BuildSkeleton(devices, head);
            string[,] bones = { { "head", "chest" }, { "chest", "waist" }, { "chest", "lshoulder" }, { "chest", "rshoulder" },
                                { "lshoulder", "lelbow" }, { "rshoulder", "relbow" }, { "lelbow", "lhand" }, { "relbow", "rhand" },
                                { "waist", "lknee" }, { "waist", "rknee" }, { "lknee", "lfoot" }, { "rknee", "rfoot" } };
            for (int i = 0; i < bones.GetLength(0); i++)
            {
                Joint a, b;
                if (!joints.TryGetValue(bones[i, 0], out a) || !joints.TryGetValue(bones[i, 1], out b)) continue;
                var pa = project(a.Pos); var pb = project(b.Pos);
                if (!pa.HasValue || !pb.HasValue) continue;
                bool estimated = a.Estimated || b.Estimated;
                using (var glow = new Pen(Theme.Alpha(Theme.Maple, estimated ? 25 : 55), 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(glow, pa.Value, pb.Value);
                using (var line = new LinearGradientBrush(pa.Value, new PointF(pb.Value.X + 0.1f, pb.Value.Y + 0.1f),
                    Color.FromArgb(estimated ? 90 : 230, Theme.MapleSoft), Color.FromArgb(estimated ? 90 : 230, Theme.ElectricSoft)))
                using (var pen = new Pen(line, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    if (estimated) pen.DashStyle = DashStyle.Dash;
                    g.DrawLine(pen, pa.Value, pb.Value);
                }
            }

            // Appareils : point lumineux, forme selon le type, couleur selon le suivi.
            if (devices != null)
                foreach (var d in devices)
                {
                    if (!d.Connected || d.Class == Valve.VR.ETrackedDeviceClass.TrackingReference) continue;
                    P3 p;
                    if (!shown.TryGetValue(d.Index + d.Serial, out p)) continue;
                    var q = project(p);
                    if (!q.HasValue) continue;
                    float x = q.Value.X, y = q.Value.Y;
                    var c = StatusColor(d);
                    // Ombre au sol, pour lire la profondeur.
                    var shadow = project(new P3(p.X, 0, p.Z));
                    if (shadow.HasValue) using (var b = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(b, shadow.Value.X - 7, shadow.Value.Y - 2.5f, 14, 5);
                    Theme.Glow(g, new RectangleF(x - 16, y - 16, 32, 32), Color.FromArgb(110, c));
                    using (var b = new SolidBrush(c))
                    using (var edge = new Pen(Color.FromArgb(200, 255, 255, 255), 1f))
                    {
                        if (d.Class == Valve.VR.ETrackedDeviceClass.HMD) { g.FillRectangle(b, x - 9, y - 5, 18, 10); g.DrawRectangle(edge, x - 9, y - 5, 18, 10); }
                        else if (d.Class == Valve.VR.ETrackedDeviceClass.Controller) { g.FillEllipse(b, x - 6, y - 6, 12, 12); g.DrawEllipse(edge, x - 6, y - 6, 12, 12); }
                        else if (d.IsVirtualTracker)
                        {
                            var diamond = new[] { new PointF(x, y - 7), new PointF(x + 7, y), new PointF(x, y + 7), new PointF(x - 7, y) };
                            g.FillPolygon(b, diamond); g.DrawPolygon(edge, diamond);
                        }
                        else { g.FillRectangle(b, x - 6, y - 6, 12, 12); g.DrawRectangle(edge, x - 6, y - 6, 12, 12); }
                    }
                }

            using (var b = new SolidBrush(Color.FromArgb(150, Theme.Dim)))
                g.DrawString(Lang.T("Glisse pour tourner · double-clic pour recentrer"), Theme.Small, b, 8, SceneHeight - 20);
            using (var b = new SolidBrush(Theme.Dim)) g.DrawString(Lang.T("▬ casque  ● manette  ■ tracker  ◆ virtuel"), Theme.Small, b, 8, 6);
            PaintList(g, devices);
        }

        struct Joint { public P3 Pos; public bool Estimated; }

        // Associe les appareils aux parties du corps ; ce qui manque est estimé (en pointillés).
        Dictionary<string, Joint> BuildSkeleton(VrDevice[] devices, P3 head)
        {
            var j = new Dictionary<string, Joint>();
            Action<string, P3, bool> set = (name, p, est) => j[name] = new Joint { Pos = p, Estimated = est };
            set("head", new P3(head.X, head.Y - 0.08f, head.Z), false);
            double yaw = forwardYaw * Math.PI / 180;
            float fx = (float)Math.Sin(yaw), fz = -(float)Math.Cos(yaw), rx = -fz, rz = fx;
            var reals = new List<P3>();
            foreach (var d in devices)
            {
                P3 p;
                if (!d.Connected || !shown.TryGetValue(d.Index + d.Serial, out p)) continue;
                if (d.Class == Valve.VR.ETrackedDeviceClass.Controller)
                    set(d.Role == Valve.VR.ETrackedControllerRole.LeftHand ? "lhand" : "rhand", p, false);
                else if (d.IsVirtualTracker)
                {
                    string joint = d.Type.Substring(d.Type.LastIndexOf('/') + 1);
                    var map = new Dictionary<string, string> { { "chest", "chest" }, { "waist", "waist" }, { "left_elbow", "lelbow" }, { "right_elbow", "relbow" },
                        { "left_knee", "lknee" }, { "right_knee", "rknee" }, { "left_foot", "lfoot" }, { "right_foot", "rfoot" } };
                    string name;
                    if (map.TryGetValue(joint, out name)) set(name, p, false);
                }
                else if (d.Class == Valve.VR.ETrackedDeviceClass.GenericTracker) reals.Add(p);
            }
            // Vrais trackers : le plus haut aux hanches, les plus bas aux pieds (gauche / droite selon le corps).
            reals.Sort((a, b) => b.Y.CompareTo(a.Y));
            if (reals.Count >= 1 && !j.ContainsKey("waist") && reals[0].Y > 0.5f) { set("waist", reals[0], false); reals.RemoveAt(0); }
            var feet = reals.FindAll(p => p.Y < 0.4f);
            if (feet.Count >= 2)
            {
                bool firstIsLeft = (feet[0].X - feet[1].X) * rx + (feet[0].Z - feet[1].Z) * rz < 0;
                if (!j.ContainsKey("lfoot")) set("lfoot", firstIsLeft ? feet[0] : feet[1], false);
                if (!j.ContainsKey("rfoot")) set("rfoot", firstIsLeft ? feet[1] : feet[0], false);
            }

            // Estimations pour relier le tout.
            P3 h = j["head"].Pos;
            if (!j.ContainsKey("waist")) set("waist", new P3(h.X, Math.Max(0.6f, h.Y * 0.56f), h.Z), true);
            P3 w = j["waist"].Pos;
            if (!j.ContainsKey("chest")) set("chest", new P3(h.X + (w.X - h.X) * 0.35f, h.Y + (w.Y - h.Y) * 0.35f, h.Z + (w.Z - h.Z) * 0.35f), true);
            P3 c = j["chest"].Pos;
            set("lshoulder", new P3(c.X - rx * 0.18f, c.Y + 0.05f, c.Z - rz * 0.18f), true);
            set("rshoulder", new P3(c.X + rx * 0.18f, c.Y + 0.05f, c.Z + rz * 0.18f), true);
            foreach (var side in new[] { "l", "r" })
            {
                Joint hand, sh = j[side + "shoulder"];
                if (!j.ContainsKey(side + "elbow") && j.TryGetValue(side + "hand", out hand))
                    set(side + "elbow", new P3((sh.Pos.X + hand.Pos.X) / 2, (sh.Pos.Y + hand.Pos.Y) / 2 - 0.08f, (sh.Pos.Z + hand.Pos.Z) / 2), true);
                Joint foot;
                if (!j.TryGetValue(side + "foot", out foot)) continue;
                if (!j.ContainsKey(side + "knee"))
                    set(side + "knee", new P3((w.X + foot.Pos.X) / 2 + fx * 0.08f, (w.Y + foot.Pos.Y) / 2, (w.Z + foot.Pos.Z) / 2 + fz * 0.08f), true);
            }
            return j;
        }

        static void DrawLine3(Graphics g, Pen pen, Func<P3, PointF?> project, P3 a, P3 b)
        {
            var pa = project(a); var pb = project(b);
            if (pa.HasValue && pb.HasValue) g.DrawLine(pen, pa.Value, pb.Value);
        }

        // Liste : nom, état du suivi (pastille) et batterie (jauge).
        void PaintList(Graphics g, VrDevice[] devices)
        {
            float y = SceneHeight + 12;
            Theme.DrawSpaced(g, Lang.T("APPAREILS"), Theme.CardTitle, Theme.MapleSoft, 2, y, 2f);
            y += 24;
            if (devices == null) return;
            foreach (var d in devices)
            {
                if (d.Class == Valve.VR.ETrackedDeviceClass.TrackingReference || d.Class == Valve.VR.ETrackedDeviceClass.Invalid) continue;
                if (y > Height - 22) break;
                var c = StatusColor(d);
                using (var b = new SolidBrush(Theme.Alpha(Theme.ElectricSoft, 18))) g.FillRectangle(b, 0, y - 2, Width, 22);
                using (var b = new SolidBrush(c)) g.FillRectangle(b, 0, y - 2, 3, 22);
                using (var b = new SolidBrush(Theme.Text)) g.DrawString(Lang.T(d.Label), Theme.Small, b, 10, y + 1);
                // Pastille d'état.
                string status = Lang.T(StatusText(d));
                var chip = new RectangleF(180, y + 1, 86, 16);
                using (var b = new SolidBrush(Color.FromArgb(40, c))) g.FillRectangle(b, chip);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var b = new SolidBrush(c)) g.DrawString(status, Theme.Small, b, chip, sf);
                // Jauge de batterie (les trackers virtuels n'en ont pas).
                if (d.Battery >= 0 && !d.IsVirtualTracker)
                {
                    int pct = (int)Math.Round(d.Battery * 100);
                    var bar = new RectangleF(276, y + 5, 30, 9);
                    using (var pen = new Pen(Color.FromArgb(120, Theme.Soft), 1f)) { g.DrawRectangle(pen, bar.X, bar.Y, bar.Width, bar.Height); g.FillRectangle(Brushes.Transparent, bar); }
                    using (var b = new SolidBrush(Theme.Soft)) g.FillRectangle(b, bar.Right + 1, bar.Y + 2.5f, 2, 4);
                    using (var b = new SolidBrush(pct <= 20 ? Theme.Red : pct <= 40 ? Theme.Orange : Theme.Green))
                        g.FillRectangle(b, bar.X + 1.5f, bar.Y + 1.5f, (bar.Width - 3) * Math.Max(0, Math.Min(1, d.Battery)), bar.Height - 3);
                    using (var b = new SolidBrush(Theme.Soft)) g.DrawString(pct + "%" + (d.Charging ? "⚡" : ""), Theme.Small, b, 312, y + 1);
                }
                y += 25;
            }
        }
    }

    // Étiquette qui apparaît à droite d'une icône de la barre de gauche quand on la survole.
    class NavTip : Control
    {
        public NavTip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(150, 34);
            Visible = false;
        }

        public void ShowNext(Control target)
        {
            Width = (int)Math.Ceiling(Text.Length * 9.2f + 34);
            Location = new Point(target.Right + 10, target.Top + (target.Height - Height) / 2);
            Visible = true;
            BringToFront();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            g.Clear(Color.FromArgb(10, 12, 32));
            using (var pen = new Pen(Color.FromArgb(90, Theme.MapleSoft), 1f)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(Theme.Maple)) g.FillRectangle(b, 0, 0, 3, Height);
            Theme.DrawSpaced(g, Text.ToUpperInvariant(), Theme.Eyebrow, Theme.Text, 14, (Height - Theme.Eyebrow.Height) / 2f + 1, 1.6f);
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
        public bool BasesAuto;      // base stations allumées à la connexion et en veille à la déconnexion (FBT)
        public string Theme = "noir", Language = "fr";
        public bool Intro = true;   // animation du sabre au démarrage
        public bool TutorialSeen;   // tuto de découverte déjà vu

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
                if (key == "basesauto") BasesAuto = val == "1";
                if (key == "theme") Theme = val;
                if (key == "lang") Language = val;
                if (key == "intro") Intro = val == "1";
                if (key == "tuto") TutorialSeen = val == "1";
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
                    "desktop=" + (DesktopViewer ? "1" : "0"),
                    "basesauto=" + (BasesAuto ? "1" : "0"),
                    "theme=" + Theme,
                    "lang=" + Language,
                    "intro=" + (Intro ? "1" : "0"),
                    "tuto=" + (TutorialSeen ? "1" : "0")
                });
            }
            catch { }
        }
    }

    // ================================================================ Lancement avec Windows

    // LunaQuest au démarrage de Windows, en discret (icône près de l'horloge) : clé « Run » de l'utilisateur.
    static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "LunaQuest";

        static string Command { get { return "\"" + Application.ExecutablePath + "\" --tray"; } }

        public static bool Enabled
        {
            get
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey))
                        return key != null && string.Equals(key.GetValue(Name) as string, Command, StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }
            set
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (value) key.SetValue(Name, Command);
                    else if (key.GetValue(Name) != null) key.DeleteValue(Name, false);
                }
            }
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

        // Manettes vues par SteamVR et les jeux : celles du casque branché (ro.product.model : « Quest 3 »,
        // « Quest 3S », « Quest Pro », « Quest 2 »…). Avec l'entrée SteamVR 2.0, les manettes annoncent un suivi
        // des doigts « estimé », comme les vraies manettes Quest : VRChat garde ses gestes de base au lieu de
        // ceux des manettes Index (suivi « partiel »). Renvoie true si quelque chose a changé.
        public bool MatchHeadset(string model)
        {
            string m = (model ?? "").ToLowerInvariant();
            if (!m.Contains("quest")) return false;
            bool changed = SetIfDifferent(S + "headset.controllers.content.emulation_mode.variant",
                m.Contains("pro") ? "QuestPro" : m.Contains("3") ? "Quest3Plus" : "Quest2Touch");
            if (m.Contains("pro")) changed |= SetIfDifferent(S + "headset.emulation_mode.variant", "QuestPro");
            if (!GetBool(S + "headset.controllers.content.hand_skeleton.content.steamvr_input_2_0"))
            {
                Set(S + "headset.controllers.content.hand_skeleton.content.steamvr_input_2_0", true);
                changed = true;
            }
            return changed;
        }

        bool SetIfDifferent(string path, string value)
        {
            if (GetString(path) == value) return false;
            Set(path, value);
            return true;
        }

        // Suivi du corps par les caméras du Quest, envoyé à SteamVR sous forme de trackers Vive virtuels.
        // Les jambes estimées servent de secours aux genoux quand les vrais trackers ne sont pas là.
        public bool BodyTracking
        {
            get { return GetBool(S + "headset.body_tracking.enabled"); }
            set
            {
                Set(S + "headset.body_tracking.enabled", value);
                if (!value) return;
                Set(S + "headset.body_tracking.content.sources.body_tracking_fb.enabled", true);
                Set(S + "headset.body_tracking.content.sources.body_tracking_fb.content.full_body", true);
                Set(S + "headset.body_tracking.content.sink.variant", "FakeViveTracker");
                Set(S + "headset.body_tracking.content.tracked", true);
            }
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

        // Port de l'interface web locale d'ALVR (statistiques, enregistrement).
        public int WebServerPort
        {
            get { int port = (int)GetNumber(S + "connection.web_server_port", 8082); return port > 0 ? port : 8082; }
        }

        // Au-delà de l'écran du casque, l'image est réduite par le compositeur de Meta. Son filtre « super
        // sampling » fait la moyenne de plusieurs pixels au lieu d'en sauter : plus de détail, moins de scintillement.
        // La netteté artificielle (sharpening) reste toujours coupée.
        public const int HeadsetPanelWidth = 2064;
        public bool SuperSampling
        {
            get
            {
                return GetBool(S + "video.clientside_post_processing.enabled")
                    && GetString(S + "video.clientside_post_processing.content.super_sampling.variant") != "Disabled";
            }
            set
            {
                Set(S + "video.clientside_post_processing.enabled", value);
                Set(S + "video.clientside_post_processing.content.super_sampling.variant", "Quality");
                Set(S + "video.clientside_post_processing.content.sharpening.variant", "Disabled");
            }
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

        // Dossier d'un pilote ALVR (et pas d'un autre pilote SteamVR comme Space Calibrator ou Virtual Desktop).
        static bool IsAlvrFolder(string folder)
        {
            try
            {
                var m = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(folder, "driver.vrdrivermanifest"))) as Dictionary<string, object>;
                object name;
                return m != null && m.TryGetValue("name", out name) && "alvr_server".Equals(name as string, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static string SessionPath(string folder) { return Path.Combine(folder, "session.json"); }

        public static string DashboardPath(string folder) { return Path.Combine(folder, "ALVR Dashboard.exe"); }

        // ALVR (partie PC) fourni avec LunaQuest, dans le dossier d'installation.
        public static string Bundled { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALVR"); } }

        public static string Find()
        {
            if (IsValidFolder(Bundled)) return Bundled;
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
            // Un seul ALVR à la fois dans SteamVR : on retire les autres dossiers ALVR (ex. la version officielle),
            // en gardant tous les autres pilotes.
            if (d.TryGetValue("external_drivers", out list) && list is object[])
                foreach (var item in (object[])list)
                    if (!(item is string) || !IsAlvrFolder((string)item)) drivers.Add(item);
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
            if (EyeWidth > 0) s.SuperSampling = EyeWidth > AlvrSession.HeadsetPanelWidth;
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

        // Après un plantage, SteamVR bloque les drivers chargés à ce moment (« mode sans échec ») et ALVR
        // ne se lance plus. On lève ce blocage (SteamVR doit être fermé). Renvoie true si c'était bloqué.
        public static bool UnblockDriver(string driver)
        {
            string path = FilePath();
            if (!File.Exists(path)) return false;
            var json = new JavaScriptSerializer();
            var root = json.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
            object o;
            var section = root != null && root.TryGetValue("driver_" + driver, out o) ? o as Dictionary<string, object> : null;
            if (section == null || !section.ContainsKey("blocked_by_safe_mode")) return false;
            section.Remove("blocked_by_safe_mode");
            File.Copy(path, path + ".lunaquest.bak", true);
            File.WriteAllText(path, json.Serialize(root), new UTF8Encoding(false));
            return true;
        }

        // Rôles donnés aux vrais trackers Vive dans SteamVR (Gérer les trackers) : numéro de série → rôle.
        public static Dictionary<string, string> ReadViveTrackerRoles()
        {
            var roles = new Dictionary<string, string>();
            try
            {
                var root = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath())) as Dictionary<string, object>;
                object o;
                var trackers = root != null && root.TryGetValue("trackers", out o) ? o as Dictionary<string, object> : null;
                if (trackers == null) return roles;
                const string prefix = "/devices/htc/vive_tracker";
                foreach (var kv in trackers)
                    if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && kv.Value is string)
                        roles[kv.Key.Substring(prefix.Length)] = (string)kv.Value;
            }
            catch { }
            return roles;
        }
    }

    // Réglages du suivi du corps combiné, lus par la partie PC d'ALVR compilée pour LunaQuest
    // (%APPDATA%\LunaQuest\body.json) : trackers virtuels créés, décalages, genoux calculés.
    class BodyConfig
    {
        public static readonly string[] Joints = { "chest", "waist", "left_elbow", "right_elbow", "left_knee", "right_knee", "left_foot", "right_foot" };
        public static readonly string[] JointLabels = { "Torse", "Hanches", "Coude gauche", "Coude droit", "Genou gauche", "Genou droit", "Pied gauche", "Pied droit" };

        public readonly Dictionary<string, bool> Enabled = new Dictionary<string, bool>();
        public readonly Dictionary<string, double[]> Offsets = new Dictionary<string, double[]>();
        public bool KneesFromRealTrackers = true;
        public string Mode = "fbt"; // fbe : FBE Unique, fbt : vrais trackers seuls

        public static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LunaQuest", "body.json"); }
        }

        // Par défaut : vrais trackers aux hanches et aux pieds, le casque complète torse, coudes et genoux.
        public BodyConfig()
        {
            foreach (var j in Joints)
            {
                Enabled[j] = j == "chest" || j.EndsWith("_elbow") || j.EndsWith("_knee");
                Offsets[j] = new double[6];
            }
        }

        public static BodyConfig Load()
        {
            var c = new BodyConfig();
            try
            {
                if (!File.Exists(FilePath)) return c;
                var root = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath)) as Dictionary<string, object>;
                if (root == null) return c;
                object o;
                if (root.TryGetValue("knees_from_real_trackers", out o) && o is bool) c.KneesFromRealTrackers = (bool)o;
                if (root.TryGetValue("mode", out o) && o is string) c.Mode = (string)o;
                var joints = root.TryGetValue("joints", out o) ? o as Dictionary<string, object> : null;
                if (joints != null)
                    foreach (var j in Joints)
                        if (joints.TryGetValue(j, out o) && o is bool) c.Enabled[j] = (bool)o;
                var offsets = root.TryGetValue("offsets", out o) ? o as Dictionary<string, object> : null;
                if (offsets != null)
                    foreach (var j in Joints)
                    {
                        var arr = offsets.TryGetValue(j, out o) ? o as object[] : null;
                        if (arr == null) continue;
                        for (int i = 0; i < 6 && i < arr.Length; i++) c.Offsets[j][i] = Convert.ToDouble(arr[i], CultureInfo.InvariantCulture);
                    }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            var joints = new Dictionary<string, object>();
            var offsets = new Dictionary<string, object>();
            foreach (var j in Joints)
            {
                joints[j] = Enabled[j];
                offsets[j] = Offsets[j];
            }
            var root = new Dictionary<string, object> { { "mode", Mode }, { "knees_from_real_trackers", KneesFromRealTrackers }, { "joints", joints }, { "offsets", offsets } };
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(root), new UTF8Encoding(false));
        }

        public string JointsKey()
        {
            var sb = new StringBuilder();
            foreach (var j in Joints) sb.Append(Enabled[j] ? '1' : '0');
            return sb.ToString();
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
            public Card(int x, int y, int w, int h, string title) { Bounds = new Rectangle(x, y, w, h); Title = Lang.T(title); }
        }

        // Pages : 0 Connexion, 1 Réglages ALVR, 2 Guide, 3 Corps, 4 Paramètres.
        readonly List<Control>[] pages = { new List<Control>(), new List<Control>(), new List<Control>(), new List<Control>(), new List<Control>() };
        readonly List<Card>[] cards = { new List<Card>(), new List<Card>(), new List<Card>(), new List<Card>(), new List<Card>() };
        readonly List<Control> fields = new List<Control>();
        int page;
        Bitmap space;

        const int SidebarWidth = 80;
        NavButton[] nav;
        int[] navPages;
        NavTip navTip;
        Timer fxTimer;

        // Connexion
        readonly StatusDot[] dots = new StatusDot[6];
        readonly Label[] values = new Label[6];
        Label battery;
        MoonButton btnConnect, btnAdb, btnApk, btnFolder, btnRestartVr;
        bool connectedState; // casque branché, tunnel USB ouvert et SteamVR lancé : le gros bouton devient « Déconnecter »
        bool clientWasRunning; // appli ALVR / LunaQuest Client ouverte dans le casque au dernier relevé
        ToggleSwitch tglAuto, tglAwake, tglDesktop, tglStartup;
        DesktopViewer desktopViewer;
        // Perfs, enregistrement, icône près de l'horloge, alertes de batterie, mises à jour.
        Label lblPerf, lblUpdate;
        MoonButton btnRecord;
        bool recording;
        NotifyIcon tray;
        bool startHidden = Array.IndexOf(Environment.GetCommandLineArgs(), "--tray") > 0;
        Timer statsTimer;
        int statsTicks;
        readonly HashSet<string> lowBatteryWarned = new HashSet<string>();
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
            ClientSize = new Size(1100, 760);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.NightTop;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            space = SpaceArt.Render(ClientSize, SidebarWidth);
            BuildSidebar();
            BuildConnectionPage();
            BuildSettingsPage();
            BuildGuidePage();
            BuildBodyPage();
            BuildPreferencesPage();
            ShowPage(0);

            // Depuis la 0.3.0, LunaQuest fournit son propre ALVR (nécessaire pour les modes FBE) : on passe
            // dessus en reprenant les réglages de l'ALVR utilisé jusque-là.
            string bundled = AlvrInstall.Bundled;
            if (AlvrInstall.IsValidFolder(bundled) && AlvrInstall.IsValidFolder(config.AlvrFolder)
                && !string.Equals(Path.GetFullPath(config.AlvrFolder).TrimEnd('\\'), Path.GetFullPath(bundled).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                && !IsLunaStreamer(config.AlvrFolder))
            {
                try
                {
                    string marker = Path.Combine(bundled, "lunaquest-settings-imported");
                    string oldSession = AlvrInstall.SessionPath(config.AlvrFolder);
                    if (!File.Exists(marker) && File.Exists(oldSession))
                    {
                        File.Copy(oldSession, AlvrInstall.SessionPath(bundled), true);
                        File.WriteAllText(marker, config.AlvrFolder);
                    }
                    Log("ALVR fourni avec LunaQuest utilisé, avec tes réglages de " + config.AlvrFolder + ".");
                }
                catch (Exception e) { Log("Réglages ALVR non repris : " + e.Message); }
                config.AlvrFolder = bundled;
                config.Save();
            }

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

            UpdateVrLink();

            try { AlvrLink.Port = AlvrSession.Load(AlvrInstall.SessionPath(config.AlvrFolder)).WebServerPort; } catch { }
            AlvrLink.Start();
            statsTimer = new Timer { Interval = 1000 };
            statsTimer.Tick += delegate { StatsTick(); };
            statsTimer.Start();
            BuildTray();
            Task.Run(() => CheckForUpdate());

            fxTimer = new Timer { Interval = 40 };
            fxTimer.Tick += delegate { Animate(); };
            fxTimer.Start();

            // On rend au casque sa mise en veille normale en quittant.
            FormClosing += delegate
            {
                if (recording) AlvrLink.Request("StopRecording");
                if (tray != null) tray.Visible = false;
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
                int on = Theme.IsLight ? 0 : 1;
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
            var l = new TLabel { Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent, Location = new Point(x, y) };
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

        // Barre de gauche : une icône par page, le nom apparaît au survol. Le guide reste tout en bas.
        void BuildSidebar()
        {
            navTip = new NavTip();
            Controls.Add(navTip);
            var items = new[] { new NavButton("plug", "Connexion"), new NavButton("sliders", "Réglages ALVR"), new NavButton("body", "Corps et trackers"),
                                new NavButton("gear", "Paramètres"), new NavButton("book", "Guide") };
            int[] tops = { 96, 156, 216, ClientSize.Height - 52 - 22 - 60, ClientSize.Height - 52 - 22 };
            navPages = new[] { 0, 1, 3, 4, 2 };
            nav = items;
            for (int i = 0; i < items.Length; i++)
            {
                int index = navPages[i];
                var b = items[i];
                b.Location = new Point((SidebarWidth - b.Width) / 2, tops[i]);
                b.Click += delegate { ShowPage(index); };
                b.MouseEnter += delegate { navTip.Text = b.Text; navTip.ShowNext(b); };
                b.MouseLeave += delegate { navTip.Visible = false; };
                AddTo(-1, b);
            }
        }

        void BuildConnectionPage()
        {
            cards[0].Add(new Card(104, 116, 470, 368, "État"));
            cards[0].Add(new Card(590, 116, 482, 368, "Actions"));
            cards[0].Add(new Card(104, 500, 968, 232, "Journal"));

            battery = AddLabel(0, "", Theme.Small, Theme.Soft, 404, 128, 150);
            battery.TextAlign = ContentAlignment.MiddleRight;

            string[] names = { "ADB", "Casque", "ALVR (casque)", "Tunnel USB", "ALVR (PC)", "SteamVR" };
            for (int i = 0; i < names.Length; i++)
            {
                int y = 168 + i * 42;
                dots[i] = AddTo(0, new StatusDot { Location = new Point(122, y) });
                AddLabel(0, names[i], Theme.Body, Theme.Soft, 150, y);
                values[i] = AddLabel(0, "…", Theme.BodyBold, Theme.Text, 280, y - 1, 280);
            }
            // Perfs en direct, envoyées par ALVR pendant le jeu.
            lblPerf = AddLabel(0, "", Theme.Small, Theme.Dim, 124, 424, 430);
            lblPerf.Height = Theme.Small.Height * 2 + 6;

            btnConnect = AddButton(0, "Connecter", true, 610, 158, 318, 60);
            // Enregistrement vidéo du flux envoyé au casque (fonction d'ALVR).
            btnRecord = AddButton(0, "Filmer", false, 940, 158, 112, 60);
            btnRecord.Click += delegate { ToggleRecording(); };
            btnConnect.Click += delegate { if (connectedState) RunBusy(FullDisconnect); else RunBusy(Connect); };

            btnAdb = AddButton(0, "Installer ADB", false, 610, 234, 215, 42);
            btnAdb.Click += delegate { RunBusy(InstallAdb); };
            btnApk = AddButton(0, "Installer l'appli casque", false, 837, 234, 215, 42);
            btnApk.Click += delegate { InstallApk(); };
            btnFolder = AddButton(0, "Dossier ALVR…", false, 610, 286, 215, 42);
            btnFolder.Click += delegate { PickAlvrFolder(); };
            btnRestartVr = AddButton(0, "Relancer SteamVR", false, 837, 286, 215, 42);
            btnRestartVr.Click += delegate { RunBusy(RestartSteamVr); };

            tglAuto = AddTo(0, new ToggleSwitch("Connexion automatique au branchement") { Location = new Point(612, 344), Size = new Size(440, 28), Checked = config.AutoConnect });
            tglAuto.CheckedChanged += delegate { config.AutoConnect = tglAuto.Checked; config.Save(); };
            tglAwake = AddTo(0, new ToggleSwitch("Garder le casque éveillé (évite les jeux figés)") { Location = new Point(612, 376), Size = new Size(440, 28), Checked = config.KeepAwake });
            tglAwake.CheckedChanged += delegate
            {
                config.KeepAwake = tglAwake.Checked;
                config.Save();
                if (last.Ready) { string serial = last.Serial; bool on = tglAwake.Checked; Task.Run(() => SetKeepAwake(serial, on)); }
            };
            tglDesktop = AddTo(0, new ToggleSwitch("Écran du PC dans le casque (2× menu gauche)") { Location = new Point(612, 408), Size = new Size(440, 28), Checked = config.DesktopViewer });
            tglDesktop.CheckedChanged += delegate
            {
                config.DesktopViewer = tglDesktop.Checked;
                config.Save();
                UpdateVrLink();
                if (!tglDesktop.Checked) Log("Écran PC désactivé.");
            };
            tglStartup = AddTo(0, new ToggleSwitch("Lancer avec Windows (icône près de l'horloge)") { Location = new Point(612, 440), Size = new Size(440, 28), Checked = Startup.Enabled });
            tglStartup.CheckedChanged += delegate
            {
                try { Startup.Enabled = tglStartup.Checked; Log(tglStartup.Checked ? "LunaQuest se lancera avec Windows, en discret près de l'horloge." : "LunaQuest ne se lancera plus avec Windows."); }
                catch (Exception e) { Log("Impossible de changer le lancement avec Windows : " + e.Message); }
            };

            log = AddTo(0, new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.None, BorderStyle = BorderStyle.None,
                BackColor = Theme.Field, ForeColor = Theme.Soft, Font = Theme.Mono,
                Location = new Point(126, 542), Size = new Size(924, 170)
            });
            fields.Add(log);
        }

        void BuildSettingsPage()
        {
            cards[1].Add(new Card(104, 116, 470, 352, "Image"));
            cards[1].Add(new Card(590, 116, 482, 352, "Son, manettes et image"));
            cards[1].Add(new Card(104, 484, 470, 112, "Casque filaire"));
            cards[1].Add(new Card(590, 484, 482, 112, "Mes préréglages"));

            AddLabel(1, "Images par seconde (Hz)", Theme.Body, Theme.Soft, 124, 150);
            segFps = AddTo(1, new Segmented("72", "80", "90", "120") { Location = new Point(124, 174), Size = new Size(430, 38) });

            AddLabel(1, "Débit vidéo", Theme.Body, Theme.Soft, 124, 228);
            lblBitrate = AddLabel(1, "", Theme.BodyBold, Theme.Text, 404, 227, 150);
            lblBitrate.TextAlign = ContentAlignment.MiddleRight;
            sldBitrate = AddTo(1, new MoonSlider(20, 300, 5) { Location = new Point(122, 252), Size = new Size(434, 30) });

            AddLabel(1, "Codec", Theme.Body, Theme.Soft, 124, 296);
            segCodec = AddTo(1, new Segmented("H.264", "HEVC", "AV1") { Location = new Point(124, 320), Size = new Size(430, 38) });

            AddLabel(1, "Résolution par œil (largeur en pixels)", Theme.Body, Theme.Soft, 124, 374);
            segRes = AddTo(1, new Segmented("1680", "2064", "2448", "2880", "3264", "3840") { Location = new Point(124, 398), Size = new Size(430, 38) });

            tglGameAudio = AddTo(1, new ToggleSwitch("Son du jeu dans le casque") { Location = new Point(610, 156), Size = new Size(440, 28) });
            tglMic = AddTo(1, new ToggleSwitch("Micro du casque") { Location = new Point(610, 200), Size = new Size(440, 28) });
            lblMicHint = AddLabel(1, "", Theme.Small, Theme.Dim, 666, 230, 390);
            tglSkeleton = AddTo(1, new ToggleSwitch("Mains squelettiques (suivi des mains)") { Location = new Point(610, 266), Size = new Size(440, 28) });
            AddLabel(1, "À laisser activé : sans lui, une manette peut manquer.", Theme.Small, Theme.Dim, 666, 296, 390);
            tglFoveation = AddTo(1, new ToggleSwitch("Fovéation (bords de l'image moins nets)") { Location = new Point(610, 326), Size = new Size(440, 28) });
            AddLabel(1, "Plus léger pour le câble, mais l'image paraît baveuse.", Theme.Small, Theme.Dim, 666, 354, 390);
            tglZone = AddTo(1, new ToggleSwitch("Mémoire de zone (trackers)") { Location = new Point(610, 384), Size = new Size(440, 28) });
            AddLabel(1, "Garde la calibration des trackers entre les sessions.", Theme.Small, Theme.Dim, 666, 412, 390);

            AddLabel(1, "Nom du casque (ex. 1234.client.alvr)", Theme.Body, Theme.Soft, 124, 518);
            txtHostname = AddTo(1, new TextBox
            {
                BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text, Font = new Font("Segoe UI", 10.5f),
                Location = new Point(130, 552), Size = new Size(200, 24)
            });
            fields.Add(txtHostname);
            lblClients = AddLabel(1, "", Theme.Small, Theme.Soft, 352, 554, 210);

            // Mes préréglages : on choisit un nom dans la liste ou on en tape un nouveau.
            presets = new PresetStore(Path.Combine(appDir, "LunaQuest.presets"));
            cmbPresets = AddTo(1, new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, BackColor = Theme.Field, ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 10f), Location = new Point(616, 518), Size = new Size(430, 26)
            });
            fields.Add(cmbPresets);
            btnPresetLoad = AddButton(1, "Charger", false, 610, 556, 140, 30);
            btnPresetLoad.Click += delegate { LoadPreset(); };
            btnPresetSave = AddButton(1, "Sauvegarder", false, 758, 556, 150, 30);
            btnPresetSave.Click += delegate { SavePreset(); };
            btnPresetDelete = AddButton(1, "Supprimer", false, 916, 556, 136, 30);
            btnPresetDelete.Click += delegate { DeletePreset(); };
            RefreshPresetList(null);

            lblSettingsStatus = AddLabel(1, "", Theme.Small, Theme.Soft, 106, 636, 560);
            btnOpenAlvr = AddButton(1, "Ouvrir ALVR (avancé)", false, 682, 620, 200, 46);
            btnOpenAlvr.Click += delegate { OpenAlvrDashboard(); };
            btnSave = AddButton(1, "Enregistrer", true, 892, 620, 180, 46);
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

        // ---------------------------------------------------------------- Page Paramètres : langue, thème, démarrage

        Segmented segLanguage;
        readonly List<ThemeSwatch> swatches = new List<ThemeSwatch>();
        Label lblPrefStatus;

        void BuildPreferencesPage()
        {
            cards[4].Add(new Card(104, 116, 968, 124, "Langue"));
            cards[4].Add(new Card(104, 256, 968, 272, "Thème"));
            cards[4].Add(new Card(104, 544, 968, 188, "Démarrage"));

            segLanguage = AddTo(4, new Segmented(Lang.Names) { Location = new Point(124, 166), Size = new Size(560, 40) });
            segLanguage.SelectedIndex = Math.Max(0, Array.IndexOf(Lang.Codes, Lang.Code));
            segLanguage.SelectionChanged += delegate
            {
                string code = Lang.Codes[segLanguage.SelectedIndex];
                if (code == config.Language) return;
                config.Language = code;
                config.Save();
                AskRestart();
            };
            var langHint = AddLabel(4, "La langue et le thème s'appliquent au redémarrage de LunaQuest.", Theme.Small, Theme.Dim, 706, 166, 346);
            langHint.Height = Theme.Small.Height * 2 + 8;

            for (int i = 0; i < Theme.Names.Length; i++)
            {
                var sw = AddTo(4, new ThemeSwatch(Theme.Names[i], Theme.Labels[i]) { Location = new Point(124 + (i % 4) * 234, 300 + (i / 4) * 110) });
                sw.Selected = Theme.Names[i] == Theme.Current;
                sw.Click += delegate
                {
                    if (sw.ThemeName == config.Theme) return;
                    foreach (var other in swatches) other.Selected = other == sw;
                    config.Theme = sw.ThemeName;
                    config.Save();
                    AskRestart();
                };
                swatches.Add(sw);
            }

            var tglIntro = AddTo(4, new ToggleSwitch("Animation au démarrage (coup de sabre)") { Location = new Point(124, 590), Size = new Size(520, 28), Checked = config.Intro });
            tglIntro.CheckedChanged += delegate { config.Intro = tglIntro.Checked; config.Save(); };
            var replayIntro = AddButton(4, "Voir l'animation", false, 124, 636, 240, 42);
            replayIntro.Click += delegate { PlayIntro(false); };
            var replayTour = AddButton(4, "Revoir le tutoriel", false, 376, 636, 240, 42);
            replayTour.Click += delegate { StartTour(); };
            lblPrefStatus = AddLabel(4, "", Theme.Small, Theme.Soft, 124, 694, 920);
        }

        // Langue et thème sont lus au démarrage : on propose de relancer LunaQuest tout de suite.
        void AskRestart()
        {
            lblPrefStatus.Text = "Enregistré. Redémarre LunaQuest pour appliquer.";
            if (MessageBox.Show(this, Lang.T("LunaQuest doit redémarrer pour appliquer ce changement.\n\nRedémarrer maintenant ?"), "LunaQuest",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (tray != null) tray.Visible = false;
            Application.Restart();
        }

        // ---------------------------------------------------------------- Animation de démarrage et tutoriel

        IntroOverlay intro;
        TutorialOverlay tour;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (startHidden) return;
            if (config.Intro) PlayIntro(!config.TutorialSeen);
            else if (!config.TutorialSeen) StartTour();
        }

        // Image de la zone de la fenêtre (sans la barre de titre), sans les calques d'animation.
        public Bitmap SnapshotClient()
        {
            bool introShown = intro != null && intro.Visible, tourShown = tour != null && tour.Visible;
            if (introShown) intro.Visible = false;
            if (tourShown) tour.Visible = false;
            var origin = PointToScreen(Point.Empty);
            var client = new Rectangle(origin.X - Left, origin.Y - Top, ClientSize.Width, ClientSize.Height);
            var shot = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var whole = new Bitmap(Width, Height))
            {
                DrawToBitmap(whole, new Rectangle(0, 0, Width, Height));
                using (var g = Graphics.FromImage(shot)) g.DrawImage(whole, new Rectangle(Point.Empty, ClientSize), client, GraphicsUnit.Pixel);
            }
            if (introShown) intro.Visible = true;
            if (tourShown) tour.Visible = true;
            return shot;
        }

        void PlayIntro(bool thenTour)
        {
            if (intro != null || tour != null) return;
            ShowPage(0);
            intro = new IntroOverlay(this) { Bounds = ClientRectangle };
            Controls.Add(intro);
            intro.BringToFront();
            intro.Finished += delegate
            {
                Controls.Remove(intro);
                intro.Dispose();
                intro = null;
                if (thenTour) StartTour();
            };
            intro.Start();
        }

        public void StartTour()
        {
            if (tour != null || intro != null) return;
            tour = new TutorialOverlay(this, TourSteps()) { Bounds = ClientRectangle };
            Controls.Add(tour);
            tour.BringToFront();
            tour.Finished += delegate
            {
                Controls.Remove(tour);
                tour.Dispose();
                tour = null;
                ShowPage(0);
                config.TutorialSeen = true;
                config.Save();
            };
            tour.Start();
        }

        public void ShowPageForTour(int index) { if (page != index) ShowPage(index); }

        // Étapes du tutoriel : page affichée, zone mise en avant (vide : au centre), titre et explication.
        List<TourStep> TourSteps()
        {
            int bottom = ClientSize.Height - 52 - 22;
            return new List<TourStep>
            {
                new TourStep(0, Rectangle.Empty, "Bienvenue dans LunaQuest", "Joue à tes jeux PC VR sur ton Quest par câble USB. Voici un tour rapide de l'appli : il avance tout seul, ou clique pour passer à la suite."),
                new TourStep(0, new Rectangle(12, 90, 56, 190), "Le menu", "Une icône par partie de l'appli. Passe la souris dessus pour voir son nom."),
                new TourStep(0, new Rectangle(104, 116, 470, 368), "État en direct", "ADB, le casque, ALVR, le tunnel USB et SteamVR : tu vois d'un coup d'œil ce qui est prêt. Les perfs du jeu s'affichent en bas."),
                new TourStep(0, new Rectangle(604, 150, 456, 76), "Connecter et filmer", "Un clic ouvre le tunnel USB, SteamVR et l'appli du casque. Le bouton Filmer enregistre ta session en vidéo."),
                new TourStep(0, new Rectangle(604, 338, 456, 136), "Options", "Connexion automatique, casque toujours éveillé, écran du PC dans le casque et lancement avec Windows."),
                new TourStep(0, new Rectangle(104, 500, 968, 232), "Journal", "Tout ce que fait LunaQuest s'affiche ici, avec les éventuels problèmes et comment les régler."),
                new TourStep(1, new Rectangle(104, 116, 470, 352), "Réglages ALVR", "Images par seconde, débit, codec et résolution, sans ouvrir ALVR."),
                new TourStep(1, new Rectangle(590, 484, 482, 112), "Mes préréglages", "Enregistre tes réglages pour chaque jeu et recharge-les en un clic."),
                new TourStep(3, new Rectangle(104, 116, 560, 396), "Corps et trackers", "FBE Unique pour estimer tout le corps avec le casque, ou FBT avec tes vrais trackers Vive."),
                new TourStep(3, new Rectangle(680, 116, 392, 616), "Aperçu en direct", "Tes appareils en 3D, leur suivi et leur batterie. En bas : allumer ou éteindre tes base stations."),
                new TourStep(4, new Rectangle(104, 116, 968, 412), "Paramètres", "Choisis la langue et le thème de LunaQuest."),
                new TourStep(2, new Rectangle(12, bottom - 4, 56, 60), "Guide", "Le guide pas à pas est toujours ici, en bas à gauche. Dans le casque, enfonce les deux joysticks pour voir les perfs. Bon jeu !")
            };
        }

        void BuildGuidePage()
        {
            cards[2].Add(new Card(104, 116, 968, 616, "Premiers pas"));
            var open = AddButton(2, "Ouvrir le tutoriel complet", false, 828, 670, 224, 42);
            open.Click += delegate
            {
                string tuto = Path.Combine(appDir, "Tutoriel.html");
                if (File.Exists(tuto)) Process.Start(tuto);
                else MessageBox.Show(this, Lang.T("Le fichier Tutoriel.html n'est pas à côté de LunaQuest.exe."), "LunaQuest");
            };
        }

        // ---------------------------------------------------------------- Page Corps et trackers

        // Deux façons de suivre le corps : FBE Unique (tout estimé par le casque) et FBT (seulement les vrais trackers).
        static readonly string[] BodyModes = { "fbe", "fbt" };
        static readonly string[] BodyModeNames = { "FBE Unique", "FBT" };
        static readonly string[] BodyModeTexts =
        {
            "Tout le corps estimé par les caméras du casque, sans vrais trackers : choisis les trackers virtuels à créer.",
            "Seulement tes vrais trackers Vive, suivis par tes bases. Le suivi du corps par le casque est coupé."
        };

        Segmented segBodyMode;
        Label lblBodyModeActive, lblBodyModeText, lblRealTrackers, lblBodyStatus;
        // Base stations : allumage et veille par Bluetooth.
        Label lblBase;
        MoonButton btnBaseOn, btnBaseOff;
        readonly ToggleSwitch[] tglJoints = new ToggleSwitch[BodyConfig.Joints.Length];
        ComboBox cmbOffsetJoint;
        readonly NumericUpDown[] numOffsets = new NumericUpDown[6];
        MoonButton btnBodySave;
        TrackerPreview trackerPreview;
        Timer previewTimer;
        BodyConfig body = new BodyConfig();
        bool loadingBody, bodyTrackingSaved;
        string bodyModeSaved = "fbt", bodyJointsSaved = "";

        void BuildBodyPage()
        {
            segBodyMode = AddTo(3, new Segmented(BodyModeNames) { Location = new Point(104, 116), Size = new Size(560, 40) });
            segBodyMode.SelectionChanged += delegate { if (!loadingBody) BodyModeChanged(true); };
            lblBodyModeActive = AddLabel(3, "", Theme.Small, Theme.Soft, 104, 164, 560);

            cards[3].Add(new Card(104, 192, 560, 320, "Réglages du mode"));
            cards[3].Add(new Card(104, 528, 560, 140, "Décalages des trackers virtuels"));
            cards[3].Add(new Card(680, 116, 392, 616, "Aperçu en direct"));

            lblBodyModeText = AddLabel(3, "", Theme.Body, Theme.Soft, 124, 232, 520);
            lblBodyModeText.AutoSize = false;
            lblBodyModeText.Size = new Size(520, 62);

            for (int i = 0; i < tglJoints.Length; i++)
            {
                int col = i % 2, row = i / 2;
                tglJoints[i] = AddTo(3, new ToggleSwitch(BodyConfig.JointLabels[i]) { Location = new Point(124 + col * 260, 302 + row * 36), Size = new Size(250, 28) });
                tglJoints[i].CheckedChanged += delegate { if (!loadingBody) MarkBodyDirty(); };
            }
            lblRealTrackers = AddLabel(3, "", Theme.Small, Theme.Soft, 124, 296, 520);
            lblRealTrackers.AutoSize = false;
            lblRealTrackers.Size = new Size(520, 190);

            cmbOffsetJoint = AddTo(3, new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Field, ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 10f), Location = new Point(128, 568), Size = new Size(196, 26)
            });
            // Liste dessinée à la main : sinon Windows l'affiche en blanc.
            cmbOffsetJoint.DrawMode = DrawMode.OwnerDrawFixed;
            cmbOffsetJoint.DrawItem += (sender, e) =>
            {
                bool selected = (e.State & DrawItemState.Selected) != 0;
                using (var b = new SolidBrush(selected ? Theme.Electric : Theme.Field)) e.Graphics.FillRectangle(b, e.Bounds);
                if (e.Index >= 0)
                    TextRenderer.DrawText(e.Graphics, cmbOffsetJoint.Items[e.Index].ToString(), cmbOffsetJoint.Font, e.Bounds, Theme.Text,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            foreach (var label in BodyConfig.JointLabels) cmbOffsetJoint.Items.Add(Lang.T(label));
            cmbOffsetJoint.SelectedIndex = 0;
            cmbOffsetJoint.SelectedIndexChanged += delegate { ShowOffsets(); };
            fields.Add(cmbOffsetJoint);

            string[] axes = { "Côté (cm)", "Hauteur (cm)", "Avant (cm)", "Rot. X (°)", "Rot. Y (°)", "Rot. Z (°)" };
            for (int i = 0; i < 6; i++)
            {
                int index = i, col = i % 3, row = i / 3;
                AddLabel(3, axes[i], Theme.Small, Theme.Soft, 348 + col * 104, 552 + row * 52, 100);
                numOffsets[i] = AddTo(3, new NumericUpDown
                {
                    Minimum = i < 3 ? -50 : -180, Maximum = i < 3 ? 50 : 180, DecimalPlaces = i < 3 ? 1 : 0, Increment = i < 3 ? 0.5m : 5,
                    BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text, Font = new Font("Segoe UI", 10f),
                    Location = new Point(352 + col * 104, 572 + row * 52), Size = new Size(84, 24), TextAlign = HorizontalAlignment.Center
                });
                numOffsets[i].ValueChanged += delegate { OffsetChanged(index); };
                fields.Add(numOffsets[i]);
            }
            var reset = AddButton(3, "Remettre à zéro", false, 124, 616, 204, 34);
            reset.Click += delegate { for (int i = 0; i < 6; i++) numOffsets[i].Value = 0; };

            lblBodyStatus = AddLabel(3, "", Theme.Small, Theme.Soft, 106, 694, 350);
            btnBodySave = AddButton(3, "Activer ce mode", true, 464, 682, 200, 44);
            btnBodySave.Click += delegate { SaveBody(); };

            trackerPreview = AddTo(3, new TrackerPreview { Location = new Point(696, 152), Size = new Size(360, 480) });
            var tglBasesAuto = AddTo(3, new ToggleSwitch("Bases automatiques en FBT (connexion / fin)") { Location = new Point(698, 638), Size = new Size(356, 28), Checked = config.BasesAuto });
            tglBasesAuto.CheckedChanged += delegate { config.BasesAuto = tglBasesAuto.Checked; config.Save(); };
            // Base stations : veille ou allumage par Bluetooth, sans être connecté.
            btnBaseOn = AddButton(3, "Allumer les bases", false, 696, 672, 176, 34);
            btnBaseOff = AddButton(3, "Éteindre les bases", false, 880, 672, 176, 34);
            btnBaseOn.Click += delegate { SetBasePower(true); };
            btnBaseOff.Click += delegate { SetBasePower(false); };
            lblBase = AddLabel(3, "Base stations 2.0, par le Bluetooth du PC.", Theme.Small, Theme.Dim, 696, 710, 360);
            previewTimer = new Timer { Interval = 16 };
            previewTimer.Tick += delegate { trackerPreview.Tick(); };
        }

        string SelectedBodyMode { get { return BodyModes[Math.Max(0, segBodyMode.SelectedIndex)]; } }

        // Affiche les réglages du mode choisi ; userChange : on propose les trackers habituels de ce mode.
        void BodyModeChanged(bool userChange)
        {
            string mode = SelectedBodyMode;
            lblBodyModeText.Text = BodyModeTexts[segBodyMode.SelectedIndex];
            bool fbe = mode == "fbe", fbt = mode == "fbt";
            foreach (var t in tglJoints) t.Visible = fbe;
            lblRealTrackers.Visible = fbt;
            if (userChange)
            {
                loadingBody = true;
                if (fbe) SetJoints(BodyConfig.Joints);
                loadingBody = false;
                MarkBodyDirty();
            }
        }

        void SetJoints(params string[] on)
        {
            for (int i = 0; i < BodyConfig.Joints.Length; i++) tglJoints[i].Checked = Array.IndexOf(on, BodyConfig.Joints[i]) >= 0;
        }

        void MarkBodyDirty()
        {
            lblBodyStatus.Text = "Clique sur « Activer ce mode » pour l'appliquer.";
            lblBodyStatus.ForeColor = Theme.Orange;
        }

        // La partie PC d'ALVR compilée pour LunaQuest contient la gestion du corps combiné.
        static bool IsLunaStreamer(string folder)
        {
            try
            {
                string dll = Path.Combine(folder, @"bin\win64\driver_alvr_server.dll");
                return File.Exists(dll) && Encoding.ASCII.GetString(File.ReadAllBytes(dll)).Contains("body.json");
            }
            catch { return false; }
        }

        void LoadBodyUi()
        {
            loadingBody = true;
            body = BodyConfig.Load();
            // Ancien mode FBE Mix (retiré) : on repasse en FBT pour qu'ALVR ne le relance pas.
            if (body.Mode == "mix")
            {
                body.Mode = "fbt";
                foreach (var j in BodyConfig.Joints) body.Enabled[j] = false;
                try { body.Save(); } catch { }
            }
            bool tracking = false;
            try
            {
                if (AlvrInstall.IsValidFolder(config.AlvrFolder))
                    tracking = AlvrSession.Load(AlvrInstall.SessionPath(config.AlvrFolder)).BodyTracking;
            }
            catch { }
            // FBE Unique passe par le suivi du corps d'ALVR ; FBT non.
            string mode = Array.IndexOf(BodyModes, body.Mode) >= 0 ? body.Mode : "fbt";
            if (mode == "fbe" && !tracking) mode = "fbt";
            if (mode != "fbe" && tracking) mode = "fbe";
            segBodyMode.SelectedIndex = Array.IndexOf(BodyModes, mode);
            for (int i = 0; i < BodyConfig.Joints.Length; i++) tglJoints[i].Checked = body.Enabled[BodyConfig.Joints[i]];
            bodyTrackingSaved = tracking;
            bodyModeSaved = mode;
            bodyJointsSaved = body.JointsKey();
            BodyModeChanged(false);
            ShowOffsets();
            loadingBody = false;
            lblBodyModeActive.Text = "Mode actif : " + BodyModeNames[Array.IndexOf(BodyModes, mode)];

            var roles = SteamVrConfig.ReadViveTrackerRoles();
            var sb = new StringBuilder("Vrais trackers Vive (rôles dans SteamVR → Gérer les trackers) :\n");
            if (roles.Count == 0) sb.Append("Aucun rôle réglé. Les jeux comme VRChat s'en passent : ils reconnaissent les trackers à la calibration.\n");
            foreach (var kv in roles) sb.Append(kv.Key).Append(" : ").Append(kv.Value.Replace("TrackerRole_", "")).Append('\n');
            sb.Append("\nL'aperçu à droite montre en direct leur suivi et leur batterie.");
            lblRealTrackers.Text = sb.ToString();

            bool luna = IsLunaStreamer(config.AlvrFolder);
            lblBodyStatus.Text = luna ? "" : "Modes FBE : ALVR version LunaQuest requis.";
            lblBodyStatus.ForeColor = luna ? Theme.Soft : Theme.Orange;
        }

        void ShowOffsets()
        {
            if (cmbOffsetJoint.SelectedIndex < 0) return;
            var o = body.Offsets[BodyConfig.Joints[cmbOffsetJoint.SelectedIndex]];
            bool was = loadingBody;
            loadingBody = true;
            for (int i = 0; i < 6; i++)
            {
                decimal v = (decimal)(i < 3 ? o[i] * 100 : o[i]);
                numOffsets[i].Value = Math.Max(numOffsets[i].Minimum, Math.Min(numOffsets[i].Maximum, v));
            }
            loadingBody = was;
        }

        // Les décalages sont lus en direct par ALVR : on les écrit tout de suite.
        void OffsetChanged(int axis)
        {
            if (loadingBody || cmbOffsetJoint.SelectedIndex < 0) return;
            var o = body.Offsets[BodyConfig.Joints[cmbOffsetJoint.SelectedIndex]];
            o[axis] = axis < 3 ? (double)numOffsets[axis].Value / 100.0 : (double)numOffsets[axis].Value;
            try { body.Save(); lblBodyStatus.Text = "Décalage appliqué."; lblBodyStatus.ForeColor = Theme.Green; }
            catch (Exception e) { lblBodyStatus.Text = "Impossible d'enregistrer : " + e.Message; lblBodyStatus.ForeColor = Theme.Red; }
        }

        void SaveBody()
        {
            string mode = SelectedBodyMode;
            body.Mode = mode;
            body.KneesFromRealTrackers = false;
            for (int i = 0; i < BodyConfig.Joints.Length; i++) body.Enabled[BodyConfig.Joints[i]] = mode != "fbt" && tglJoints[i].Checked;
            try { body.Save(); }
            catch (Exception e) { lblBodyStatus.Text = "Impossible d'enregistrer : " + e.Message; lblBodyStatus.ForeColor = Theme.Red; return; }

            // Changer de mode ou de trackers virtuels demande un redémarrage de SteamVR.
            bool tracking = mode == "fbe";
            if (tracking == bodyTrackingSaved && body.JointsKey() == bodyJointsSaved && mode == bodyModeSaved)
            {
                lblBodyStatus.Text = "Déjà actif.";
                lblBodyStatus.ForeColor = Theme.Green;
                return;
            }
            if (!AlvrInstall.IsValidFolder(config.AlvrFolder)) return;
            bool vrRunning = IsRunning("vrserver");
            bool dashRunning = IsRunning("ALVR Dashboard");
            if ((vrRunning || dashRunning) &&
                MessageBox.Show(this, Lang.T("SteamVR doit redémarrer pour changer de mode.\n\nRedémarrer maintenant ?"),
                    "LunaQuest", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            string path = AlvrInstall.SessionPath(config.AlvrFolder);
            string name = BodyModeNames[Array.IndexOf(BodyModes, mode)];
            RunBusy(() =>
            {
                if (vrRunning || dashRunning) { StopSteamVr(); Log("SteamVR fermé pour changer de mode de suivi du corps."); }
                var s = AlvrSession.Load(path);
                s.BodyTracking = tracking;
                s.Save();
                Log("Mode " + name + " activé.");
                if (vrRunning) { Process.Start("steam://rungameid/250820"); Log("Redémarrage de SteamVR…"); }
            }, () =>
            {
                bodyTrackingSaved = tracking;
                bodyModeSaved = mode;
                bodyJointsSaved = body.JointsKey();
                lblBodyModeActive.Text = "Mode actif : " + name;
                lblBodyStatus.Text = vrRunning ? "Activé, SteamVR redémarre." : "Activé au prochain lancement de SteamVR.";
                lblBodyStatus.ForeColor = Theme.Green;
            });
        }

        // Base stations : la recherche Bluetooth et les commandes prennent quelques secondes.
        void SetBasePower(bool on)
        {
            btnBaseOn.Enabled = btnBaseOff.Enabled = false;
            lblBase.Text = on ? "Recherche et allumage des base stations…" : "Recherche et mise en veille des base stations…";
            lblBase.ForeColor = Theme.Orange;
            Task.Run(() =>
            {
                var lines = new List<string>();
                int done = 0;
                string error = null;
                try { done = BaseStations.SetPower(on, line => { lock (lines) lines.Add(line); Log("Base stations : " + line); }); }
                catch (Exception e) { error = e.GetBaseException().Message; }
                BeginInvoke(new Action(() =>
                {
                    btnBaseOn.Enabled = btnBaseOff.Enabled = true;
                    if (error != null) { lblBase.Text = error; lblBase.ForeColor = Theme.Red; }
                    else if (done == 0) { lblBase.Text = lines.Count > 0 ? lines[lines.Count - 1] : "Aucune base station trouvée."; lblBase.ForeColor = Theme.Orange; }
                    else { lblBase.Text = done + (done > 1 ? " base stations " : " base station ") + (on ? (done > 1 ? "allumées." : "allumée.") : "en veille."); lblBase.ForeColor = Theme.Green; }
                }));
            });
        }

        // Lien SteamVR : écran PC si activé, relevé des appareils tant que la page Corps est ouverte.
        // ---------------------------------------------------------------- Icône près de l'horloge

        // Lancé avec « --tray » (démarrage de Windows) : la fenêtre reste cachée, seule l'icône apparaît.
        protected override void SetVisibleCore(bool value)
        {
            if (startHidden && !IsHandleCreated)
            {
                CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
        }

        void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(Lang.T("Ouvrir LunaQuest"), null, delegate { RestoreFromTray(); });
            menu.Items.Add(Lang.T("Connecter / Déconnecter"), null, delegate { if (connectedState) RunBusy(FullDisconnect); else RunBusy(Connect); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Lang.T("Quitter"), null, delegate { Close(); });
            tray = new NotifyIcon { Icon = Icon, Text = "LunaQuest", ContextMenuStrip = menu, Visible = true };
            // Un second lancement de LunaQuest demande à celui-ci de se montrer.
            var show = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, @"Local\LunaQuest-show");
            new System.Threading.Thread(() =>
            {
                while (show.WaitOne())
                    try { BeginInvoke(new Action(RestoreFromTray)); } catch { return; }
            }) { IsBackground = true, Name = "LunaQuest fenêtre unique" }.Start();
            tray.DoubleClick += delegate { RestoreFromTray(); };
            tray.BalloonTipClicked += delegate { RestoreFromTray(); };
            // Réduire la fenêtre la range près de l'horloge.
            Resize += delegate { if (WindowState == FormWindowState.Minimized) Hide(); };

            lblUpdate = new Label
            {
                AutoSize = false, Size = new Size(470, 24), Location = new Point(440, 86), TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.Small, ForeColor = Theme.ElectricSoft, BackColor = Color.Transparent, Cursor = Cursors.Hand, Visible = false
            };
            Controls.Add(lblUpdate);
            lblUpdate.BringToFront();
        }

        void RestoreFromTray()
        {
            startHidden = false;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        void Notify(string text)
        {
            if (tray != null) tray.ShowBalloonTip(5000, "LunaQuest", Lang.T(text), ToolTipIcon.Info);
        }

        // ---------------------------------------------------------------- Perfs, batteries

        // Chaque seconde : perfs d'ALVR sur la page Connexion ; toutes les 10 s : batteries faibles.
        void StatsTick()
        {
            var s = AlvrLink.Latest;
            string perf = s == null ? (last.SteamVrRunning ? "Perfs : en attente du flux d'ALVR." : "")
                : "Perfs : " + s.ClientFps.ToString("0") + "/" + s.ServerFps.ToString("0") + " im/s · latence " + s.LatencyMs.ToString("0") + " ms · "
                  + s.Mbps.ToString("0") + " Mbit/s · " + (s.LostPerSec == 0 ? "aucune perte" : s.LostPerSec + " pertes/s")
                  + "\nDans le casque : enfonce les deux joysticks en même temps.";
            if (lblPerf.Text != perf) lblPerf.Text = perf;
            lblPerf.ForeColor = s == null ? Theme.Dim : s.LatencyMs < 90 && s.LostPerSec == 0 ? Theme.Soft : Theme.Orange;

            if (++statsTicks % 10 != 0) return;
            var devices = DesktopViewer.Devices;
            if (devices == null) return;
            foreach (var d in devices)
            {
                if (!d.Connected || d.IsVirtualTracker || d.Battery < 0 || d.Class == Valve.VR.ETrackedDeviceClass.TrackingReference) continue;
                int pct = (int)Math.Round(d.Battery * 100);
                string key = d.Serial;
                if (pct >= 25 || d.Charging) { lowBatteryWarned.Remove(key); continue; }
                if (pct > 15 || lowBatteryWarned.Contains(key)) continue;
                lowBatteryWarned.Add(key);
                string text = Lang.T(d.Label) + " : batterie faible (" + pct + " %).";
                Log(text);
                Notify(text);
                desktopViewer.Flash(text);
            }
        }

        // ---------------------------------------------------------------- Mises à jour

        // Au démarrage : y a-t-il une version plus récente sur GitHub ? (aucune donnée envoyée, simple lecture)
        void CheckForUpdate()
        {
            try
            {
                System.Threading.Thread.Sleep(8000);
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/lunaquestld/LunaQuest/releases/latest");
                req.UserAgent = "LunaQuest";
                req.Accept = "application/vnd.github+json";
                req.Timeout = 10000;
                string json;
                using (var resp = req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream())) json = reader.ReadToEnd();
                var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                object o;
                string tag = root != null && root.TryGetValue("tag_name", out o) ? o as string : null;
                string url = root != null && root.TryGetValue("html_url", out o) ? o as string : null;
                Version latest;
                if (tag == null || url == null || !Version.TryParse(tag.TrimStart('v', 'V'), out latest)) return;
                var current = typeof(MainForm).Assembly.GetName().Version;
                if (latest <= new Version(current.Major, current.Minor, Math.Max(0, current.Build))) return;
                BeginInvoke(new Action(() =>
                {
                    string text = "Nouvelle version " + latest + " disponible ›";
                    lblUpdate.Text = Lang.T(text);
                    lblUpdate.Visible = true;
                    lblUpdate.Click += delegate { try { Process.Start(url); } catch { } };
                    Log("LunaQuest " + latest + " est disponible : clique en haut à droite pour la télécharger.");
                    Notify("LunaQuest " + latest + " est disponible.");
                }));
            }
            catch { }
        }

        // ---------------------------------------------------------------- Enregistrement vidéo

        // ALVR enregistre le flux vidéo tel qu'il est envoyé au casque (fichier .h264 / .h265, lisible avec VLC).
        void ToggleRecording()
        {
            if (!recording && !IsRunning("vrserver")) { Log("Connecte-toi d'abord pour filmer."); return; }
            bool start = !recording;
            btnRecord.Enabled = false;
            string folder = config.AlvrFolder;
            Task.Run(() =>
            {
                string error = AlvrLink.Request(start ? "StartRecording" : "StopRecording");
                string saved = null;
                if (error == null && !start) saved = CollectRecording(folder);
                BeginInvoke(new Action(() =>
                {
                    btnRecord.Enabled = true;
                    if (error != null) { Log("Enregistrement impossible : " + error); return; }
                    recording = start;
                    btnRecord.Text = recording ? "Arrêter" : "Filmer";
                    btnRecord.Invalidate();
                    if (recording)
                    {
                        Log("Enregistrement en cours… (environ " + Math.Max(1, CaptureUi().Bitrate * 60 / 8 / 1000.0).ToString("0.#") + " Go par minute)");
                        try { if (AlvrSession.Load(AlvrInstall.SessionPath(folder)).Foveation) Log("La fovéation est activée : les bords de la vidéo seront déformés."); } catch { }
                    }
                    else if (saved != null)
                    {
                        Log("Vidéo enregistrée : " + saved + " (lisible avec VLC).");
                        try { Process.Start("explorer.exe", "/select,\"" + saved + "\""); } catch { }
                    }
                    else Log("Enregistrement arrêté.");
                }));
            });
        }

        // Range la dernière vidéo d'ALVR dans Vidéos\LunaQuest.
        static string CollectRecording(string alvrFolder)
        {
            try
            {
                System.Threading.Thread.Sleep(800); // le temps qu'ALVR ferme le fichier
                FileInfo newest = null;
                foreach (var file in new DirectoryInfo(alvrFolder).GetFiles("recording.*"))
                    if (newest == null || file.LastWriteTimeUtc > newest.LastWriteTimeUtc) newest = file;
                if (newest == null) return null;
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "LunaQuest");
                Directory.CreateDirectory(dir);
                string target = Path.Combine(dir, "LunaQuest " + newest.LastWriteTime.ToString("yyyy-MM-dd HH-mm-ss") + newest.Extension);
                File.Move(newest.FullName, target);
                return target;
            }
            catch { return null; }
        }

        // ---------------------------------------------------------------- Base stations automatiques

        // En FBT, si l'option est cochée : allumées à la connexion, en veille à la déconnexion.
        void AutoBaseStations(bool on)
        {
            if (!config.BasesAuto) return;
            try { if (BodyConfig.Load().Mode != "fbt") return; } catch { return; }
            Log(on ? "Allumage des base stations…" : "Mise en veille des base stations…");
            Task.Run(() =>
            {
                try { BaseStations.SetPower(on, line => Log("Base stations : " + line)); }
                catch (Exception e) { Log("Base stations : " + e.GetBaseException().Message); }
            });
        }

        void UpdateVrLink()
        {
            desktopViewer.ScreenEnabled = config.DesktopViewer;
            desktopViewer.MonitorEnabled = page == 3;
            if (previewTimer != null) previewTimer.Enabled = page == 3;
            // Toujours actif : perfs (deux joysticks) et alertes de batterie dans le casque.
            bool need = true;
            if (need && !desktopViewer.IsRunning) desktopViewer.Start();
            else if (!need && desktopViewer.IsRunning) Task.Run(() => desktopViewer.Stop());
        }

        static readonly string[,] GuideSteps =
        {
            { "Active le mode développeur", "Appli Meta Horizon sur ton téléphone › Appareils › ton casque › Mode développeur." },
            { "Installe SteamVR", "Sur Steam, installe SteamVR (gratuit) et lance-le une fois. La partie PC d'ALVR est déjà incluse dans LunaQuest : rien d'autre à télécharger." },
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
                int x = 132 + col * 484, y = 166 + row * 140;
                var badge = new RectangleF(x, y, 34, 34);
                Theme.Glow(g, new RectangleF(x - 12, y - 12, 58, 58), Theme.Alpha(Theme.Maple, 60));
                using (var b = new LinearGradientBrush(badge, Theme.Maple, Theme.Electric, 45f)) g.FillRectangle(b, badge);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var tb = new SolidBrush(Theme.OnAccent))
                    g.DrawString((i + 1).ToString("00"), Theme.Button, tb, badge, sf);
                using (var tb = new SolidBrush(Theme.Text)) g.DrawString(Lang.T(GuideSteps[i, 0]), Theme.StepTitle, tb, x + 50, y + 4);
                using (var tb = new SolidBrush(Theme.Soft)) g.DrawString(Lang.T(GuideSteps[i, 1]), Theme.Body, tb, new RectangleF(x + 50, y + 32, 400, 96));
            }
        }

        void ShowPage(int index)
        {
            page = index;
            SuspendLayout();
            for (int i = 0; i < pages.Length; i++)
                foreach (var c in pages[i]) c.Visible = i == index;
            ResumeLayout();
            for (int i = 0; i < nav.Length; i++) nav[i].Selected = navPages[i] == index;
            if (index == 1 && !settingsDirty) LoadSettingsUi();
            if (index == 3) LoadBodyUi();
            UpdateVrLink();
            glitchTicks = 7; // petit glitch du titre à chaque changement de page, comme sur le site
            Invalidate();
        }

        // ---------------------------------------------------------------- Décor animé : glitch du titre et feuilles d'érable

        static readonly Rectangle TitleArea = new Rectangle(98, 6, 440, 100);
        readonly Random fx = new Random();
        int glitchTicks, nextGlitch = 90;
        float gx1, gx2, gy1, gy2;

        class FallingLeaf { public float X, Y, Size, Angle, Spin, Speed, Sway; public Color Color; public bool Drawn; public Rectangle Last; }
        readonly List<FallingLeaf> leaves = new List<FallingLeaf>();

        FallingLeaf NewLeaf(bool top)
        {
            bool blue = fx.NextDouble() < .15;
            return new FallingLeaf
            {
                X = SidebarWidth + (float)fx.NextDouble() * (ClientSize.Width - SidebarWidth),
                Y = top ? -30 : (float)fx.NextDouble() * ClientSize.Height,
                Size = 7 + (float)fx.NextDouble() * 10,
                Angle = (float)fx.NextDouble() * 360,
                Spin = ((float)fx.NextDouble() - .5f) * 2.4f,
                Speed = .45f + (float)fx.NextDouble() * .7f,
                Sway = (float)fx.NextDouble() * 6.28f,
                Color = blue ? Theme.Alpha(Theme.Electric, 70 + fx.Next(70)) : Theme.Alpha(Theme.Maple, 70 + fx.Next(80))
            };
        }

        static Rectangle LeafBounds(FallingLeaf l)
        {
            int r = (int)Math.Ceiling(l.Size * 1.1f) + 2;
            return new Rectangle((int)l.X - r, (int)l.Y - r, 2 * r, 2 * r);
        }

        // Une feuille n'est dessinée que sur le fond nu : jamais sur une carte, un contrôle ou la barre de gauche,
        // sinon elle resterait figée dans le fond des contrôles transparents.
        bool LeafVisible(Rectangle r)
        {
            if (r.Left < SidebarWidth + 2) return false;
            foreach (var card in cards[page]) if (card.Bounds.IntersectsWith(r)) return false;
            foreach (Control c in Controls) if (c.Visible && c.Bounds.IntersectsWith(r)) return false;
            return true;
        }

        void Animate()
        {
            if (WindowState == FormWindowState.Minimized) return;
            while (leaves.Count < 12) leaves.Add(NewLeaf(false));
            for (int i = 0; i < leaves.Count; i++)
            {
                var l = leaves[i];
                if (l.Drawn) Invalidate(l.Last);
                l.Sway += .03f;
                l.X += (float)Math.Sin(l.Sway) * .7f;
                l.Y += l.Speed;
                l.Angle += l.Spin;
                if (l.Y > ClientSize.Height + 30) { leaves[i] = l = NewLeaf(true); }
                var r = LeafBounds(l);
                l.Drawn = LeafVisible(r);
                l.Last = r;
                if (l.Drawn) Invalidate(r);
            }

            if (glitchTicks > 0)
            {
                glitchTicks--;
                gx1 = fx.Next(-7, 8); gx2 = fx.Next(-7, 8);
                gy1 = fx.Next(0, 70); gy2 = fx.Next(0, 70);
                Invalidate(TitleArea);
                if (glitchTicks == 0) nextGlitch = 70 + fx.Next(110);
            }
            else if (--nextGlitch <= 0) glitchTicks = 6;
            AnimateScope();
        }

        // ---------------------------------------------------------------- Télescope de la barre de gauche

        static readonly PointF ScopePivot = new PointF(40, 44);
        static readonly RectangleF ScopeArea = new RectangleF(4, 6, 72, 74);
        float scopeAngle = -35f, scopeShown = -35f;

        // Le télescope vise la souris tant qu'elle est dans la fenêtre, sinon la grande lune en haut à droite.
        void AnimateScope()
        {
            var mouse = PointToClient(Cursor.Position);
            PointF target = ClientRectangle.Contains(mouse) ? (PointF)mouse : new PointF(ClientSize.Width - 104, 74);
            float dx = target.X - ScopePivot.X, dy = target.Y - ScopePivot.Y;
            if (dx * dx + dy * dy > 16)
            {
                // Entre « en haut à gauche » et « un peu vers le bas à droite » : il ne vise jamais le sol.
                float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                if (angle > 90) angle -= 360;
                scopeAngle = Math.Max(-165f, Math.Min(35f, angle));
            }
            scopeShown += (scopeAngle - scopeShown) * 0.25f;
            Invalidate(Rectangle.Ceiling(ScopeArea)); // petite zone : redessinée à chaque image pour l'étoile qui scintille
        }

        void PaintScope(Graphics g)
        {
            float px = ScopePivot.X, py = ScopePivot.Y;
            // Trépied.
            using (var pen = new Pen(Color.FromArgb(200, Theme.Soft), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, px, py + 2, px - 15, py + 30);
                g.DrawLine(pen, px, py + 2, px + 15, py + 30);
            }
            using (var pen = new Pen(Color.FromArgb(150, Theme.Dim), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(pen, px, py + 2, px + 2, py + 32);

            // Tube, tourné vers la cible : oculaire à l'arrière, lentille cerclée de rouge à l'avant.
            var state = g.Save();
            g.TranslateTransform(px, py);
            g.RotateTransform(scopeShown);
            var tube = new RectangleF(-12, -5, 30, 10);
            using (var b = new LinearGradientBrush(tube, Color.FromArgb(90, 130, 255), Color.FromArgb(20, 40, 140), 90f)) g.FillRectangle(b, tube);
            using (var pen = new Pen(Color.FromArgb(140, Theme.ElectricSoft), 1f)) g.DrawRectangle(pen, tube.X, tube.Y, tube.Width, tube.Height);
            using (var b = new SolidBrush(Theme.Maple)) g.FillRectangle(b, 16, -6.5f, 5, 13);           // pare-soleil
            using (var b = new SolidBrush(Theme.MoonLight)) g.FillRectangle(b, 21, -5, 1.5f, 10);      // lentille
            using (var b = new SolidBrush(Theme.Soft)) g.FillRectangle(b, -18, -2.5f, 6, 5);           // oculaire
            using (var b = new SolidBrush(Color.FromArgb(120, 255, 255, 255))) g.FillRectangle(b, -10, -4, 24, 2); // reflet
            g.Restore(state);

            // Rotule.
            using (var b = new SolidBrush(Theme.Maple)) g.FillEllipse(b, px - 3.5f, py - 3.5f, 7, 7);
            using (var b = new SolidBrush(Theme.NightTop)) g.FillEllipse(b, px - 1.5f, py - 1.5f, 3, 3);

            // Petite étoile qui scintille au bout de la visée.
            double a = scopeShown * Math.PI / 180;
            float sx = px + (float)Math.Cos(a) * 30, sy = py + (float)Math.Sin(a) * 30;
            int twinkle = 120 + (int)(100 * Math.Abs(Math.Sin(Environment.TickCount / 300.0)));
            Theme.Glow(g, new RectangleF(sx - 5, sy - 5, 10, 10), Theme.Alpha(Theme.MoonLight, twinkle));
        }

        void PaintTitle(Graphics g)
        {
            const float x = 104, y = 10;
            var luna = g.MeasureString("LUNA", Theme.Display, PointF.Empty, StringFormat.GenericTypographic);
            var quest = g.MeasureString("QUEST", Theme.Display, PointF.Empty, StringFormat.GenericTypographic);
            using (var sf = StringFormat.GenericTypographic)
            {
                if (glitchTicks > 0)
                {
                    // Séparation rouge / bleue sur deux bandes horizontales, comme le titre du site.
                    var clip = g.Clip;
                    g.SetClip(new RectangleF(x - 10, y + gy1, 420, 18), CombineMode.Intersect);
                    using (var b = new SolidBrush(Theme.Alpha(Theme.Maple, 200))) { g.DrawString("LUNA", Theme.Display, b, x + gx1, y, sf); g.DrawString("QUEST", Theme.Display, b, x + luna.Width + gx1, y, sf); }
                    g.Clip = clip;
                    g.SetClip(new RectangleF(x - 10, y + gy2, 420, 16), CombineMode.Intersect);
                    using (var b = new SolidBrush(Theme.Alpha(Theme.Electric, 200))) { g.DrawString("LUNA", Theme.Display, b, x + gx2, y, sf); g.DrawString("QUEST", Theme.Display, b, x + luna.Width + gx2, y, sf); }
                    g.Clip = clip;
                }
                using (var b = new SolidBrush(Theme.Text)) g.DrawString("LUNA", Theme.Display, b, x, y, sf);
                using (var b = new SolidBrush(Theme.Maple)) g.DrawString("QUEST", Theme.Display, b, x + luna.Width, y, sf);
            }
            // « by LD » griffonné en rouge, penché, au-dessus de la fin du titre.
            var state = g.Save();
            g.TranslateTransform(x + luna.Width + quest.Width + 6, y - 6);
            g.RotateTransform(12);
            using (var b = new SolidBrush(Theme.Maple)) g.DrawString("by LD", Theme.Tag, b, 0, 0);
            g.Restore(state);
            Theme.DrawSpaced(g, Lang.T("PC VR · META QUEST · CÂBLE USB"), Theme.Eyebrow, Theme.MapleSoft, x + 2, 74, 2.4f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var clip = e.ClipRectangle;
            g.DrawImage(space, clip, clip, GraphicsUnit.Pixel);
            Theme.Smooth(g);
            if (clip.IntersectsWith(Rectangle.Ceiling(ScopeArea))) PaintScope(g);
            if (clip.IntersectsWith(TitleArea)) PaintTitle(g);
            foreach (var l in leaves)
                if (l.Drawn && clip.IntersectsWith(l.Last)) Theme.DrawLeaf(g, l.X, l.Y, l.Size, l.Angle, l.Color);
            foreach (var card in cards[page])
            {
                if (!clip.IntersectsWith(card.Bounds)) continue;
                var r = new RectangleF(card.Bounds.X + .5f, card.Bounds.Y + .5f, card.Bounds.Width - 1, card.Bounds.Height - 1);
                using (var b = new SolidBrush(Theme.CardFill)) g.FillRectangle(b, r);
                using (var pen = new Pen(Theme.CardBorder, 1f)) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                // Titre de carte façon « eyebrow » du site, souligné d'un trait rouge vers bleu.
                Theme.DrawSpaced(g, card.Title.ToUpperInvariant(), Theme.CardTitle, Theme.MapleSoft, card.Bounds.X + 20, card.Bounds.Y + 14, 2f);
                var bar = new RectangleF(card.Bounds.X + 20, card.Bounds.Y + 32, 46, 2);
                using (var b = new LinearGradientBrush(new RectangleF(bar.X, bar.Y, bar.Width + 1, bar.Height), Theme.Maple, Theme.Electric, 0f)) g.FillRectangle(b, bar);
            }
            foreach (var f in fields)
            {
                if (!f.Visible) continue;
                var r = new RectangleF(f.Left - 10, f.Top - 7, f.Width + 20, f.Height + 14);
                if (!clip.IntersectsWith(Rectangle.Ceiling(r))) continue;
                using (var b = new SolidBrush(Theme.Field)) g.FillRectangle(b, r);
                using (var pen = new Pen(Theme.Alpha(Theme.ElectricSoft, 60), 1f)) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
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
            log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + Lang.T(msg) + Environment.NewLine);
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

            // Les bases mettent quelques secondes à démarrer : on les allume en premier.
            AutoBaseStations(true);
            try { AlvrLink.Port = AlvrSession.Load(AlvrInstall.SessionPath(s.AlvrFolder)).WebServerPort; } catch { }

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
                    if (SteamVrConfig.UnblockDriver("alvr_server"))
                        Log("ALVR avait été bloqué par SteamVR après un plantage : réactivé.");
                }
                catch (Exception e) { Log("Impossible de réactiver ALVR dans SteamVR : " + e.Message); }
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
                    // Les manettes du casque branché, avec les gestes de base dans les jeux.
                    if (session.MatchHeadset(s.Model))
                    {
                        changed = true;
                        Log("Manettes réglées sur celles du " + s.Model + ".");
                    }
                    if (changed) session.Save();
                }
                catch (Exception e) { Log("Config ALVR illisible : " + e.Message); }
            }
            if (!s.HasWiredClient && s.AlvrPackage != LunaClientPackage) Log("Attention : aucun casque enregistré pour le câble. Va dans Réglages ALVR › Casque filaire.");
            // SteamVR déjà lancé : la config d'ALVR ne peut changer qu'au prochain lancement.
            if (s.SteamVrRunning)
                try
                {
                    if (AlvrSession.Load(AlvrInstall.SessionPath(s.AlvrFolder)).MatchHeadset(s.Model))
                        Log("Les manettes du " + s.Model + " seront utilisées au prochain lancement de SteamVR (Relancer SteamVR).");
                }
                catch { }

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
            if (recording)
            {
                AlvrLink.Request("StopRecording");
                string saved = CollectRecording(config.AlvrFolder);
                BeginInvoke(new Action(() => { recording = false; btnRecord.Text = "Filmer"; btnRecord.Invalidate(); }));
                if (saved != null) Log("Vidéo enregistrée : " + saved + ".");
            }
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
            AutoBaseStations(false);
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
                MessageBox.Show(this, Lang.T("VB-CABLE n'est pas installé : avec le micro activé, ALVR refusera de se connecter.\n\nEnregistrer quand même ?"),
                    "LunaQuest", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // SteamVR et ALVR Dashboard réécrivent la config : il faut les fermer avant.
            bool vrRunning = IsRunning("vrserver");
            bool dashRunning = IsRunning("ALVR Dashboard");
            if ((vrRunning || dashRunning) &&
                MessageBox.Show(this, Lang.T("SteamVR doit redémarrer pour appliquer les réglages.\n\nRedémarrer maintenant ?"),
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
                if (s.SuperSampling)
                    Log("Résolution au-delà de l'écran du casque : filtre de réduction « super sampling » de Meta activé.");
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
