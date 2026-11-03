using System.Drawing;
using System.Windows.Forms;

namespace QuteApp.UI;

/// <summary>Panel blanco con borde fino, usado como "tarjeta" en toda la app.</summary>
public class Tarjeta : Panel
{
    public Tarjeta()
    {
        BackColor = Color.White;
        Padding = new Padding(16);
        Margin = new Padding(0, 0, 16, 16);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var lapiz = new Pen(Tema.Borde);
        e.Graphics.DrawRectangle(lapiz, 0, 0, Width - 1, Height - 1);
    }
}

/// <summary>Colores, fuentes y fábricas de controles para mantener un estilo único (RNF-07: botones grandes y legibles).</summary>
public static class Tema
{
    public static readonly Color Primario = Color.FromArgb(0x1E, 0x40, 0x8A);
    public static readonly Color PrimarioOscuro = Color.FromArgb(0x16, 0x2F, 0x66);
    public static readonly Color Acento = Color.FromArgb(0xF5, 0xA6, 0x23);
    public static readonly Color Exito = Color.FromArgb(0x2E, 0x9E, 0x5B);
    public static readonly Color Peligro = Color.FromArgb(0xD6, 0x45, 0x45);
    public static readonly Color Gris = Color.FromArgb(0x8A, 0x94, 0xA6);
    public static readonly Color Fondo = Color.FromArgb(0xF3, 0xF5, 0xF9);
    public static readonly Color Texto = Color.FromArgb(0x22, 0x2B, 0x3A);
    public static readonly Color TextoSuave = Color.FromArgb(0x6B, 0x76, 0x8A);
    public static readonly Color Borde = Color.FromArgb(0xE1, 0xE5, 0xEC);

    public static readonly Font Normal = new("Segoe UI", 10.5f);
    public static readonly Font Negrita = new("Segoe UI", 10.5f, FontStyle.Bold);
    public static readonly Font Pequena = new("Segoe UI", 9f);
    public static readonly Font Subtitulo = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font Titulo = new("Segoe UI", 20f, FontStyle.Bold);
    public static readonly Font Grande = new("Segoe UI", 22f, FontStyle.Bold);

    public static Color Aclarar(Color c, float f) =>
        Color.FromArgb(c.A, (int)(c.R + (255 - c.R) * f), (int)(c.G + (255 - c.G) * f), (int)(c.B + (255 - c.B) * f));

    public static Button Boton(string texto, Color fondo, int ancho = 200, int alto = 44, Color? colorTexto = null)
    {
        var b = new Button
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            BackColor = fondo,
            ForeColor = colorTexto ?? Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = Negrita,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 8),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Aclarar(fondo, 0.15f);
        return b;
    }

    public static Label Etiqueta(string texto, Font? fuente = null, Color? color = null) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = fuente ?? Normal,
        ForeColor = color ?? Texto,
        Margin = new Padding(0, 0, 0, 6)
    };

    public static Label Badge(string texto, Color fondo) => new()
    {
        Text = texto,
        AutoSize = true,
        BackColor = fondo,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Padding = new Padding(8, 3, 8, 3),
        Margin = new Padding(0, 0, 8, 6)
    };

    public static TextBox Caja(string? placeholder = null, bool password = false) => new()
    {
        PlaceholderText = placeholder ?? "",
        UseSystemPasswordChar = password,
        BorderStyle = BorderStyle.FixedSingle,
        Font = Normal
    };

    public static ComboBox Combo(IEnumerable<string> items, int ancho = 320)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ancho, Font = Normal };
        c.Items.AddRange(items.Cast<object>().ToArray());
        return c;
    }

    /// <summary>Título pequeño arriba + control de entrada debajo.</summary>
    public static Control Campo(string titulo, Control input, int ancho = 320)
    {
        var p = new Panel { Width = ancho, Margin = new Padding(0, 0, 16, 6) };
        var l = new Label
        {
            Text = titulo,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextoSuave,
            AutoSize = true,
            Location = new Point(0, 0)
        };
        input.Font = Normal;
        input.Location = new Point(0, 22);
        input.Width = ancho - 4;
        p.Height = 22 + Math.Max(input.Height, 30) + 10;
        p.Controls.Add(l);
        p.Controls.Add(input);
        return p;
    }

    public static Tarjeta Kpi(string titulo, string valor, Color color, string? detalle = null, int ancho = 230)
    {
        var t = new Tarjeta { Width = ancho, Height = 112 };
        t.Controls.Add(new Label { Text = titulo, Font = Pequena, ForeColor = TextoSuave, AutoSize = true, Location = new Point(16, 12) });
        t.Controls.Add(new Label { Text = valor, Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = color, AutoSize = true, Location = new Point(14, 34) });
        if (detalle != null)
            t.Controls.Add(new Label { Text = detalle, Font = Pequena, ForeColor = TextoSuave, AutoSize = true, Location = new Point(16, 82) });
        return t;
    }

    public static void ActualizarKpi(Tarjeta kpi, string valor) => ((Label)kpi.Controls[1]).Text = valor;

    public static DataGridView Grid(params string[] columnas)
    {
        var g = new DataGridView
        {
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            GridColor = Borde,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            Font = Normal,
            Margin = new Padding(0, 0, 16, 16)
        };
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = 38;
        g.ColumnHeadersDefaultCellStyle.BackColor = Primario;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = Negrita;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primario;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0xDB, 0xE6, 0xFB);
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF7, 0xF9, 0xFC);
        g.RowTemplate.Height = 34;
        foreach (var c in columnas) g.Columns.Add(c, c);
        return g;
    }
}
