// Animation de démarrage (un sabre lumineux tranche l'écran en diagonale, les deux moitiés s'écartent sur
// l'appli) et tutoriel de découverte (chaque partie de l'appli mise en surbrillance, page après page).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace LunaQuest
{
    // ================================================================ Animation du sabre

    class IntroOverlay : Control
    {
        const double LogoIn = 0.55, SlashEnd = 0.95, FlashEnd = 1.15, SplitEnd = 1.95;

        readonly MainForm form;
        readonly Timer timer = new Timer { Interval = 15 };
        readonly Stopwatch clock = new Stopwatch();
        readonly Random rnd = new Random();
        Bitmap cover, app;
        bool done;

        class Spark { public PointF P, V; public float Life; }
        readonly List<Spark> sparks = new List<Spark>();

        public event EventHandler Finished;

        public IntroOverlay(MainForm form)
        {
            this.form = form;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            Cursor = Cursors.Hand;
            timer.Tick += delegate { Step(); };
        }

        public void Start()
        {
            cover = RenderCover(form.ClientSize);
            clock.Start();
            timer.Start();
        }

        // Un clic passe l'animation.
        protected override void OnClick(EventArgs e) { Finish(); base.OnClick(e); }

        void Finish()
        {
            if (done) return;
            done = true;
            timer.Stop();
            if (Finished != null) Finished(this, EventArgs.Empty);
        }

        double T { get { return clock.Elapsed.TotalSeconds; } }

        void Step()
        {
            double t = T;
            // L'image de l'appli est prise juste avant que l'écran s'ouvre.
            if (app == null && t >= FlashEnd - 0.1) app = form.SnapshotClient();
            if (t >= SlashEnd - 0.4 && t < SlashEnd)
            {
                var tip = Along((t - (SlashEnd - 0.4)) / 0.4);
                for (int i = 0; i < 6; i++)
                {
                    double a = rnd.NextDouble() * Math.PI * 2;
                    float speed = 2 + (float)rnd.NextDouble() * 9;
                    sparks.Add(new Spark { P = tip, V = new PointF((float)Math.Cos(a) * speed, (float)Math.Sin(a) * speed), Life = 1 });
                }
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                s.P = new PointF(s.P.X + s.V.X, s.P.Y + s.V.Y);
                s.V = new PointF(s.V.X * 0.93f, s.V.Y * 0.93f + 0.25f);
                s.Life -= 0.045f;
                if (s.Life <= 0) sparks.RemoveAt(i);
            }
            if (t >= SplitEnd) { Finish(); return; }
            Invalidate();
        }

        // Point de la diagonale (du coin haut gauche au coin bas droit, un peu au-delà), t de 0 à 1.
        PointF Along(double t)
        {
            float w = Width, h = Height;
            return new PointF((float)(-0.08 * w + t * 1.16 * w), (float)(-0.08 * h + t * 1.16 * h));
        }

        static double Ease(double t) { t = Math.Max(0, Math.Min(1, t)); return t * t * t; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            double t = T;
            float w = Width, h = Height;
            Color blade = Theme.Maple;

            if (t < FlashEnd || app == null)
            {
                g.DrawImage(cover, 0, 0);
                // Logo qui apparaît doucement.
                if (t < LogoIn)
                    using (var b = new SolidBrush(Color.FromArgb((int)(255 * (1 - t / LogoIn)), Theme.NightTop))) g.FillRectangle(b, 0, 0, w, h);
                // Le sabre traverse l'écran : lame blanche, halo de la couleur du thème, traînée.
                if (t >= SlashEnd - 0.4 && t < FlashEnd)
                {
                    double p = Math.Min(1, (t - (SlashEnd - 0.4)) / 0.4);
                    var tip = Along(p);
                    var tail = Along(Math.Max(0, p - 0.45));
                    if (t >= SlashEnd) tail = Along(0);
                    float fade = t >= SlashEnd ? (float)(1 - (t - SlashEnd) / (FlashEnd - SlashEnd)) : 1f;
                    DrawBlade(g, tail, tip, blade, fade);
                }
                foreach (var s in sparks)
                    using (var b = new SolidBrush(Color.FromArgb((int)(255 * Math.Max(0, s.Life)), Color.White)))
                        g.FillEllipse(b, s.P.X - 1.6f, s.P.Y - 1.6f, 3.2f, 3.2f);
                // Éclair au moment de la coupe.
                if (t >= SlashEnd && t < FlashEnd)
                {
                    int a = (int)(110 * (1 - (t - SlashEnd) / (FlashEnd - SlashEnd)));
                    using (var b = new SolidBrush(Color.FromArgb(a, Color.White))) g.FillRectangle(b, 0, 0, w, h);
                }
                return;
            }

            // Les deux moitiés s'écartent perpendiculairement à la coupe et dévoilent l'appli.
            g.DrawImage(app, 0, 0);
            double k = Ease((t - FlashEnd) / (SplitEnd - FlashEnd));
            float len = (float)Math.Sqrt(w * w + h * h);
            float nx = h / len, ny = -w / len; // vers le haut à droite
            float d = (float)(k * len * 0.75);
            var upper = new[] { new PointF(0, 0), new PointF(w, 0), new PointF(w, h) };
            var lower = new[] { new PointF(0, 0), new PointF(0, h), new PointF(w, h) };
            DrawHalf(g, upper, nx * d, ny * d, blade);
            DrawHalf(g, lower, -nx * d, -ny * d, blade);
        }

        void DrawHalf(Graphics g, PointF[] tri, float dx, float dy, Color blade)
        {
            var state = g.Save();
            g.TranslateTransform(dx, dy);
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(tri);
                g.SetClip(path);
                g.DrawImage(cover, 0, 0);
                g.ResetClip();
            }
            // Bord brûlant le long de la coupe.
            using (var glow = new Pen(Color.FromArgb(110, blade), 9f))
                g.DrawLine(glow, 0, 0, Width, Height);
            using (var pen = new Pen(Color.FromArgb(230, Color.White), 2f))
                g.DrawLine(pen, 0, 0, Width, Height);
            g.Restore(state);
        }

        static void DrawBlade(Graphics g, PointF a, PointF b, Color color, float fade)
        {
            foreach (var layer in new[] { new { W = 46f, A = 40 }, new { W = 26f, A = 80 }, new { W = 13f, A = 170 } })
                using (var pen = new Pen(Color.FromArgb((int)(layer.A * fade), color), layer.W) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(pen, a, b);
            using (var core = new Pen(Color.FromArgb((int)(255 * fade), Color.White), 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(core, a, b);
            Theme.Glow(g, new RectangleF(b.X - 40, b.Y - 40, 80, 80), Color.FromArgb((int)(200 * fade), Color.White));
        }

        // Écran de départ : la nuit du thème, des étoiles et le logo.
        static Bitmap RenderCover(Size size)
        {
            var bmp = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height));
            using (var g = Graphics.FromImage(bmp))
            {
                Theme.Smooth(g);
                var full = new Rectangle(Point.Empty, size);
                using (var b = new LinearGradientBrush(full, Theme.NightTop, Theme.NightBottom, 80f)) g.FillRectangle(b, full);
                Theme.Glow(g, new RectangleF(-200, size.Height - 380, 700, 560), Theme.Alpha(Theme.Maple, 40));
                Theme.Glow(g, new RectangleF(size.Width - 560, -220, 720, 520), Theme.Alpha(Theme.Electric, 46));
                var rnd = new Random(11);
                for (int i = 0; i < 220; i++)
                {
                    float x = (float)rnd.NextDouble() * size.Width, y = (float)rnd.NextDouble() * size.Height;
                    float s = (float)(0.6 + Math.Pow(rnd.NextDouble(), 3) * 2.2);
                    using (var br = new SolidBrush(Theme.Alpha(Theme.IsLight ? Theme.Soft : Theme.Text, 60 + rnd.Next(150)))) g.FillEllipse(br, x, y, s, s);
                }
                using (var big = new Font("Impact", 76f, GraphicsUnit.Pixel))
                using (var tag = new Font("Segoe Print", 20f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var sf = StringFormat.GenericTypographic)
                {
                    var luna = g.MeasureString("LUNA", big, PointF.Empty, sf);
                    var quest = g.MeasureString("QUEST", big, PointF.Empty, sf);
                    float x = (size.Width - luna.Width - quest.Width) / 2f, y = size.Height / 2f - luna.Height / 2f - 10;
                    Theme.Glow(g, new RectangleF(x - 60, y - 50, luna.Width + quest.Width + 120, luna.Height + 100), Theme.Alpha(Theme.Maple, 50));
                    using (var b = new SolidBrush(Theme.Text)) g.DrawString("LUNA", big, b, x, y, sf);
                    using (var b = new SolidBrush(Theme.Maple)) g.DrawString("QUEST", big, b, x + luna.Width, y, sf);
                    var state = g.Save();
                    g.TranslateTransform(x + luna.Width + quest.Width + 8, y - 10);
                    g.RotateTransform(12);
                    using (var b = new SolidBrush(Theme.Maple)) g.DrawString("by LD", tag, b, 0, 0);
                    g.Restore(state);
                }
            }
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                if (cover != null) cover.Dispose();
                if (app != null) app.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ================================================================ Tutoriel guidé

    class TourStep
    {
        public int Page; public Rectangle Area; public string Title, Text;
        public TourStep(int page, Rectangle area, string title, string text) { Page = page; Area = area; Title = title; Text = text; }
    }

    class TutorialOverlay : Control
    {
        const double StepSeconds = 6.5;

        readonly MainForm form;
        readonly List<TourStep> steps;
        readonly Timer timer = new Timer { Interval = 15 };
        readonly Stopwatch stepClock = new Stopwatch();
        int index = -1;
        Bitmap shot, previous;
        RectangleF spot, spotFrom;
        float fade = 1f;
        RectangleF bubble, skipButton, nextButton;
        bool done;

        public event EventHandler Finished;

        public TutorialOverlay(MainForm form, List<TourStep> steps)
        {
            this.form = form;
            this.steps = steps;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            timer.Tick += delegate { Tick(); };
        }

        public void Start()
        {
            Go(0);
            timer.Start();
            Focus();
        }

        void Go(int i)
        {
            if (i >= steps.Count) { Finish(); return; }
            index = i;
            // Change de page derrière le calque, puis prend l'image de la nouvelle page.
            form.ShowPageForTour(steps[i].Page);
            if (previous != null) previous.Dispose();
            previous = shot;
            shot = form.SnapshotClient();
            fade = previous == null ? 1f : 0f;
            spotFrom = spot.IsEmpty ? Target(steps[i]) : spot;
            stepClock.Restart();
            Invalidate();
        }

        // Zone à mettre en avant ; une étape sans zone met la bulle au centre.
        RectangleF Target(TourStep s)
        {
            if (s.Area.IsEmpty) return new RectangleF(Width / 2f, Height / 2f, 0, 0);
            return new RectangleF(s.Area.X - 6, s.Area.Y - 6, s.Area.Width + 12, s.Area.Height + 12);
        }

        void Finish()
        {
            if (done) return;
            done = true;
            timer.Stop();
            if (Finished != null) Finished(this, EventArgs.Empty);
        }

        void Tick()
        {
            if (done || index < 0) return;
            var target = Target(steps[index]);
            float k = (float)Math.Min(1, stepClock.Elapsed.TotalSeconds / 0.45);
            k = 1 - (1 - k) * (1 - k) * (1 - k);
            spot = new RectangleF(spotFrom.X + (target.X - spotFrom.X) * k, spotFrom.Y + (target.Y - spotFrom.Y) * k,
                                  spotFrom.Width + (target.Width - spotFrom.Width) * k, spotFrom.Height + (target.Height - spotFrom.Height) * k);
            fade = Math.Min(1f, fade + 0.08f);
            if (stepClock.Elapsed.TotalSeconds >= StepSeconds) { Go(index + 1); return; }
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (skipButton.Contains(e.Location)) Finish();
            else Go(index + 1);
            base.OnMouseClick(e);
        }

        protected override bool IsInputKey(Keys keyData) { return true; }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Finish();
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Go(index + 1);
            else if (e.KeyCode == Keys.Left && index > 0) Go(index - 1);
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            if (index < 0 || shot == null) return;
            var step = steps[index];

            // Fondu entre l'ancienne page et la nouvelle.
            if (previous != null && fade < 1f) g.DrawImage(previous, 0, 0);
            if (fade >= 1f || previous == null) g.DrawImage(shot, 0, 0);
            else
                using (var attrs = new ImageAttributes())
                {
                    attrs.SetColorMatrix(new ColorMatrix { Matrix33 = fade });
                    g.DrawImage(shot, new Rectangle(0, 0, shot.Width, shot.Height), 0, 0, shot.Width, shot.Height, GraphicsUnit.Pixel, attrs);
                }

            // Tout s'assombrit sauf la zone expliquée, entourée d'un trait lumineux.
            bool centered = spot.Width < 2;
            using (var region = new Region(ClientRectangle))
            {
                if (!centered)
                    using (var hole = Theme.Round(spot, 10)) region.Exclude(hole);
                using (var b = new SolidBrush(Color.FromArgb(centered ? 200 : 178, 0, 0, 0))) g.FillRegion(b, region);
            }
            if (!centered)
            {
                Theme.Glow(g, RectangleF.Inflate(spot, 30, 30), Theme.Alpha(Theme.Maple, 30));
                using (var path = Theme.Round(spot, 10))
                using (var brush = new LinearGradientBrush(RectangleF.Inflate(spot, 2, 2), Theme.Maple, Theme.Electric, 45f))
                using (var pen = new Pen(brush, 3f))
                    g.DrawPath(pen, path);
            }

            PaintBubble(g, step, centered);
        }

        void PaintBubble(Graphics g, TourStep step, bool centered)
        {
            const float bw = 420;
            string title = Lang.T(step.Title), text = Lang.T(step.Text);
            float textH = g.MeasureString(text, Theme.Body, (int)(bw - 40)).Height;
            float bh = 98 + textH + 52;
            float x, y;
            if (centered) { x = (Width - bw) / 2f; y = (Height - bh) / 2f; }
            else
            {
                // À droite, à gauche, en dessous ou au-dessus de la zone : là où il y a de la place.
                if (spot.Right + 20 + bw <= Width - 10) { x = spot.Right + 20; y = spot.Y; }
                else if (spot.X - 20 - bw >= 90) { x = spot.X - 20 - bw; y = spot.Y; }
                else if (spot.Bottom + 20 + bh <= Height - 10) { x = spot.X; y = spot.Bottom + 20; }
                else { x = spot.X; y = spot.Y - 20 - bh; }
                x = Math.Max(90, Math.Min(Width - bw - 10, x));
                y = Math.Max(10, Math.Min(Height - bh - 10, y));
            }
            bubble = new RectangleF(x, y, bw, bh);

            using (var path = Theme.Round(bubble, 12))
            {
                using (var b = new SolidBrush(Color.FromArgb(245, Theme.NightBottom))) g.FillPath(b, path);
                using (var pen = new Pen(Theme.Alpha(Theme.ElectricSoft, 160), 1.5f)) g.DrawPath(pen, path);
            }
            Theme.DrawSpaced(g, (index + 1) + " / " + steps.Count, Theme.CardTitle, Theme.MapleSoft, x + 20, y + 18, 2f);
            using (var b = new LinearGradientBrush(new RectangleF(x + 20, y + 38, 47, 2), Theme.Maple, Theme.Electric, 0f)) g.FillRectangle(b, x + 20, y + 38, 46, 2);
            using (var b = new SolidBrush(Theme.Text)) g.DrawString(title, Theme.StepTitle, b, x + 20, y + 50);
            using (var b = new SolidBrush(Theme.Soft)) g.DrawString(text, Theme.Body, b, new RectangleF(x + 20, y + 84, bw - 40, textH + 4));

            // Boutons : passer le tutoriel, ou étape suivante.
            skipButton = new RectangleF(x + 20, y + bh - 50, 120, 34);
            nextButton = new RectangleF(x + bw - 160, y + bh - 50, 140, 34);
            using (var b = new SolidBrush(Theme.Alpha(Theme.Electric, 56))) g.FillRectangle(b, skipButton);
            using (var pen = new Pen(Theme.Alpha(Theme.ElectricSoft, 105), 1f)) g.DrawRectangle(pen, skipButton.X, skipButton.Y, skipButton.Width, skipButton.Height);
            using (var b = new SolidBrush(Theme.Maple)) g.FillRectangle(b, nextButton);
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                using (var b = new SolidBrush(Theme.Text)) g.DrawString(Lang.T("Passer").ToUpperInvariant(), Theme.Button, b, skipButton, sf);
                string next = index == steps.Count - 1 ? Lang.T("C'est parti !") : Lang.T("Suivant") + " ›";
                using (var b = new SolidBrush(Theme.OnAccent)) g.DrawString(next.ToUpperInvariant(), Theme.Button, b, nextButton, sf);
            }
            // Avancement automatique : fine barre sous la bulle.
            float p = (float)Math.Min(1, stepClock.Elapsed.TotalSeconds / StepSeconds);
            using (var b = new SolidBrush(Theme.Alpha(Theme.Electric, 60))) g.FillRectangle(b, x + 12, y + bh - 6, bw - 24, 3);
            using (var b = new SolidBrush(Theme.Maple)) g.FillRectangle(b, x + 12, y + bh - 6, (bw - 24) * p, 3);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                if (shot != null) shot.Dispose();
                if (previous != null) previous.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
