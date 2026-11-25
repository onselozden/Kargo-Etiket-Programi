using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace KargoEtiket
{
    public class Etiket
    {
        public Gonderen Gonderen;
        public Alici Alici;
        public string Kargo = "", Odeme = "", Kod = "", Not = "";
        public bool Kirilir = true;
    }

    // Tüm ölçüler mm. Çizim 72 mm genişlik için tasarlanır; A4/A5'te ölçeklenir.
    public static class Renderer
    {
        public const float TasarimGenislik = 72f;
        static Image _kirilir;
        static Image KirilirImg => _kirilir ?? (_kirilir = Img.Res("kirilir"));
        const string FontAd = "Segoe UI";

        static float Pt(float pt) => pt * 0.3528f;

        public static SizeF SayfaBoyutu(string boyut, float etiketYukseklik, float termalGenislik)
        {
            if (boyut == "A4") return new SizeF(210, 297);
            if (boyut == "A5") return new SizeF(148, 210);
            return new SizeF(termalGenislik, etiketYukseklik + 5);
        }

        public static float Yukseklik(Etiket e)
        {
            using (var bmp = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bmp))
            {
                g.PageUnit = GraphicsUnit.Millimeter;
                return Ciz(g, e, 0, 0, TasarimGenislik, false);
            }
        }

        // g: dünya birimi = mm, orijin = sayfanın sol üst köşesi
        public static void SayfaCiz(Graphics g, Etiket e, string boyut, float termalGenislik)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float h = Yukseklik(e);
            var st = g.Save();
            if (boyut == "A4" || boyut == "A5")
            {
                var sz = SayfaBoyutu(boyut, h, termalGenislik);
                float m = 10f;
                float w = sz.Width - 2 * m, hh = sz.Height - 2 * m;
                float s = Math.Min(w / TasarimGenislik, hh / h);
                float x = m + (w - TasarimGenislik * s) / 2f;
                g.TranslateTransform(x, m);
                g.ScaleTransform(s, s);
            }
            else
            {
                float s = termalGenislik / TasarimGenislik;
                g.TranslateTransform(0f, 2f);
                g.ScaleTransform(s, s);
            }
            Ciz(g, e, 0, 0, TasarimGenislik, true);
            g.Restore(st);
        }

        static float Yazi(Graphics g, string t, Font f, float x, float y, float w, bool ciz, StringAlignment hiza = StringAlignment.Near, Brush b = null)
        {
            if (string.IsNullOrWhiteSpace(t)) return 0;
            t = t.Replace("\r\n", "\n").Trim();
            using (var sf = new StringFormat { Alignment = hiza })
            {
                var sz = g.MeasureString(t, f, new SizeF(w, 10000), sf);
                if (ciz) g.DrawString(t, f, b ?? Brushes.Black, new RectangleF(x, y, w, sz.Height + 1), sf);
                return sz.Height;
            }
        }

        static SizeF Sigdir(Image img, float maxW, float maxH)
        {
            float s = Math.Min(maxW / img.Width, maxH / img.Height);
            return new SizeF(img.Width * s, img.Height * s);
        }

        static void Cizgi(Graphics g, float x, float y, float w, bool ciz)
        {
            if (!ciz) return;
            using (var p = new Pen(Color.Black, 0.3f) { DashStyle = DashStyle.Dash })
                g.DrawLine(p, x, y, x + w, y);
        }

        public static float Ciz(Graphics g, Etiket e, float x0, float y0, float W, bool ciz)
        {
            float y = y0;
            var S = e.Gonderen ?? new Gonderen();
            var R = e.Alici ?? new Alici();
            using (var fBaslik = new Font(FontAd, Pt(9), FontStyle.Bold | FontStyle.Underline, GraphicsUnit.World))
            using (var fGAd = new Font(FontAd, Pt(9.5f), FontStyle.Bold, GraphicsUnit.World))
            using (var fGYazi = new Font(FontAd, Pt(8f), FontStyle.Regular, GraphicsUnit.World))
            using (var fAAd = new Font(FontAd, Pt(13f), FontStyle.Bold, GraphicsUnit.World))
            using (var fAYazi = new Font(FontAd, Pt(10.5f), FontStyle.Regular, GraphicsUnit.World))
            using (var fAKalin = new Font(FontAd, Pt(10.5f), FontStyle.Bold, GraphicsUnit.World))
            using (var fNot = new Font(FontAd, Pt(10.5f), FontStyle.Bold, GraphicsUnit.World))
            using (var fKodBaslik = new Font(FontAd, Pt(9f), FontStyle.Bold, GraphicsUnit.World))
            using (var fKod = new Font(FontAd, Pt(18f), FontStyle.Bold, GraphicsUnit.World))
            {
                // ---------- GÖNDEREN ----------
                y += Yazi(g, "GÖNDEREN:", fBaslik, x0, y, W, ciz) + 0.8f;
                if (S.Logo != null)
                {
                    var sz = Sigdir(S.Logo, W * 0.62f, 13f);
                    if (ciz) g.DrawImage(S.Logo, x0, y, sz.Width, sz.Height);
                    y += sz.Height + 1.2f;
                }
                y += Yazi(g, S.Ad, fGAd, x0, y, W, ciz);
                y += Yazi(g, S.Adres, fGYazi, x0, y, W, ciz);
                string tw = string.Join("     ", new[] { string.IsNullOrWhiteSpace(S.Telefon) ? "" : "Tel: " + S.Telefon.Trim(), (S.Web ?? "").Trim() });
                y += Yazi(g, tw.Trim(), fGYazi, x0, y, W, ciz);

                y += 2f; Cizgi(g, x0, y, W, ciz); y += 2.2f;

                // ---------- ALICI ----------
                float K = 17f;
                float ust = y;
                bool kir = e.Kirilir && KirilirImg != null;
                if (kir && ciz) g.DrawImage(KirilirImg, x0 + W - K, y, K, K);
                Func<float> gen = () => (kir && y < ust + K + 0.5f) ? W - K - 2f : W;

                y += Yazi(g, "ALICI:", fBaslik, x0, y, gen(), ciz) + 0.8f;
                if (R.Logo != null)
                {
                    var sz = Sigdir(R.Logo, gen(), 12f);
                    if (ciz) g.DrawImage(R.Logo, x0, y, sz.Width, sz.Height);
                    y += sz.Height + 1f;
                }
                y += Yazi(g, R.Ad, fAAd, x0, y, gen(), ciz);
                string yt = string.Join("  ", new[] { (R.Yetkili ?? "").Trim(), (R.Telefon ?? "").Trim() }).Trim();
                if (!string.IsNullOrEmpty(R.Yetkili) || !string.IsNullOrEmpty(R.Telefon))
                    y += Yazi(g, yt, fAKalin, x0, y, gen(), ciz);
                if (!string.IsNullOrWhiteSpace(R.Adres)) { y += 0.6f; y += Yazi(g, R.Adres, fAYazi, x0, y, gen(), ciz); }
                if (kir) y = Math.Max(y, ust + K);

                string not = string.Join("\n", new[] { (R.Not ?? "").Trim(), (e.Not ?? "").Trim() }).Trim();
                if (not.Length > 0) { y += 1.5f; y += Yazi(g, not, fNot, x0, y, W, ciz); }

                // ---------- KARGO ----------
                var kg = Kargo.Bul(e.Kargo);
                string kisa = Odeme.Kisa(e.Odeme), uzun = Odeme.Uzun(e.Odeme);
                string kod = (e.Kod ?? "").Trim();
                if (kg != null || kod.Length > 0 || kisa.Length > 0)
                {
                    y += 2f; Cizgi(g, x0, y, W, ciz); y += 2.5f;
                    if (kg?.Logo != null)
                    {
                        var sz = Sigdir(kg.Logo, W * 0.72f, 18f);
                        if (ciz) g.DrawImage(kg.Logo, x0 + (W - sz.Width) / 2f, y, sz.Width, sz.Height);
                        y += sz.Height + 1.5f;
                    }
                    if (kod.Length > 0)
                        y += Yazi(g, "ANLAŞMA KODU", fKodBaslik, x0, y, W, ciz, StringAlignment.Center);
                    string satir = (kisa + "  " + kod).Trim();
                    if (satir.Length > 0)
                        y += Yazi(g, satir, fKod, x0, y, W, ciz, StringAlignment.Center);
                    if (uzun.Length > 0)
                        y += Yazi(g, uzun, fKodBaslik, x0, y, W, ciz, StringAlignment.Center);
                }
                y += 2f;
            }
            return y - y0;
        }
    }
}
