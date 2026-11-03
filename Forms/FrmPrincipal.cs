using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;
using QuteApp.Vistas;

namespace QuteApp.Forms;

/// <summary>
/// Pantalla principal: barra superior (marca, estado de red, notificaciones, usuario, cerrar sesión),
/// menú lateral según el rol (RNF-05), área de contenido con vistas intercambiables y barra de estado.
/// </summary>
public class FrmPrincipal : Form
{
    private record ItemMenu(string Clave, string Texto, Func<VistaBase> Crear);

    public bool CerroSesion { get; private set; }

    private readonly Panel host = new() { Dock = DockStyle.Fill, BackColor = Tema.Fondo };
    private readonly FlowLayoutPanel menu = new()
    {
        Dock = DockStyle.Left, Width = 240, BackColor = Tema.PrimarioOscuro,
        FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 14, 0, 0)
    };
    private readonly Panel barra = new() { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Primario };
    private readonly StatusStrip estado = new();
    private readonly ToolStripStatusLabel lblHora = new();
    private readonly System.Windows.Forms.Timer reloj = new() { Interval = 1000 };

    private readonly List<ItemMenu> definiciones = new();
    private readonly Dictionary<string, Button> botones = new();
    private VistaBase? vistaActual;

    public FrmPrincipal()
    {
        var u = Sesion.Actual ?? throw new InvalidOperationException("No hay una sesión iniciada.");

        Text = "Qute Aguilares";
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 780);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.Fondo;
        Font = Tema.Normal;

        ConstruirBarra(u);
        ConstruirMenu(u);
        ConstruirEstado();

        // Orden importante para el acoplamiento (Dock): Fill primero y Bottom al final.
        Controls.Add(host);
        Controls.Add(menu);
        Controls.Add(barra);
        Controls.Add(estado);

        Navegar(definiciones[0].Clave);

        reloj.Tick += (_, _) => lblHora.Text = DateTime.Now.ToString("dddd dd/MM/yyyy  HH:mm:ss");
        reloj.Start();

        FormClosing += (_, e) =>
        {
            if (!CerroSesion && e.CloseReason == CloseReason.UserClosing &&
                MessageBox.Show("¿Deseas salir de Qute Aguilares?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                e.Cancel = true;
        };
        FormClosed += (_, _) => reloj.Dispose();
    }

    // ---------------- Barra superior ----------------
    private void ConstruirBarra(UsuarioSesion u)
    {
        barra.Controls.Add(new Label
        {
            Text = "🛺  Qute Aguilares", ForeColor = Color.White,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 16)
        });

        var derecha = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 10, 16, 0)
        };

        var lblRed = new Label { Text = "● En línea", ForeColor = Color.FromArgb(0x7B, 0xE0, 0xA0), Font = Tema.Negrita, AutoSize = true, Margin = new Padding(0, 10, 24, 0) };
        var lblNotif = new Label { Text = "🔔 2", ForeColor = Color.White, Font = Tema.Negrita, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(0, 10, 24, 0) };
        lblNotif.Click += (_, _) => MessageBox.Show(NotificacionesDemo(u.Rol), "Notificaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);

        var lblUsuario = new Label
        {
            Text = $"{u.NombreCompleto}\n{u.RolTexto}", ForeColor = Color.White, Font = Tema.Pequena,
            AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(0, 2, 18, 0)
        };

        var btnSalir = Tema.Boton("Cerrar sesión", Tema.Peligro, 130, 38);
        btnSalir.Margin = new Padding(0, 2, 0, 0);
        btnSalir.Click += (_, _) =>
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar sesión", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            CerroSesion = true;
            Sesion.Actual = null;
            Close();
        };

        derecha.Controls.Add(lblRed);
        derecha.Controls.Add(lblNotif);
        derecha.Controls.Add(lblUsuario);
        derecha.Controls.Add(btnSalir);
        barra.Controls.Add(derecha);
    }

    private static string NotificacionesDemo(RolUsuario rol) => rol switch
    {
        RolUsuario.Pasajero => "• Tu último viaje fue finalizado.\n• Recuerda calificar a tu conductor.",
        RolUsuario.Conductor => "• Tienes nuevas solicitudes cercanas.\n• Tu calificación semanal subió a 4.8 ★.",
        _ => "• 2 solicitudes de conductores esperan aprobación.\n• 3 incidentes pendientes de revisión."
    };

    // ---------------- Menú lateral por rol ----------------
    private void ConstruirMenu(UsuarioSesion u)
    {
        menu.Controls.Add(new Label
        {
            Text = $"MENÚ · {u.RolTexto.ToUpper()}", ForeColor = Color.FromArgb(0x9F, 0xB4, 0xE0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), AutoSize = false, Size = new Size(240, 28), Padding = new Padding(20, 6, 0, 0)
        });

        definiciones.AddRange(MenuPorRol(u.Rol));

        foreach (var item in definiciones)
        {
            var b = new Button
            {
                Text = item.Texto, Width = 240, Height = 52, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0), Font = Tema.Normal, ForeColor = Color.White, BackColor = Tema.PrimarioOscuro,
                Cursor = Cursors.Hand, Margin = Padding.Empty, UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Tema.Primario;
            var clave = item.Clave;
            b.Click += (_, _) => Navegar(clave);
            botones[item.Clave] = b;
            menu.Controls.Add(b);
        }
    }

    private static List<ItemMenu> MenuPorRol(RolUsuario rol) => rol switch
    {
        RolUsuario.Pasajero => new List<ItemMenu>
        {
            new ItemMenu("inicio", "🏠   Inicio", () => new VistaInicioPasajero()),
            new ItemMenu("pedir", "🛺   Pedir qute", () => new VistaPedirQute()),
            new ItemMenu("miviaje", "📍   Mi viaje", () => new VistaMiViaje()),
            new ItemMenu("historial", "🕘   Historial", () => new VistaHistorial(false)),
            new ItemMenu("incidente", "⚠️   Reportar incidente", () => new VistaIncidente()),
            new ItemMenu("perfil", "👤   Mi perfil", () => new VistaPerfil()),
        },
        RolUsuario.Conductor => new List<ItemMenu>
        {
            new ItemMenu("inicio", "🏠   Inicio", () => new VistaInicioConductor()),
            new ItemMenu("solicitudes", "📥   Solicitudes", () => new VistaSolicitudes()),
            new ItemMenu("viaje", "🛺   Viaje actual", () => new VistaViajeConductor()),
            new ItemMenu("historial", "💵   Historial y ganancias", () => new VistaHistorial(true)),
            new ItemMenu("incidente", "⚠️   Reportar incidente", () => new VistaIncidente()),
            new ItemMenu("perfil", "👤   Mi perfil y unidad", () => new VistaPerfil()),
        },
        _ => new List<ItemMenu>
        {
            new ItemMenu("inicio", "📊   Dashboard", () => new VistaDashboard()),
            new ItemMenu("conductores", "🧑   Conductores y unidades", () => new VistaConductoresAdmin()),
            new ItemMenu("usuarios", "👥   Usuarios", () => new VistaUsuariosAdmin()),
            new ItemMenu("tarifas", "💲   Tarifas", () => new VistaTarifas()),
            new ItemMenu("puntos", "📌   Puntos de referencia", () => new VistaPuntosReferencia()),
            new ItemMenu("incidentes", "⚠️   Incidentes", () => new VistaIncidentesAdmin()),
            new ItemMenu("reportes", "📑   Reportes", () => new VistaReportes()),
            new ItemMenu("perfil", "👤   Mi perfil", () => new VistaPerfil()),
        }
    };

    // ---------------- Barra de estado ----------------
    private void ConstruirEstado()
    {
        estado.Items.Add(new ToolStripStatusLabel("📍 Cobertura: Aguilares y cantones cercanos"));
        estado.Items.Add(new ToolStripStatusLabel { Spring = true });
        estado.Items.Add(lblHora);
        estado.Items.Add(new ToolStripStatusLabel("   v1.0 demo"));
    }

    // ---------------- Navegación entre vistas ----------------
    private void Navegar(string clave)
    {
        var item = definiciones.FirstOrDefault(i => i.Clave == clave);
        if (item == null) return;

        var anterior = vistaActual;
        vistaActual = item.Crear();
        vistaActual.Navegar = Navegar;
        host.Controls.Add(vistaActual);

        if (anterior != null)
        {
            host.Controls.Remove(anterior);
            anterior.Dispose();
        }

        foreach (var kv in botones)
        {
            bool activo = kv.Key == clave;
            kv.Value.BackColor = activo ? Tema.Primario : Tema.PrimarioOscuro;
            kv.Value.Font = activo ? Tema.Negrita : Tema.Normal;
        }
    }
}
