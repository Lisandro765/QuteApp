using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QuteApp.Servicios;

namespace QuteApp.UI;

/// <summary>
/// Mapa de demostración dibujado con GDI+ (calles, puntos de referencia, origen A, destino B y conductor Q).
/// Para producción se puede reemplazar por GMap.NET o WebView2 + Leaflet/OpenStreetMap (limitación de mapas gratuitos).
/// </summary>
public class MapaControl : Control
{
    public PointF? Origen { get; set; }
    public PointF? Destino { get; set; }
    public PointF? Conductor { get; set; }

    public MapaControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Font = Tema.Pequena;
    }

    private PointF Px(PointF n) => new(n.X * Width, n.Y * Height);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(0xE9, 0xEF, 0xE4));

        using (var calle = new Pen(Color.White, 9))
        {
            for (int i = 1; i < 6; i++) g.DrawLine(calle, Width * i / 6f, 0, Width * i / 6f, Height);
            for (int i = 1; i < 4; i++) g.DrawLine(calle, 0, Height * i / 4f, Width, Height * i / 4f);
        }

        foreach (var kv in Mapa.Puntos)
        {
            var p = Px(kv.Value);
            g.FillEllipse(Brushes.SteelBlue, p.X - 5, p.Y - 5, 10, 10);
            g.DrawString(kv.Key, Font, Brushes.DimGray, p.X + 8, p.Y - 8);
        }

        if (Origen != null && Destino != null)
        {
            using var ruta = new Pen(Tema.Primario, 4) { DashStyle = DashStyle.Dash };
            g.DrawLine(ruta, Px(Origen.Value), Px(Destino.Value));
        }

        Marcador(g, Origen, Tema.Exito, "A");
        Marcador(g, Destino, Tema.Peligro, "B");
        Marcador(g, Conductor, Tema.Acento, "Q");

        g.DrawString("Mapa de demostración · Aguilares", Font, Brushes.Gray, 8, Height - 20);
    }

    private void Marcador(Graphics g, PointF? punto, Color color, string letra)
    {
        if (punto == null) return;
        var c = Px(punto.Value);
        using var br = new SolidBrush(color);
        using var borde = new Pen(Color.White, 2);
        g.FillEllipse(br, c.X - 14, c.Y - 14, 28, 28);
        g.DrawEllipse(borde, c.X - 14, c.Y - 14, 28, 28);
        using var f = new Font("Segoe UI", 9f, FontStyle.Bold);
        var s = g.MeasureString(letra, f);
        g.DrawString(letra, f, Brushes.White, c.X - s.Width / 2, c.Y - s.Height / 2);
    }
}

/// <summary>Gráfico de barras simple para el dashboard del administrador.</summary>
public class BarrasControl : Control
{
    public string[] Etiquetas { get; set; } = Array.Empty<string>();
    public double[] Valores { get; set; } = Array.Empty<double>();

    public BarrasControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Valores.Length == 0) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        double max = Valores.Max();
        if (max <= 0) max = 1;
        int n = Valores.Length;
        float margen = 20, baseY = Height - 28;
        float ancho = (Width - margen * 2) / n;
        using var pincel = new SolidBrush(Tema.Primario);
        using var f = new Font("Segoe UI", 9f);
        for (int i = 0; i < n; i++)
        {
            float h = (float)(Valores[i] / max) * (baseY - 36);
            float x = margen + i * ancho + ancho * 0.2f;
            g.FillRectangle(pincel, x, baseY - h, ancho * 0.6f, h);
            var valor = Valores[i].ToString("0");
            var sv = g.MeasureString(valor, f);
            g.DrawString(valor, f, Brushes.Black, x + ancho * 0.3f - sv.Width / 2, baseY - h - sv.Height);
            if (i < Etiquetas.Length)
            {
                var se = g.MeasureString(Etiquetas[i], f);
                g.DrawString(Etiquetas[i], f, Brushes.DimGray, x + ancho * 0.3f - se.Width / 2, baseY + 4);
            }
        }
    }
}
