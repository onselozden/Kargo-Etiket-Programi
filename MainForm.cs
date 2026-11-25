using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace KargoEtiket
{
    public class MainForm : Form
    {
        Veri V;
        ComboBox cbGonderen, cbAlici, cbKargo, cbOdeme, cbYazici;
        TextBox tbKod, tbNot;
        CheckBox chKirilir;
        RadioButton rbTermal, rbA5, rbA4;
        NumericUpDown nuKopya;
        OnizlemePanel onizleme;
        bool yukleniyor;

        public MainForm()
        {
            V = Veri.Yukle();
            Text = "Kargo Etiket Programı";
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1000, 680);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var sol = new Panel { Dock = DockStyle.Left, Width = 400, Padding = new Padding(12) };
            onizleme = new OnizlemePanel { Dock = DockStyle.Fill };
            Controls.Add(onizleme);
            Controls.Add(sol);
            var menu = new MenuStrip();
            var mTema = new ToolStripMenuItem("Tema");
            var mAcik = new ToolStripMenuItem("Açık", null, (s, e) => TemaSec("ACIK"));
            var mKoyu = new ToolStripMenuItem("Koyu", null, (s, e) => TemaSec("KOYU"));
            mTema.DropDownItems.Add(mAcik); mTema.DropDownItems.Add(mKoyu);
            mTema.DropDownOpening += (s, e) => { mAcik.Checked = V.Ayarlar.Tema != "KOYU"; mKoyu.Checked = V.Ayarlar.Tema == "KOYU"; };
            menu.Items.Add(mTema);
            menu.Items.Add(new ToolStripMenuItem("Hakkında", null, (s, e) => Hakkinda()));
            MainMenuStrip = menu;
            Controls.Add(menu);

            int y = 10; const int L = 12, W = 370;
            Func<string, Label> Lbl = t => { var l = new Label { Text = t, Left = L, Top = y, AutoSize = true, Font = new Font(Font, FontStyle.Bold) }; sol.Controls.Add(l); y += 22; return l; };
            Func<string, int, int, EventHandler, Button> Btn = (t, x, w, h) => { var b = new Button { Text = t, Left = x, Top = y, Width = w, Height = 28 }; b.Click += h; sol.Controls.Add(b); return b; };

            Lbl("Gönderen");
            cbGonderen = new ComboBox { Left = L, Top = y, Width = W, DropDownStyle = ComboBoxStyle.DropDownList };
            sol.Controls.Add(cbGonderen); y += 30;
            Btn("Yeni", L, 80, (s, e) => GonderenDuzenle(null));
            Btn("Düzenle", L + 86, 80, (s, e) => GonderenDuzenle(cbGonderen.SelectedItem as Gonderen));
            Btn("Sil", L + 172, 80, (s, e) => GonderenSil());
            y += 40;

            Lbl("Alıcı");
            cbAlici = new ComboBox { Left = L, Top = y, Width = W, DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 20 };
            sol.Controls.Add(cbAlici); y += 30;
            Btn("Yeni", L, 80, (s, e) => AliciDuzenle(null));
            Btn("Düzenle", L + 86, 80, (s, e) => AliciDuzenle(cbAlici.SelectedItem as Alici));
            Btn("Sil", L + 172, 80, (s, e) => AliciSil());
            y += 40;

            Lbl("Kargo firması");
            cbKargo = KargoCombo(L, y, W); sol.Controls.Add(cbKargo); y += 40;

            Lbl("Ödeme / Anlaşma kodu");
            cbOdeme = new ComboBox { Left = L, Top = y, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cbOdeme.Items.AddRange(Odeme.Adlar);
            tbKod = new TextBox { Left = L + 188, Top = y, Width = W - 188 };
            sol.Controls.Add(cbOdeme); sol.Controls.Add(tbKod); y += 30;
            Btn("Kargo / ödeme / kodu bu alıcıya kaydet", L, W, (s, e) => AliciyaKaydet());
            y += 40;

            Lbl("Gönderi notu (takip no, ürün, seri no, arıza...)");
            tbNot = new TextBox { Left = L, Top = y, Width = W, Height = 70, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            sol.Controls.Add(tbNot); y += 78;

            chKirilir = new CheckBox { Text = "Dikkat Kırılır", Left = L, Top = y, AutoSize = true, Checked = true, Font = new Font(Font, FontStyle.Bold) };
            sol.Controls.Add(chKirilir); y += 32;

            Lbl("Çıktı boyutu");
            rbTermal = new RadioButton { Text = "70 mm Termal", Left = L, Top = y, Width = 130 };
            rbA5 = new RadioButton { Text = "A5", Left = L + 150, Top = y, Width = 60 };
            rbA4 = new RadioButton { Text = "A4", Left = L + 220, Top = y, Width = 60 };
            sol.Controls.AddRange(new Control[] { rbTermal, rbA5, rbA4 }); y += 30;

            Lbl("Yazıcı");
            cbYazici = new ComboBox { Left = L, Top = y, Width = W, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string p in PrinterSettings.InstalledPrinters) cbYazici.Items.Add(p);
            sol.Controls.Add(cbYazici); y += 32;

            sol.Controls.Add(new Label { Text = "Kopya:", Left = L, Top = y + 4, AutoSize = true });
            nuKopya = new NumericUpDown { Left = L + 55, Top = y, Width = 60, Minimum = 1, Maximum = 20, Value = 1 };
            sol.Controls.Add(nuKopya);
            Btn("Önizleme", L + 130, 100, (s, e) => Yazdir(true));
            var bY = Btn("YAZDIR", L + 236, W - 236, (s, e) => Yazdir(false));
            bY.Font = new Font(Font, FontStyle.Bold); bY.Height = 34; bY.BackColor = Color.FromArgb(17, 76, 160); bY.ForeColor = Color.White; bY.FlatStyle = FlatStyle.Flat; bY.Tag = "sabit";
            y += 44;
            sol.Controls.Add(new Label { Text = "Veri: " + Veri.Yol, Left = L, Top = y, Width = W, Height = 34, ForeColor = Color.Gray, Font = new Font(Font.FontFamily, 7.5f) });

            // olaylar
            cbGonderen.SelectedIndexChanged += (s, e) => Yenile();
            cbAlici.SelectedIndexChanged += (s, e) => AliciSecildi();
            cbKargo.SelectedIndexChanged += (s, e) => Yenile();
            cbOdeme.SelectedIndexChanged += (s, e) => Yenile();
            tbKod.TextChanged += (s, e) => Yenile();
            tbNot.TextChanged += (s, e) => Yenile();
            chKirilir.CheckedChanged += (s, e) => Yenile();
            EventHandler bd = (s, e) => { if (((RadioButton)s).Checked) BoyutDegisti(); };
            rbTermal.CheckedChanged += bd; rbA5.CheckedChanged += bd; rbA4.CheckedChanged += bd;
            cbYazici.SelectedIndexChanged += (s, e) => YaziciDegisti();

            GonderenleriDoldur(V.Gonderenler.FirstOrDefault(g => g.Varsayilan));
            AlicilariDoldur(null);
            yukleniyor = true;
            (V.Ayarlar.Boyut == "A4" ? rbA4 : V.Ayarlar.Boyut == "A5" ? rbA5 : rbTermal).Checked = true;
            yukleniyor = false;
            BoyutDegisti();
            Tema.Koyu = V.Ayarlar.Tema == "KOYU";
            Tema.Uygula(this);
        }

        void TemaSec(string t)
        {
            V.Ayarlar.Tema = t; V.Kaydet();
            Tema.Koyu = t == "KOYU";
            Tema.Uygula(this);
        }

        void Hakkinda()
        {
            using (var f = new HakkindaForm()) f.ShowDialog(this);
        }

        ComboBox KargoCombo(int x, int y, int w)
        {
            var cb = new ComboBox { Left = x, Top = y, Width = w, DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 30, MaxDropDownItems = 12 };
            cb.Items.Add("(Seçilmedi)");
            foreach (var k in Kargo.Liste) cb.Items.Add(k);
            cb.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0) return;
                var it = cb.Items[e.Index];
                int tx = e.Bounds.Left + 4;
                if (it is Kargo k && k.Logo != null)
                {
                    float sc = Math.Min(70f / k.Logo.Width, (e.Bounds.Height - 4f) / k.Logo.Height);
                    int lw = (int)(k.Logo.Width * sc), lh = (int)(k.Logo.Height * sc);
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(k.Logo, e.Bounds.Left + 4 + (70 - lw) / 2, e.Bounds.Top + (e.Bounds.Height - lh) / 2, lw, lh);
                    tx += 80;
                }
                TextRenderer.DrawText(e.Graphics, it.ToString(), cb.Font, new Rectangle(tx, e.Bounds.Top, e.Bounds.Width - tx, e.Bounds.Height), e.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                e.DrawFocusRectangle();
            };
            return cb;
        }

        public static void KargoSec(ComboBox cb, string kod)
        {
            for (int i = 0; i < cb.Items.Count; i++) if (cb.Items[i] is Kargo k && k.Kod == kod) { cb.SelectedIndex = i; return; }
            cb.SelectedIndex = 0;
        }
        public static string KargoKod(ComboBox cb) => (cb.SelectedItem as Kargo)?.Kod ?? "";

        string Boyut => rbA4.Checked ? "A4" : rbA5.Checked ? "A5" : "TERMAL";

        void GonderenleriDoldur(Gonderen sec)
        {
            cbGonderen.Items.Clear();
            foreach (var g in V.Gonderenler) cbGonderen.Items.Add(g);
            if (sec != null) cbGonderen.SelectedItem = sec;
            if (cbGonderen.SelectedIndex < 0 && cbGonderen.Items.Count > 0) cbGonderen.SelectedIndex = 0;
        }

        void AlicilariDoldur(Alici sec)
        {
            cbAlici.Items.Clear();
            foreach (var a in V.Alicilar.OrderBy(a => a.Ad, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true))) cbAlici.Items.Add(a);
            if (sec != null) cbAlici.SelectedItem = sec;
            else if (cbAlici.Items.Count > 0) cbAlici.SelectedIndex = 0;
            AliciSecildi();
        }

        void AliciSecildi()
        {
            var a = cbAlici.SelectedItem as Alici;
            yukleniyor = true;
            if (a != null)
            {
                KargoSec(cbKargo, a.Kargo);
                cbOdeme.SelectedIndex = Math.Max(0, Array.IndexOf(Odeme.Kodlar, a.Odeme ?? ""));
                tbKod.Text = a.AnlasmaKodu;
            }
            yukleniyor = false;
            Yenile();
        }

        Etiket MevcutEtiket() => new Etiket
        {
            Gonderen = cbGonderen.SelectedItem as Gonderen,
            Alici = cbAlici.SelectedItem as Alici,
            Kargo = KargoKod(cbKargo),
            Odeme = cbOdeme.SelectedIndex >= 0 ? Odeme.Kodlar[cbOdeme.SelectedIndex] : "",
            Kod = tbKod.Text,
            Not = tbNot.Text,
            Kirilir = chKirilir.Checked
        };

        void Yenile()
        {
            if (yukleniyor) return;
            onizleme.Ayarla(MevcutEtiket(), Boyut, V.Ayarlar.TermalGenislik);
        }

        void BoyutDegisti()
        {
            if (yukleniyor) return;
            V.Ayarlar.Boyut = Boyut; V.Kaydet();
            string kayitli = Boyut == "A4" ? V.Ayarlar.YaziciA4 : Boyut == "A5" ? V.Ayarlar.YaziciA5 : V.Ayarlar.YaziciTermal;
            yukleniyor = true;
            int idx = cbYazici.Items.IndexOf(kayitli ?? "");
            if (idx < 0 && Boyut == "TERMAL")
                for (int i = 0; i < cbYazici.Items.Count; i++)
                { string n = cbYazici.Items[i].ToString().ToUpperInvariant(); if (n.Contains("XP-80") || n.Contains("XP80") || n.Contains("80C") || n.Contains("POS")) { idx = i; break; } }
            if (idx < 0) idx = cbYazici.Items.IndexOf(new PrinterSettings().PrinterName);
            cbYazici.SelectedIndex = idx < 0 && cbYazici.Items.Count > 0 ? 0 : idx;
            yukleniyor = false;
            Yenile();
        }

        void YaziciDegisti()
        {
            if (yukleniyor) return;
            string p = cbYazici.SelectedItem as string ?? "";
            if (Boyut == "A4") V.Ayarlar.YaziciA4 = p; else if (Boyut == "A5") V.Ayarlar.YaziciA5 = p; else V.Ayarlar.YaziciTermal = p;
            V.Kaydet();
        }

        void AliciyaKaydet()
        {
            if (!(cbAlici.SelectedItem is Alici a)) return;
            a.Kargo = KargoKod(cbKargo);
            a.Odeme = cbOdeme.SelectedIndex >= 0 ? Odeme.Kodlar[cbOdeme.SelectedIndex] : "";
            a.AnlasmaKodu = tbKod.Text.Trim();
            V.Kaydet();
            MessageBox.Show(a.Ad + " için kaydedildi.", "Kargo Etiket Programı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void GonderenDuzenle(Gonderen g)
        {
            bool yeni = g == null;
            if (yeni) g = new Gonderen();
            using (var f = new GonderenForm(g))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (yeni) V.Gonderenler.Add(g);
                if (g.Varsayilan) foreach (var x in V.Gonderenler) if (x != g) x.Varsayilan = false;
                V.Kaydet();
                GonderenleriDoldur(g);
                Yenile();
            }
        }

        void GonderenSil()
        {
            if (!(cbGonderen.SelectedItem is Gonderen g)) return;
            if (V.Gonderenler.Count <= 1) { MessageBox.Show("En az bir gönderen kalmalı.", "Kargo Etiket Programı"); return; }
            if (MessageBox.Show(g.Ad + " silinsin mi?", "Kargo Etiket Programı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            V.Gonderenler.Remove(g);
            if (!V.Gonderenler.Any(x => x.Varsayilan)) V.Gonderenler[0].Varsayilan = true;
            V.Kaydet();
            GonderenleriDoldur(V.Gonderenler.First(x => x.Varsayilan));
        }

        void AliciDuzenle(Alici a)
        {
            bool yeni = a == null;
            if (yeni) a = new Alici();
            using (var f = new AliciForm(a))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (yeni) V.Alicilar.Add(a);
                V.Kaydet();
                AlicilariDoldur(a);
            }
        }

        void AliciSil()
        {
            if (!(cbAlici.SelectedItem is Alici a)) return;
            if (MessageBox.Show(a.Ad + " silinsin mi?", "Kargo Etiket Programı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            V.Alicilar.Remove(a);
            V.Kaydet();
            AlicilariDoldur(null);
        }

        void Yazdir(bool onizle)
        {
            var et = MevcutEtiket();
            if (et.Alici == null) { MessageBox.Show("Alıcı seçin.", "Kargo Etiket Programı"); return; }
            string boyut = Boyut;
            float tg = V.Ayarlar.TermalGenislik;
            var doc = new PrintDocument { DocumentName = "Kargo Etiket - " + et.Alici.Ad };
            if (cbYazici.SelectedItem is string p && p.Length > 0) doc.PrinterSettings.PrinterName = p;
            if (!doc.PrinterSettings.IsValid) { MessageBox.Show("Yazıcı bulunamadı.", "Kargo Etiket Programı"); return; }

            float h = Renderer.Yukseklik(et) * (tg / Renderer.TasarimGenislik);
            var sayfa = Renderer.SayfaBoyutu(boyut, h, tg);
            Func<float, int> Inch = mm => (int)Math.Round(mm / 25.4f * 100f);
            PaperSize ps = null;
            if (boyut != "TERMAL")
            {
                var kind = boyut == "A4" ? PaperKind.A4 : PaperKind.A5;
                foreach (PaperSize x in doc.PrinterSettings.PaperSizes) if (x.Kind == kind) { ps = x; break; }
            }
            if (ps == null) ps = new PaperSize("Kargo Etiket " + boyut, Inch(boyut == "TERMAL" ? 80f : sayfa.Width), Inch(sayfa.Height));
            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Landscape = false;
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            doc.OriginAtMargins = false;

            int kalan = (int)nuKopya.Value;
            doc.BeginPrint += (s, e) => kalan = (int)nuKopya.Value;
            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.PageUnit = GraphicsUnit.Millimeter;
                if (boyut != "TERMAL" && !(doc.PrintController?.IsPreview ?? false)) g.TranslateTransform(-e.PageSettings.HardMarginX * 0.254f, -e.PageSettings.HardMarginY * 0.254f);
                Renderer.SayfaCiz(g, et, boyut, tg);
                kalan--;
                e.HasMorePages = kalan > 0;
            };

            try
            {
                if (onizle)
                {
                    using (var d = new PrintPreviewDialog { Document = doc, Width = 900, Height = 800, StartPosition = FormStartPosition.CenterParent })
                    {
                        try { d.Icon = Icon; } catch { }
                        d.ShowDialog(this);
                    }
                }
                else doc.Print();
            }
            catch (Exception ex) { MessageBox.Show("Yazdırma hatası: " + ex.Message, "Kargo Etiket Programı"); }
        }

    }

    public class OnizlemePanel : Panel
    {
        Etiket et; string boyut = "TERMAL"; float tg = 70;
        public OnizlemePanel() { DoubleBuffered = true; BackColor = Color.FromArgb(90, 95, 105); ResizeRedraw = true; }
        public void Ayarla(Etiket e, string b, float t) { et = e; boyut = b; tg = t; Invalidate(); }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            if (et == null) return;
            var g = pe.Graphics;
            float h = Renderer.Yukseklik(et) * (tg / Renderer.TasarimGenislik);
            var sz = Renderer.SayfaBoyutu(boyut, h, tg);
            float pad = 20;
            float s = Math.Min((Width - 2 * pad) / sz.Width, (Height - 2 * pad) / sz.Height);
            if (boyut == "TERMAL") s = Math.Min(s, 7f);
            float pw = sz.Width * s, ph = sz.Height * s;
            float x = (Width - pw) / 2, y = (Height - ph) / 2;
            using (var sh = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillRectangle(sh, x + 4, y + 4, pw, ph);
            g.FillRectangle(Brushes.White, x, y, pw, ph);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(s, s);
            Renderer.SayfaCiz(g, et, boyut, tg);
            g.Restore(st);
            string bilgi = boyut == "TERMAL" ? $"{tg:0} mm termal  —  {h:0} mm" : boyut;
            TextRenderer.DrawText(g, bilgi, Font, new Point(8, 6), Color.White);
        }
    }
}
