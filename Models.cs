using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace KargoEtiket
{
    public static class Img
    {
        public static Image Res(string name)
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("res." + name + ".png");
            if (s == null) return null;
            using (s) using (var tmp = Image.FromStream(s)) return new Bitmap(tmp);
        }
        public static string ResB64(string name)
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("res." + name + ".png");
            if (s == null) return null;
            using (s) using (var ms = new MemoryStream()) { s.CopyTo(ms); return Convert.ToBase64String(ms.ToArray()); }
        }
        public static Image FromB64(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return null;
            try { using (var ms = new MemoryStream(Convert.FromBase64String(b64))) using (var tmp = Image.FromStream(ms)) return new Bitmap(tmp); }
            catch { return null; }
        }
        public static string FileToB64(string path, int maxW = 700)
        {
            using (var src = Image.FromFile(path))
            {
                int w = src.Width, h = src.Height;
                if (w > maxW) { h = h * maxW / w; w = maxW; }
                using (var bmp = new Bitmap(w, h))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, 0, 0, w, h);
                    }
                    using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return Convert.ToBase64String(ms.ToArray()); }
                }
            }
        }
    }

    public class Kargo
    {
        public string Kod, Ad;
        Image _logo; bool _loaded;
        public Image Logo { get { if (!_loaded) { _logo = Img.Res("kargo_" + Kod); _loaded = true; } return _logo; } }
        public override string ToString() => Ad;

        public static readonly List<Kargo> Liste = new List<Kargo>
        {
            new Kargo{Kod="YURTICI",Ad="Yurtiçi Kargo"},
            new Kargo{Kod="ARAS",Ad="Aras Kargo"},
            new Kargo{Kod="SURAT",Ad="Sürat Kargo"},
            new Kargo{Kod="PTT",Ad="PTT Kargo"},
            new Kargo{Kod="HEPSIJET",Ad="HepsiJET"},
            new Kargo{Kod="TRENDYOL",Ad="Trendyol"},
            new Kargo{Kod="KOLAYGELSIN",Ad="Kolay Gelsin"},
            new Kargo{Kod="CARGOX",Ad="CargoX"},
            new Kargo{Kod="DHL",Ad="DHL"},
            new Kargo{Kod="FEDEX",Ad="FedEx"},
            new Kargo{Kod="UPS",Ad="UPS"},
        };
        public static Kargo Bul(string kod) => Liste.FirstOrDefault(k => k.Kod == kod);
    }

    public static class Odeme
    {
        // "UA" = Ücret Alıcı, "PO" = Peşin Ödemeli (Ücret Gönderici), "" = yok
        public static readonly string[] Kodlar = { "UA", "PO", "" };
        public static readonly string[] Adlar = { "Ücret Alıcı (Ü.A.)", "Ücret Gönderici (P.Ö.)", "Belirtme" };
        public static string Kisa(string k) => k == "UA" ? "Ü.A." : k == "PO" ? "P.Ö." : "";
        public static string Uzun(string k) => k == "UA" ? "ÜCRET ALICI" : k == "PO" ? "ÜCRET GÖNDERİCİ" : "";
    }

    public class Gonderen
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Ad = "", Adres = "", Telefon = "", Web = "", LogoB64 = "";
        public bool Varsayilan;
        [XmlIgnore] Image _logo; [XmlIgnore] string _logoKey;
        [XmlIgnore] public Image Logo { get { if (_logoKey != LogoB64) { _logo = Img.FromB64(LogoB64); _logoKey = LogoB64; } return _logo; } }
        public override string ToString() => (Varsayilan ? "★ " : "") + Ad;
    }

    public class Alici
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Ad = "", Yetkili = "", Telefon = "", Adres = "", Not = "", LogoB64 = "";
        public string Kargo = "YURTICI", Odeme = "UA", AnlasmaKodu = "";
        [XmlIgnore] Image _logo; [XmlIgnore] string _logoKey;
        [XmlIgnore] public Image Logo { get { if (_logoKey != LogoB64) { _logo = Img.FromB64(LogoB64); _logoKey = LogoB64; } return _logo; } }
        public override string ToString() => Ad;
    }

    public class Ayarlar
    {
        public string Boyut = "TERMAL"; // TERMAL, A5, A4
        public string YaziciTermal = "", YaziciA5 = "", YaziciA4 = "";
        public float TermalGenislik = 70f;
        public string Tema = "ACIK"; // ACIK, KOYU
    }

    public class Veri
    {
        public List<Gonderen> Gonderenler = new List<Gonderen>();
        public List<Alici> Alicilar = new List<Alici>();
        public Ayarlar Ayarlar = new Ayarlar();

        [XmlIgnore] public static string Yol;

        static string BulYol()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string p = Path.Combine(exeDir, "Kargo-Etiket-Veri.xml");
            try
            {
                if (File.Exists(p)) return p;
                File.WriteAllText(p + ".tmp", "x"); File.Delete(p + ".tmp");
                return p;
            }
            catch
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kargo-Etiket-Programi");
                Directory.CreateDirectory(d);
                return Path.Combine(d, "Kargo-Etiket-Veri.xml");
            }
        }

        public static Veri Yukle()
        {
            Yol = BulYol();
            if (File.Exists(Yol))
            {
                try
                {
                    Veri v0;
                    using (var f = File.OpenRead(Yol))
                        v0 = (Veri)new XmlSerializer(typeof(Veri)).Deserialize(f);
                    if (v0.Ayarlar.TermalGenislik >= 71.5f) v0.Ayarlar.TermalGenislik = 70f;
                    return v0;
                }
                catch (Exception ex)
                {
                    try { File.Copy(Yol, Yol + ".bozuk-" + DateTime.Now.ToString("yyyyMMddHHmmss")); } catch { }
                    MessageBox.Show("Veri dosyası okunamadı, varsayılan veriler yüklendi.\n" + ex.Message, "Kargo Etiket Programı");
                }
            }
            var v = Varsayilanlar();
            v.Kaydet();
            return v;
        }

        public void Kaydet()
        {
            try
            {
                string tmp = Yol + ".yeni";
                using (var f = File.Create(tmp))
                    new XmlSerializer(typeof(Veri)).Serialize(f, this);
                if (File.Exists(Yol)) File.Replace(tmp, Yol, null); else File.Move(tmp, Yol);
            }
            catch (Exception ex) { MessageBox.Show("Kaydedilemedi: " + ex.Message, "Kargo Etiket Programı"); }
        }

        static Veri Varsayilanlar()
        {
            var v = new Veri();
            v.Gonderenler.Add(new Gonderen { Ad = "Firma Adınız", Varsayilan = true });
            return v;
        }
    }
}
