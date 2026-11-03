using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Vistas;

/// <summary>Dashboard del administrador: KPIs, viajes de la semana y alertas (RN-07, RN-09).</summary>
public class VistaDashboard : VistaBase
{
    public VistaDashboard() : base("Dashboard", "Resumen de la operación en Aguilares")
    {
        int pendientes = AuthService.Usuarios.Count(x => x.Rol == RolUsuario.Conductor && x.EstadoAprobacion == Aprobacion.Pendiente);
        int incPend = Incidentes.Lista.Count(i => i.Estado == "pendiente");

        Contenido.Controls.Add(Tema.Kpi("Viajes hoy", "38", Tema.Primario, "+12% vs ayer"));
        Contenido.Controls.Add(Tema.Kpi("Conductores activos", "12 / 18", Tema.Exito, "Disponibles ahora"));
        Contenido.Controls.Add(Tema.Kpi("Ingresos del día", "$96.50", Tema.Exito, "Pagos en efectivo"));
        Contenido.Controls.Add(Tema.Kpi("Calificación promedio", "★ 4.7", Tema.Acento, "Últimos 30 días"));
        Contenido.Controls.Add(Tema.Kpi("Incidentes pendientes", incPend.ToString(), Tema.Peligro, "Requieren atención"));
        var k = Tema.Kpi("Conductores por aprobar", pendientes.ToString(), Tema.Acento, "Solicitudes de registro");
        Contenido.Controls.Add(k);
        Salto(k);

        var tBarras = new Tarjeta { Width = 560, Height = 280 };
        var barras = new BarrasControl
        {
            Dock = DockStyle.Fill,
            Etiquetas = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(i - 6).ToString("ddd")).ToArray(),
            Valores = new double[] { 31, 28, 35, 40, 33, 45, 38 }
        };
        tBarras.Controls.Add(barras);
        tBarras.Controls.Add(new Label { Text = "Viajes de la última semana", Font = Tema.Subtitulo, ForeColor = Tema.Primario, Dock = DockStyle.Top, Height = 34 });
        Contenido.Controls.Add(tBarras);

        var tAlertas = new Tarjeta { Width = 460, Height = 280 };
        tAlertas.Controls.Add(new Label
        {
            Text = "⚠ Carlos H. acumula 3 cancelaciones seguidas (RN-07).\n\n⚠ Un conductor tiene promedio 2.8 con 12 calificaciones: revisar (RN-09).\n\nℹ " + pendientes + " solicitud(es) de conductor por revisar.\n\nℹ " + incPend + " incidente(s) pendiente(s).",
            Font = Tema.Normal, ForeColor = Tema.TextoSuave, Dock = DockStyle.Fill
        });
        tAlertas.Controls.Add(new Label { Text = "Alertas", Font = Tema.Subtitulo, ForeColor = Tema.Primario, Dock = DockStyle.Top, Height = 34 });
        Contenido.Controls.Add(tAlertas);
    }
}

/// <summary>RF-04: aprobar o rechazar conductores y vehículos.</summary>
public class VistaConductoresAdmin : VistaBase
{
    private readonly DataGridView grid = Tema.Grid("Nombre", "Teléfono", "DUI", "Licencia", "Placa", "Unidad", "Estado");
    private readonly ComboBox cbo = Tema.Combo(new[] { "Todos", "Pendiente", "Aprobado", "Rechazado" }, 200);

    public VistaConductoresAdmin() : base("Conductores y unidades", "Aprueba o rechaza solicitudes de registro")
    {
        var fila = new FlowLayoutPanel { Width = 420, Height = 44 };
        fila.Controls.Add(new Label { Text = "Estado:", AutoSize = true, Font = Tema.Negrita, Margin = new Padding(0, 6, 10, 0) });
        fila.Controls.Add(cbo);
        Contenido.Controls.Add(fila);
        Salto(fila);

        grid.Height = 320;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);
        Salto(grid);

        var aprobar = Tema.Boton("✔ Aprobar", Tema.Exito, 160, 46);
        var rechazar = Tema.Boton("✖ Rechazar", Tema.Peligro, 160, 46);
        var fb = new FlowLayoutPanel { Width = 420, Height = 56 };
        fb.Controls.Add(aprobar);
        fb.Controls.Add(rechazar);
        Contenido.Controls.Add(fb);

        aprobar.Click += (_, _) => Cambiar(Aprobacion.Aprobado);
        rechazar.Click += (_, _) => Cambiar(Aprobacion.Rechazado);
        cbo.SelectedIndexChanged += (_, _) => Llenar();
        cbo.SelectedIndex = 0;
    }

    private void Llenar()
    {
        grid.Rows.Clear();
        foreach (var u in AuthService.Usuarios.Where(x => x.Rol == RolUsuario.Conductor))
        {
            if (cbo.Text != "Todos" && u.EstadoAprobacion.ToString() != cbo.Text) continue;
            int i = grid.Rows.Add(u.NombreCompleto, u.Telefono, u.Dui, u.Licencia, u.Placa, u.NumeroUnidad, u.EstadoAprobacion);
            grid.Rows[i].Tag = u;
        }
    }

    private void Cambiar(Aprobacion nuevo)
    {
        if (grid.CurrentRow?.Tag is not UsuarioSesion u) { MessageBox.Show("Selecciona un conductor.", "Conductores", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        u.EstadoAprobacion = nuevo; // CA-02
        Llenar();
    }
}

/// <summary>RF-19: suspender o reactivar usuarios y conductores.</summary>
public class VistaUsuariosAdmin : VistaBase
{
    private readonly DataGridView grid = Tema.Grid("Nombre", "Teléfono", "Correo", "Rol", "Cuenta");
    private readonly TextBox txtBuscar = Tema.Caja("Buscar por nombre o teléfono");

    public VistaUsuariosAdmin() : base("Usuarios", "Suspende o reactiva cuentas")
    {
        txtBuscar.Width = 320;
        txtBuscar.Margin = new Padding(0, 0, 0, 10);
        Contenido.Controls.Add(txtBuscar);
        Salto(txtBuscar);

        grid.Height = 330;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);
        Salto(grid);

        var susp = Tema.Boton("⛔ Suspender", Tema.Peligro, 170, 46);
        var react = Tema.Boton("♻ Reactivar", Tema.Exito, 170, 46);
        var fb = new FlowLayoutPanel { Width = 420, Height = 56 };
        fb.Controls.Add(susp);
        fb.Controls.Add(react);
        Contenido.Controls.Add(fb);

        txtBuscar.TextChanged += (_, _) => Llenar();
        susp.Click += (_, _) => Cambiar(true);
        react.Click += (_, _) => Cambiar(false);
        Llenar();
    }

    private void Llenar()
    {
        grid.Rows.Clear();
        var q = txtBuscar.Text.Trim().ToLower();
        foreach (var u in AuthService.Usuarios)
        {
            if (q.Length > 0 && !u.NombreCompleto.ToLower().Contains(q) && !u.Telefono.Contains(q)) continue;
            int i = grid.Rows.Add(u.NombreCompleto, u.Telefono, u.Correo ?? "—", u.RolTexto, u.Suspendido ? "Suspendido" : "Activo");
            grid.Rows[i].Tag = u;
        }
    }

    private void Cambiar(bool suspender)
    {
        if (grid.CurrentRow?.Tag is not UsuarioSesion u) { MessageBox.Show("Selecciona un usuario.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (u.Rol == RolUsuario.Admin) { MessageBox.Show("No se puede suspender a un administrador.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        u.Suspendido = suspender;
        Llenar();
    }
}

/// <summary>RF-17 / CA-09: configurar tarifa base, costo por km y mínima.</summary>
public class VistaTarifas : VistaBase
{
    private static NumericUpDown Num(decimal v) => new() { DecimalPlaces = 2, Increment = 0.05m, Minimum = 0, Maximum = 50, Value = v };

    public VistaTarifas() : base("Tarifas", "Los cambios aplican solo a nuevos cálculos")
    {
        var nBase = Num(Tarifas.Base);
        var nKm = Num(Tarifas.CostoKm);
        var nMin = Num(Tarifas.Minima);
        var nDist = new NumericUpDown { DecimalPlaces = 1, Minimum = 0.1m, Maximum = 30, Value = 2.0m, Increment = 0.5m };
        var lblSim = Tema.Etiqueta("", Tema.Grande, Tema.Exito);
        var guardar = Tema.Boton("💾 Guardar tarifa", Tema.Primario, 380, 46);

        var hist = Tema.Grid("Vigente desde", "Base", "Por km", "Mínima", "Estado");
        hist.Height = 200;
        hist.Rows.Add(Tarifas.VigenteDesde.ToString("dd/MM/yyyy"), $"${Tarifas.Base:0.00}", $"${Tarifas.CostoKm:0.00}", $"${Tarifas.Minima:0.00}", "Activa");

        void Simular() => lblSim.Text = "$" + Math.Max(nMin.Value, nBase.Value + nKm.Value * nDist.Value).ToString("0.00"); // RN-04
        nBase.ValueChanged += (_, _) => Simular();
        nKm.ValueChanged += (_, _) => Simular();
        nMin.ValueChanged += (_, _) => Simular();
        nDist.ValueChanged += (_, _) => Simular();

        guardar.Click += (_, _) =>
        {
            Tarifas.Base = nBase.Value; Tarifas.CostoKm = nKm.Value; Tarifas.Minima = nMin.Value; Tarifas.VigenteDesde = DateTime.Today;
            foreach (DataGridViewRow r in hist.Rows) r.Cells[4].Value = "Anterior";
            hist.Rows.Insert(0, DateTime.Today.ToString("dd/MM/yyyy"), $"${nBase.Value:0.00}", $"${nKm.Value:0.00}", $"${nMin.Value:0.00}", "Activa");
            MessageBox.Show("Tarifa guardada. Los nuevos cálculos usarán estos valores; los viajes finalizados no cambian.", "Tarifas", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var t1 = new Tarjeta { Width = 420, Height = 360 };
        var f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f1.Controls.Add(Tema.Etiqueta("Tarifa vigente", Tema.Subtitulo, Tema.Primario));
        f1.Controls.Add(Tema.Campo("Tarifa base ($)", nBase, 380));
        f1.Controls.Add(Tema.Campo("Costo por kilómetro ($)", nKm, 380));
        f1.Controls.Add(Tema.Campo("Tarifa mínima por viaje ($)", nMin, 380));
        f1.Controls.Add(guardar);
        t1.Controls.Add(f1);
        Contenido.Controls.Add(t1);

        var t2 = new Tarjeta { Width = 420, Height = 360 };
        var f2 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f2.Controls.Add(Tema.Etiqueta("Simulador", Tema.Subtitulo, Tema.Primario));
        f2.Controls.Add(Tema.Campo("Distancia del viaje (km)", nDist, 380));
        f2.Controls.Add(Tema.Etiqueta("Tarifa estimada:", Tema.Negrita, Tema.TextoSuave));
        f2.Controls.Add(lblSim);
        f2.Controls.Add(Tema.Etiqueta("Fórmula: base + (costo/km × distancia),\nnunca menor a la tarifa mínima.", Tema.Pequena, Tema.TextoSuave));
        t2.Controls.Add(f2);
        Contenido.Controls.Add(t2);
        Salto(t2);

        Contenido.Controls.Add(hist);
        AnchoCompleto(hist);
        Simular();
    }
}

/// <summary>RF-18: gestionar puntos de referencia (mercado, parque, iglesia, colonias, cantones).</summary>
public class VistaPuntosReferencia : VistaBase
{
    private class Punto
    {
        public string Nombre = "", Tipo = "";
        public double Lat, Lng;
        public PointF Pos;
        public bool Activo = true;
    }

    private readonly List<Punto> puntos = new();
    private readonly DataGridView grid = Tema.Grid("Nombre", "Tipo", "Latitud", "Longitud", "Visible en la app");

    private static double LatDe(PointF p) => Math.Round(13.975 - p.Y * 0.036, 6);
    private static double LngDe(PointF p) => Math.Round(-89.205 + p.X * 0.04, 6);

    public VistaPuntosReferencia() : base("Puntos de referencia", "Lugares conocidos que el pasajero elige como origen o destino")
    {
        foreach (var kv in Mapa.Puntos)
        {
            string tipo = kv.Key.Contains("Mercado") ? "Mercado" : kv.Key.Contains("Iglesia") ? "Iglesia" : kv.Key.Contains("Parque") ? "Parque"
                : kv.Key.Contains("Colonia") ? "Colonia" : kv.Key.Contains("Cantón") ? "Cantón" : "Institución";
            puntos.Add(new Punto { Nombre = kv.Key, Tipo = tipo, Pos = kv.Value, Lat = LatDe(kv.Value), Lng = LngDe(kv.Value) });
        }

        grid.Height = 280;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);
        Salto(grid);

        var alternar = Tema.Boton("👁 Mostrar / ocultar", Tema.Primario, 200, 44);
        alternar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Tag is not Punto p) return;
            p.Activo = !p.Activo;
            if (p.Activo) Mapa.Puntos[p.Nombre] = p.Pos; else Mapa.Puntos.Remove(p.Nombre);
            Llenar();
        };
        Contenido.Controls.Add(alternar);
        Salto(alternar);

        var txtNombre = Tema.Caja("Ej. Cancha municipal");
        var cboTipo = Tema.Combo(new[] { "Mercado", "Iglesia", "Parque", "Escuela", "Colonia", "Cantón", "Institución" }, 200);
        var nLat = new NumericUpDown { DecimalPlaces = 6, Minimum = 13.9m, Maximum = 14.1m, Value = 13.957m, Increment = 0.001m };
        var nLng = new NumericUpDown { DecimalPlaces = 6, Minimum = -89.3m, Maximum = -89.1m, Value = -89.188m, Increment = 0.001m };
        var agregar = Tema.Boton("➕ Agregar punto", Tema.Exito, 200, 44);

        agregar.Click += (_, _) =>
        {
            string nombre = txtNombre.Text.Trim();
            if (nombre.Length == 0 || cboTipo.SelectedIndex < 0) { MessageBox.Show("Escribe el nombre y elige el tipo.", "Puntos", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (puntos.Any(p => p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("Ya existe un punto con ese nombre.", "Puntos", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            float x = (float)Math.Clamp(((double)nLng.Value + 89.205) / 0.04, 0.02, 0.98);
            float y = (float)Math.Clamp((13.975 - (double)nLat.Value) / 0.036, 0.02, 0.98);
            var punto = new Punto { Nombre = nombre, Tipo = cboTipo.Text, Pos = new PointF(x, y), Lat = (double)nLat.Value, Lng = (double)nLng.Value };
            puntos.Add(punto);
            Mapa.Puntos[nombre] = punto.Pos;
            txtNombre.Clear();
            Llenar();
        };

        var card = new Tarjeta { Width = 900, Height = 150 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        f.Controls.Add(Tema.Campo("Nombre", txtNombre, 240));
        f.Controls.Add(Tema.Campo("Tipo", cboTipo, 180));
        f.Controls.Add(Tema.Campo("Latitud", nLat, 150));
        f.Controls.Add(Tema.Campo("Longitud", nLng, 150));
        agregar.Margin = new Padding(0, 22, 0, 0);
        f.Controls.Add(agregar);
        card.Controls.Add(f);
        Contenido.Controls.Add(card);
        AnchoCompleto(card);

        Llenar();
    }

    private void Llenar()
    {
        grid.Rows.Clear();
        foreach (var p in puntos)
        {
            int i = grid.Rows.Add(p.Nombre, p.Tipo, p.Lat.ToString("0.000000"), p.Lng.ToString("0.000000"), p.Activo ? "Sí" : "No");
            grid.Rows[i].Tag = p;
        }
    }
}

/// <summary>Seguimiento de incidentes reportados por pasajeros y conductores (CA-10).</summary>
public class VistaIncidentesAdmin : VistaBase
{
    private readonly DataGridView grid = Tema.Grid("N°", "Fecha", "Reporta", "Viaje", "Tipo", "Descripción", "Estado");

    public VistaIncidentesAdmin() : base("Incidentes", "Atiende los reportes de seguridad y calidad")
    {
        grid.Height = 320;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);
        Salto(grid);

        var cbo = Tema.Combo(new[] { "pendiente", "en_revision", "resuelto" }, 220);
        var btn = Tema.Boton("Actualizar estado", Tema.Primario, 200, 44);
        var fila = new FlowLayoutPanel { Width = 520, Height = 56 };
        fila.Controls.Add(new Label { Text = "Nuevo estado:", AutoSize = true, Font = Tema.Negrita, Margin = new Padding(0, 8, 10, 0) });
        fila.Controls.Add(cbo);
        fila.Controls.Add(btn);
        Contenido.Controls.Add(fila);

        btn.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Tag is not IncidenteDemo inc || cbo.SelectedIndex < 0) { MessageBox.Show("Selecciona un incidente y un estado.", "Incidentes", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            inc.Estado = cbo.Text;
            Llenar();
        };
        Llenar();
    }

    private void Llenar()
    {
        grid.Rows.Clear();
        foreach (var i in Incidentes.Lista.OrderByDescending(x => x.Fecha))
        {
            int idx = grid.Rows.Add(i.Id, i.Fecha.ToString("dd/MM/yyyy HH:mm"), i.Reporta, i.Viaje, i.Tipo, i.Descripcion, i.Estado);
            grid.Rows[idx].Tag = i;
        }
    }
}

/// <summary>RF-20 / CA-12: reportes por rango de fechas (viajes, monto total y calificación promedio) con exportación a CSV.</summary>
public class VistaReportes : VistaBase
{
    private record Fila(DateTime Fecha, int Viajes, decimal Monto, double Calif);

    private readonly DateTimePicker dtDesde = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };
    private readonly DateTimePicker dtHasta = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
    private readonly DataGridView grid = Tema.Grid("Fecha", "Viajes", "Monto", "Calificación");
    private readonly Tarjeta kViajes = Tema.Kpi("Total de viajes", "0", Tema.Primario);
    private readonly Tarjeta kMonto = Tema.Kpi("Monto total", "$0.00", Tema.Exito);
    private readonly Tarjeta kCalif = Tema.Kpi("Calificación promedio", "—", Tema.Acento);
    private List<Fila> filas = new();

    public VistaReportes() : base("Reportes", "Viajes, ganancias y calificaciones por rango de fechas")
    {
        var generar = Tema.Boton("📑 Generar", Tema.Primario, 140, 40);
        var exportar = Tema.Boton("⬇ Exportar CSV", Tema.Exito, 160, 40);

        var fila = new FlowLayoutPanel { Width = 760, Height = 52 };
        fila.Controls.Add(new Label { Text = "Desde:", AutoSize = true, Font = Tema.Negrita, Margin = new Padding(0, 8, 6, 0) });
        dtDesde.Margin = new Padding(0, 6, 16, 0); dtDesde.Width = 130;
        fila.Controls.Add(dtDesde);
        fila.Controls.Add(new Label { Text = "Hasta:", AutoSize = true, Font = Tema.Negrita, Margin = new Padding(0, 8, 6, 0) });
        dtHasta.Margin = new Padding(0, 6, 16, 0); dtHasta.Width = 130;
        fila.Controls.Add(dtHasta);
        fila.Controls.Add(generar);
        fila.Controls.Add(exportar);
        Contenido.Controls.Add(fila);
        Salto(fila);

        Contenido.Controls.Add(kViajes);
        Contenido.Controls.Add(kMonto);
        Contenido.Controls.Add(kCalif);
        Salto(kCalif);

        grid.Height = 300;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);

        generar.Click += (_, _) => Generar();
        exportar.Click += (_, _) => Exportar();
        Generar();
    }

    private void Generar()
    {
        var d = dtDesde.Value.Date;
        var h = dtHasta.Value.Date;
        if (h < d || (h - d).TotalDays > 366) { MessageBox.Show("Rango de fechas inválido (máximo 1 año).", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        // Datos de demostración generados de forma determinista; aquí iría la consulta a la base de datos.
        filas = new List<Fila>();
        for (var dia = d; dia <= h; dia = dia.AddDays(1))
        {
            int viajes = 20 + (dia.DayOfYear * 7) % 25;
            filas.Add(new Fila(dia, viajes, viajes * 2.1m, 4.2 + (dia.Day % 6) / 10.0));
        }

        grid.Rows.Clear();
        foreach (var f in filas) grid.Rows.Add(f.Fecha.ToString("dd/MM/yyyy"), f.Viajes, $"${f.Monto:0.00}", $"★ {f.Calif:0.0}");

        Tema.ActualizarKpi(kViajes, filas.Sum(f => f.Viajes).ToString());
        Tema.ActualizarKpi(kMonto, $"${filas.Sum(f => f.Monto):0.00}");
        Tema.ActualizarKpi(kCalif, filas.Count > 0 ? $"★ {filas.Average(f => f.Calif):0.0}" : "—");
    }

    private void Exportar()
    {
        if (filas.Count == 0) return;
        using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"reporte_{dtDesde.Value:yyyyMMdd}_{dtHasta.Value:yyyyMMdd}.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var sb = new StringBuilder("Fecha,Viajes,Monto,Calificacion\n");
        foreach (var f in filas)
            sb.AppendLine(FormattableString.Invariant($"{f.Fecha:yyyy-MM-dd},{f.Viajes},{f.Monto:0.00},{f.Calif:0.0}"));
        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        MessageBox.Show("Reporte exportado correctamente.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
