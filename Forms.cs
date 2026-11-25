using System;
using System.Drawing;
using System.Windows.Forms;

namespace KargoEtiket
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

    // Basit alan formu altyapısı
    public class AlanForm : Form
    {
        protected int y = 12;
        protected const int L = 12, LW = 120, W = 360;
        protected Panel icerik;

        public AlanForm(string baslik)
        {
            Text = baslik;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            icerik = new Panel { Dock = DockStyle.Fill };
            Controls.Add(icerik);
            Load += (s, e) => Tema.Uygula(this);
        }

        protected TextBox Alan(string etiket, string deger, int yukseklik = 0)
        {
            icerik.Controls.Add(new Label { Text = etiket, Left = L, Top = y + 3, Width = LW });
            var tb = new TextBox { Left = L + LW, Top = y, Width = W, Text = (deger ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n") };
            if (yukseklik > 0) { tb.Multiline = true; tb.Height = yukseklik; tb.ScrollBars = ScrollBars.Vertical; tb.AcceptsReturn = true; }
            icerik.Controls.Add(tb);
            y += tb.Height + 8;
            return tb;
        }

        protected string logoB64;
        protected PictureBox LogoAlani(string mevcut)
        {
            logoB64 = mevcut ?? "";
            icerik.Controls.Add(new Label { Text = "Logo", Left = L, Top = y + 3, Width = LW });
            var pb = new PictureBox { Left = L + LW, Top = y, Width = 220, Height = 70, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Image = Img.FromB64(logoB64) };
            var bSec = new Button { Text = "Seç...", Left = L + LW + 228, Top = y, Width = 130, Height = 30 };
            var bKaldir = new Button { Text = "Kaldır", Left = L + LW + 228, Top = y + 38, Width = 130, Height = 30 };
            bSec.Click += (s, e) =>
            {
                using (var d = new OpenFileDialog { Filter = "Resimler|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tümü|*.*" })
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    try { logoB64 = Img.FileToB64(d.FileName); pb.Image = Img.FromB64(logoB64); }
                    catch (Exception ex) { MessageBox.Show("Resim açılamadı: " + ex.Message); }
                }
            };
            bKaldir.Click += (s, e) => { logoB64 = ""; pb.Image = null; };
            icerik.Controls.AddRange(new Control[] { pb, bSec, bKaldir });
            y += 78;
            return pb;
        }

        protected void Butonlar(Func<bool> kaydet)
        {
            y += 6;
            var ok = new Button { Text = "Kaydet", Left = L + LW + W - 190, Top = y, Width = 90, Height = 32 };
            var ip = new Button { Text = "İptal", Left = L + LW + W - 90, Top = y, Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
            ok.Click += (s, e) => { if (kaydet()) { DialogResult = DialogResult.OK; Close(); } };
            icerik.Controls.AddRange(new Control[] { ok, ip });
            AcceptButton = null; CancelButton = ip;
            ClientSize = new Size(L + LW + W + 16, y + 46);
        }

        protected static string Duz(TextBox t) => t.Text.Replace("\r\n", "\n").Trim();
    }

    public class GonderenForm : AlanForm
    {
        public GonderenForm(Gonderen g) : base("Gönderen")
        {
            var tAd = Alan("Ad / Unvan", g.Ad);
            var tAdres = Alan("Adres", g.Adres, 50);
            var tTel = Alan("Telefon", g.Telefon);
            var tWeb = Alan("Web", g.Web);
            LogoAlani(g.LogoB64);
            var ch = new CheckBox { Text = "Varsayılan (program açılınca seçili gelsin)", Left = L + LW, Top = y, AutoSize = true, Checked = g.Varsayilan };
            icerik.Controls.Add(ch); y += 30;
            Butonlar(() =>
            {
                if (Duz(tAd).Length == 0) { MessageBox.Show("Ad boş olamaz."); return false; }
                g.Ad = Duz(tAd); g.Adres = Duz(tAdres); g.Telefon = Duz(tTel); g.Web = Duz(tWeb);
                g.LogoB64 = logoB64; g.Varsayilan = ch.Checked;
                return true;
            });
        }
    }

    public class AliciForm : AlanForm
    {
        public AliciForm(Alici a) : base("Alıcı")
        {
            var tAd = Alan("Ad / Unvan", a.Ad);
            var tYet = Alan("Yetkili / açıklama", a.Yetkili);
            var tTel = Alan("Telefon", a.Telefon);
            var tAdres = Alan("Adres", a.Adres, 60);
            var tNot = Alan("Sabit not", a.Not, 45);
            LogoAlani(a.LogoB64);

            icerik.Controls.Add(new Label { Text = "Kargo firması", Left = L, Top = y + 6, Width = LW });
            var cbK = new ComboBox { Left = L + LW, Top = y, Width = W, DropDownStyle = ComboBoxStyle.DropDownList };
            cbK.Items.Add("(Seçilmedi)");
            foreach (var k in Kargo.Liste) cbK.Items.Add(k);
            MainForm.KargoSec(cbK, a.Kargo);
            icerik.Controls.Add(cbK); y += 36;

            icerik.Controls.Add(new Label { Text = "Ödeme", Left = L, Top = y + 3, Width = LW });
            var cbO = new ComboBox { Left = L + LW, Top = y, Width = W, DropDownStyle = ComboBoxStyle.DropDownList };
            cbO.Items.AddRange(Odeme.Adlar);
            cbO.SelectedIndex = Math.Max(0, Array.IndexOf(Odeme.Kodlar, a.Odeme ?? ""));
            icerik.Controls.Add(cbO); y += 34;

            var tKod = Alan("Anlaşma kodu", a.AnlasmaKodu);

            Butonlar(() =>
            {
                if (Duz(tAd).Length == 0) { MessageBox.Show("Ad boş olamaz."); return false; }
                a.Ad = Duz(tAd); a.Yetkili = Duz(tYet); a.Telefon = Duz(tTel); a.Adres = Duz(tAdres); a.Not = Duz(tNot);
                a.LogoB64 = logoB64; a.Kargo = MainForm.KargoKod(cbK);
                a.Odeme = Odeme.Kodlar[Math.Max(0, cbO.SelectedIndex)]; a.AnlasmaKodu = Duz(tKod);
                return true;
            });
        }
    }
}

namespace KargoEtiket
{
    public static class Tema
    {
        public static bool Koyu;
        static readonly Color KArka = Color.FromArgb(32, 33, 36), KKutu = Color.FromArgb(48, 50, 54), KYazi = Color.FromArgb(235, 235, 235);

        public static void Uygula(Control c)
        {
            if (c is OnizlemePanel) return;
            if ("sabit".Equals(c.Tag)) { foreach (Control x in c.Controls) Uygula(x); return; }
            Color arka = Koyu ? KArka : SystemColors.Control, yazi = Koyu ? KYazi : SystemColors.ControlText;
            if (c is TextBox || c is ComboBox || c is NumericUpDown)
            {
                c.BackColor = Koyu ? KKutu : SystemColors.Window;
                c.ForeColor = Koyu ? KYazi : SystemColors.WindowText;
                if (c is ComboBox cb) cb.FlatStyle = Koyu ? FlatStyle.Flat : FlatStyle.Standard;
            }
            else if (c is Button b)
            {
                b.FlatStyle = Koyu ? FlatStyle.Flat : FlatStyle.Standard;
                b.BackColor = Koyu ? KKutu : SystemColors.Control;
                b.ForeColor = yazi;
                b.UseVisualStyleBackColor = !Koyu;
            }
            else if (c is PictureBox) { }
            else if (c is MenuStrip ms)
            {
                ms.RenderMode = Koyu ? ToolStripRenderMode.System : ToolStripRenderMode.ManagerRenderMode;
                ms.BackColor = Koyu ? KKutu : SystemColors.MenuBar;
                ms.ForeColor = yazi;
                foreach (ToolStripItem it in ms.Items) Menu(it, yazi);
            }
            else if (c.ForeColor == Color.Gray || c.ForeColor == Color.DarkGray) { c.BackColor = arka; }
            else { c.BackColor = arka; c.ForeColor = yazi; }
            foreach (Control x in c.Controls) Uygula(x);
        }

        static void Menu(ToolStripItem it, Color yazi)
        {
            it.ForeColor = yazi;
            if (it is ToolStripMenuItem mi)
            {
                mi.DropDown.BackColor = Koyu ? KKutu : SystemColors.Menu;
                foreach (ToolStripItem x in mi.DropDownItems) { x.BackColor = Koyu ? KKutu : SystemColors.Menu; Menu(x, yazi); }
            }
        }
    }

    public class HakkindaForm : Form
    {
        public HakkindaForm()
        {
            Text = "Hakkında";
            Font = new Font("Segoe UI", 10f);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 220);
            Image ikon = null;
            try { ikon = Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(); } catch { }
            Controls.Add(new PictureBox { Image = ikon, Left = 20, Top = 22, Width = 48, Height = 48, SizeMode = PictureBoxSizeMode.Zoom });
            Controls.Add(new Label { Text = "Kargo Etiket Programı", Left = 84, Top = 20, AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold) });
            Controls.Add(new Label { Text = "Sürüm " + Application.ProductVersion, Left = 86, Top = 56, AutoSize = true });
            Controls.Add(new Label { Text = "Kargo etiketi hazırlama ve yazdırma programı\n(72 mm termal, A5, A4)", Left = 20, Top = 92, Width = 340, Height = 44 });
            Controls.Add(new Label { Text = "Hazırlayan: Önsel Özden", Left = 20, Top = 142, AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) });
            var ok = new Button { Text = "Tamam", Left = 280, Top = 176, Width = 84, Height = 30, DialogResult = DialogResult.OK };
            Controls.Add(ok); AcceptButton = ok; CancelButton = ok;
            Load += (s, e) => Tema.Uygula(this);
        }
    }
}
