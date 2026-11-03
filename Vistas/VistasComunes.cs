using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Vistas;

/// <summary>Base de todas las secciones del área de contenido: encabezado + panel con scroll.</summary>
public abstract class VistaBase : UserControl
{
    /// <summary>Lo asigna FrmPrincipal para que una vista pueda saltar a otra (ej. "pedir", "miviaje").</summary>
    public Action<string>? Navegar { get; set; }

    protected readonly FlowLayoutPanel Contenido = new()
    {
        Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true, Padding = new Padding(0, 8, 0, 0)
    };

    protected VistaBase(string titulo, string subtitulo)
    {
        Dock = DockStyle.Fill;
        BackColor = Tema.Fondo;
        Padding = new Padding(24, 16, 24, 8);

        var cab = new Panel { Dock = DockStyle.Top, Height = 70 };
        cab.Controls.Add(new Label { Text = subtitulo, Font = Tema.Normal, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(2, 42) });
        cab.Controls.Add(new Label { Text = titulo, Font = Tema.Titulo, ForeColor = Tema.Texto, AutoSize = true, Location = new Point(0, 0) });
        Controls.Add(Contenido);
        Controls.Add(cab);
    }

    /// <summary>Hace que un control ocupe todo el ancho disponible del contenido.</summary>
    protected void AnchoCompleto(Control c, int resta = 20)
    {
        void Ajustar() => c.Width = Math.Max(320, Contenido.ClientSize.Width - resta);
        Contenido.SizeChanged += (_, _) => Ajustar();
        Ajustar();
    }

    protected void Salto(Control c) => Contenido.SetFlowBreak(c, true);

    protected Tarjeta EstadoVacio(string icono, string titulo, string detalle, string? textoBoton = null, string? destino = null)
    {
        var t = new Tarjeta { Width = 520, Height = 230 };
        t.Controls.Add(new Label { Text = icono, Font = new Font("Segoe UI", 28f), AutoSize = true, Location = new Point(20, 14) });
        t.Controls.Add(new Label { Text = titulo, Font = Tema.Subtitulo, AutoSize = true, Location = new Point(20, 80) });
        t.Controls.Add(new Label { Text = detalle, Font = Tema.Normal, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(20, 112) });
        if (textoBoton != null && destino != null)
        {
            var b = Tema.Boton(textoBoton, Tema.Primario, 220, 44);
            b.Location = new Point(20, 154);
            b.Click += (_, _) => Navegar?.Invoke(destino);
            t.Controls.Add(b);
        }
        return t;
    }
}

/// <summary>RF-15: historial de viajes (pasajero) o historial y ganancias (conductor).</summary>
public class VistaHistorial : VistaBase
{
    private static string Fecha(int dias, string hora) => DateTime.Today.AddDays(-dias).ToString("dd/MM/yyyy") + " " + hora;

    public VistaHistorial(bool esConductor)
        : base(esConductor ? "Historial y ganancias" : "Mi historial de viajes", "Consulta tus viajes anteriores y su estado")
    {
        if (esConductor)
        {
            Contenido.Controls.Add(Tema.Kpi("Ganancias de hoy", $"${EstadoConductor.GananciasHoy:0.00}", Tema.Exito, $"{EstadoConductor.ViajesHoy} viajes"));
            Contenido.Controls.Add(Tema.Kpi("Esta semana", "$74.25", Tema.Primario, "41 viajes"));
            Contenido.Controls.Add(Tema.Kpi("Este mes", "$298.00", Tema.Acento, "168 viajes"));
            var canc = Tema.Kpi("Cancelaciones seguidas", $"{EstadoConductor.CancelacionesSeguidas} / 3", Tema.Peligro, "Con 3 se alerta al administrador");
            Contenido.Controls.Add(canc);
            Salto(canc);
        }

        var cbo = Tema.Combo(new[] { "Todos", "Finalizado", "Cancelado" }, 200);
        var fila = new FlowLayoutPanel { Width = 420, Height = 44 };
        fila.Controls.Add(new Label { Text = "Filtrar por estado:", AutoSize = true, Font = Tema.Negrita, Margin = new Padding(0, 6, 10, 0) });
        fila.Controls.Add(cbo);
        Contenido.Controls.Add(fila);
        Salto(fila);

        var otro = esConductor ? "Ana Martínez" : "Carlos Hernández";
        var datos = new List<object[]>
        {
            new object[] { Fecha(0, "08:15"), "Mercado Municipal → Colonia Norte", otro, "$1.75", "Finalizado", "★ 5" },
            new object[] { Fecha(0, "07:40"), "Parque Central → Unidad de Salud", otro, "$1.50", "Finalizado", "★ 5" },
            new object[] { Fecha(1, "19:22"), "Terminal de buses → Cantón Sur", otro, "$3.25", "Finalizado", "★ 4" },
            new object[] { Fecha(2, "12:05"), "Iglesia → Alcaldía", otro, "$1.00", "Cancelado", "—" },
            new object[] { Fecha(3, "17:48"), "Colonia Norte → Mercado Municipal", otro, "$1.75", "Finalizado", "★ 5" },
            new object[] { Fecha(5, "06:55"), "Alcaldía → Terminal de buses", otro, "$2.25", "Finalizado", "★ 4" },
        };

        var grid = Tema.Grid("Fecha", "Ruta", esConductor ? "Pasajero" : "Conductor", "Monto", "Estado", "Calificación");
        grid.Height = 300;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);

        void Llenar()
        {
            grid.Rows.Clear();
            foreach (var d in datos)
                if (cbo.Text == "Todos" || (string)d[4] == cbo.Text) grid.Rows.Add(d);
        }
        cbo.SelectedIndexChanged += (_, _) => Llenar();
        cbo.SelectedIndex = 0;
    }
}

/// <summary>RF-16: reportar un incidente asociado a un viaje (queda "pendiente" para el administrador: CA-10).</summary>
public class VistaIncidente : VistaBase
{
    public VistaIncidente() : base("Reportar incidente", "Cuéntanos qué pasó en tu viaje")
    {
        var cboViaje = Tema.Combo(new[]
        {
            "#1050 · Mercado Municipal → Colonia Norte · hoy",
            "#1044 · Parque Central → Unidad de Salud · ayer",
            "#1031 · Terminal de buses → Cantón Sur · hace 3 días"
        }, 470);
        var cboTipo = Tema.Combo(new[] { "Objeto olvidado", "Cobro incorrecto", "Trato inadecuado", "Seguridad", "Otro" }, 470);
        var txt = new TextBox { Multiline = true, Height = 120, MaxLength = 500, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Describe lo ocurrido con el mayor detalle posible…" };
        var lblCont = Tema.Etiqueta("0 / 500", Tema.Pequena, Tema.TextoSuave);
        var btn = Tema.Boton("Enviar reporte", Tema.Peligro, 220, 46);

        txt.TextChanged += (_, _) => lblCont.Text = $"{txt.TextLength} / 500";
        btn.Click += (_, _) =>
        {
            if (cboViaje.SelectedIndex < 0 || cboTipo.SelectedIndex < 0 || string.IsNullOrWhiteSpace(txt.Text))
            {
                MessageBox.Show("Selecciona el viaje, el tipo de incidente y escribe una descripción.", "Faltan datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Incidentes.Lista.Add(new IncidenteDemo
            {
                Id = Incidentes.Lista.Count + 1, Reporta = Sesion.Actual!.NombreCompleto,
                Viaje = cboViaje.Text.Split(' ')[0], Tipo = cboTipo.Text, Descripcion = txt.Text.Trim(), Estado = "pendiente"
            });
            MessageBox.Show("Tu reporte fue enviado con estado «pendiente». El administrador lo revisará.", "Incidente registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txt.Clear(); cboViaje.SelectedIndex = -1; cboTipo.SelectedIndex = -1;
        };

        var card = new Tarjeta { Width = 520, Height = 480 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f.Controls.Add(Tema.Campo("Viaje relacionado", cboViaje, 480));
        f.Controls.Add(Tema.Campo("Tipo de incidente", cboTipo, 480));
        f.Controls.Add(Tema.Campo("Descripción", txt, 480));
        f.Controls.Add(lblCont);
        f.Controls.Add(btn);
        card.Controls.Add(f);
        Contenido.Controls.Add(card);

        var ayuda = new Tarjeta { Width = 340, Height = 200 };
        ayuda.Controls.Add(new Label
        {
            Text = "Si estás en peligro, llama al 911.\n\nTodos los reportes quedan ligados al viaje, al conductor y a la unidad registrada, para que el administrador pueda dar seguimiento.",
            Font = Tema.Normal, ForeColor = Tema.TextoSuave, Dock = DockStyle.Fill
        });
        Contenido.Controls.Add(ayuda);
    }
}

/// <summary>Datos de la cuenta, vehículo (conductor), contacto de confianza (pasajero) y cambio de contraseña.</summary>
public class VistaPerfil : VistaBase
{
    private static TextBox Solo(string texto) => new() { Text = texto, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Tema.Fondo };

    public VistaPerfil() : base("Mi perfil", "Tus datos y la seguridad de tu cuenta")
    {
        var u = Sesion.Actual!;

        var datos = new Tarjeta { Width = 420, Height = 360 };
        var f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f1.Controls.Add(Tema.Etiqueta("Datos personales", Tema.Subtitulo, Tema.Primario));
        f1.Controls.Add(Tema.Campo("Nombre completo", Solo(u.NombreCompleto), 380));
        f1.Controls.Add(Tema.Campo("Teléfono", Solo(u.Telefono), 380));
        f1.Controls.Add(Tema.Campo("Correo", Solo(u.Correo ?? "—"), 380));
        f1.Controls.Add(Tema.Campo("Rol", Solo(u.RolTexto), 380));
        datos.Controls.Add(f1);
        Contenido.Controls.Add(datos);

        if (u.Rol == RolUsuario.Conductor)
        {
            var veh = new Tarjeta { Width = 420, Height = 440 };
            var f2 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            f2.Controls.Add(Tema.Etiqueta("Mi unidad", Tema.Subtitulo, Tema.Primario));
            var (txtEstado, colorEstado) = u.EstadoAprobacion switch
            {
                Aprobacion.Aprobado => ("Aprobado", Tema.Exito),
                Aprobacion.Rechazado => ("Rechazado", Tema.Peligro),
                _ => ("Pendiente de aprobación", Tema.Acento)
            };
            f2.Controls.Add(Tema.Badge(txtEstado, colorEstado));
            f2.Controls.Add(Tema.Campo("Placa", Solo(u.Placa), 380));
            f2.Controls.Add(Tema.Campo("Número de unidad / color", Solo($"{(string.IsNullOrWhiteSpace(u.NumeroUnidad) ? "—" : u.NumeroUnidad)}  ·  {u.ColorQute}"), 380));
            f2.Controls.Add(Tema.Campo("DUI / Licencia", Solo($"{u.Dui}  ·  {u.Licencia}"), 380));
            f2.Controls.Add(Tema.Campo("Capacidad · Calificación", Solo($"{u.Capacidad} pasajeros  ·  ★ {u.Calificacion:0.0}"), 380));
            veh.Controls.Add(f2);
            Contenido.Controls.Add(veh);
        }
        else if (u.Rol == RolUsuario.Pasajero)
        {
            var cont = new Tarjeta { Width = 420, Height = 280 };
            var f3 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            f3.Controls.Add(Tema.Etiqueta("Contacto de confianza", Tema.Subtitulo, Tema.Primario));
            var nom = Tema.Caja("Nombre del contacto");
            var tel = Tema.Caja("Teléfono del contacto");
            var guardar = Tema.Boton("Guardar contacto", Tema.Exito, 200, 42);
            guardar.Click += (_, _) =>
            {
                if (!AuthService.EsTelefonoValido(tel.Text.Trim())) { MessageBox.Show("Ingresa un teléfono válido de 8 dígitos.", "Contacto", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                MessageBox.Show("Contacto guardado. Podrás compartirle tu viaje en curso.", "Contacto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            f3.Controls.Add(Tema.Campo("Nombre", nom, 380));
            f3.Controls.Add(Tema.Campo("Teléfono", tel, 380));
            f3.Controls.Add(guardar);
            cont.Controls.Add(f3);
            Contenido.Controls.Add(cont);
        }

        var seg = new Tarjeta { Width = 420, Height = 360 };
        var f4 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f4.Controls.Add(Tema.Etiqueta("Cambiar contraseña", Tema.Subtitulo, Tema.Primario));
        var actual = Tema.Caja(null, true); var nueva = Tema.Caja("Mínimo 6 caracteres", true); var repite = Tema.Caja(null, true);
        f4.Controls.Add(Tema.Campo("Contraseña actual", actual, 380));
        f4.Controls.Add(Tema.Campo("Nueva contraseña", nueva, 380));
        f4.Controls.Add(Tema.Campo("Confirmar nueva contraseña", repite, 380));
        var btn = Tema.Boton("Actualizar contraseña", Tema.Primario, 220, 42);
        btn.Click += (_, _) =>
        {
            if (nueva.Text.Length < 6 || nueva.Text != repite.Text) { MessageBox.Show("La nueva contraseña debe tener 6+ caracteres y coincidir.", "Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            MessageBox.Show("(Demo) Aquí se guardaría el nuevo hash en la base de datos.", "Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Information);
            actual.Clear(); nueva.Clear(); repite.Clear();
        };
        f4.Controls.Add(btn);
        seg.Controls.Add(f4);
        Contenido.Controls.Add(seg);
    }
}
