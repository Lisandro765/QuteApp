using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Vistas;

/// <summary>Inicio del conductor: disponibilidad (RF-10), resumen del día, unidad y mapa.</summary>
public class VistaInicioConductor : VistaBase
{
    private readonly Label lblEstado = new() { AutoSize = true, Font = Tema.Grande, Location = new Point(18, 12) };
    private readonly Button btnToggle = Tema.Boton("", Tema.Exito, 340, 54);
    private readonly MapaControl mapa = new() { Size = new Size(440, 270), Margin = new Padding(0, 0, 16, 16) };

    public VistaInicioConductor() : base($"¡Hola, {Sesion.Actual?.Nombre}!", "Controla tu disponibilidad y revisa tu día")
    {
        var u = Sesion.Actual!;

        if (!u.Aprobado) // RN-01
        {
            var aviso = new Tarjeta { Height = 70, BackColor = Color.FromArgb(0xFF, 0xF4, 0xD6) };
            aviso.Controls.Add(new Label
            {
                Text = u.EstadoAprobacion == Aprobacion.Rechazado
                    ? "⛔ Tu solicitud fue rechazada por el administrador. Contacta a la cooperativa."
                    : "⏳ Tu cuenta y tu vehículo están pendientes de aprobación. Aún no puedes recibir solicitudes.",
                Font = Tema.Negrita, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            Contenido.Controls.Add(aviso);
            AnchoCompleto(aviso);
            Salto(aviso);
        }

        var disp = new Tarjeta { Width = 400, Height = 160 };
        disp.Controls.Add(lblEstado);
        disp.Controls.Add(new Label { Text = "Tu ubicación solo se comparte mientras estás disponible.", Font = Tema.Pequena, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(20, 56) });
        btnToggle.Location = new Point(20, 84);
        disp.Controls.Add(btnToggle);
        Contenido.Controls.Add(disp);

        Contenido.Controls.Add(Tema.Kpi("Ganancias de hoy", $"${EstadoConductor.GananciasHoy:0.00}", Tema.Exito, "Pago en efectivo"));
        Contenido.Controls.Add(Tema.Kpi("Viajes de hoy", EstadoConductor.ViajesHoy.ToString(), Tema.Primario, "Finalizados"));
        var kCal = Tema.Kpi("Mi calificación", $"★ {u.Calificacion:0.0}", Tema.Acento, "Promedio de pasajeros");
        Contenido.Controls.Add(kCal);
        Salto(kCal);

        var unidad = new Tarjeta { Width = 400, Height = 270 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f.Controls.Add(Tema.Etiqueta("🛺 Mi unidad", Tema.Subtitulo, Tema.Primario));
        f.Controls.Add(Tema.Etiqueta($"Placa: {u.Placa}"));
        f.Controls.Add(Tema.Etiqueta($"Unidad: {(string.IsNullOrWhiteSpace(u.NumeroUnidad) ? "—" : u.NumeroUnidad)}"));
        f.Controls.Add(Tema.Etiqueta($"Color: {u.ColorQute}"));
        f.Controls.Add(Tema.Etiqueta($"Capacidad: {u.Capacidad} pasajeros"));
        f.Controls.Add(Tema.Etiqueta($"Cancelaciones seguidas: {EstadoConductor.CancelacionesSeguidas} / 3", Tema.Normal, Tema.TextoSuave));
        unidad.Controls.Add(f);
        Contenido.Controls.Add(unidad);
        Contenido.Controls.Add(mapa);

        btnToggle.Click += (_, _) =>
        {
            if (EstadoConductor.Activo != null) return;
            EstadoConductor.Disponible = !EstadoConductor.Disponible;
            Actualizar();
        };
        Actualizar();
    }

    private void Actualizar()
    {
        var u = Sesion.Actual!;
        bool ocupado = EstadoConductor.Activo != null;
        bool d = EstadoConductor.Disponible && !ocupado;

        lblEstado.Text = ocupado ? "🟡 OCUPADO" : d ? "🟢 DISPONIBLE" : "⚫ NO DISPONIBLE";
        lblEstado.ForeColor = ocupado ? Tema.Acento : d ? Tema.Exito : Tema.TextoSuave;
        btnToggle.Text = ocupado ? "Tienes un viaje activo" : d ? "Ponerme no disponible" : "Ponerme disponible";
        btnToggle.BackColor = d ? Tema.Peligro : Tema.Exito;
        btnToggle.Enabled = u.Aprobado && !ocupado;
        mapa.Conductor = d || ocupado ? EstadoConductor.Pos : null;
        mapa.Invalidate();
    }
}

/// <summary>RF-07, RF-08, RN-01, RN-02, RN-06: solicitudes cercanas con cuenta regresiva de 2 minutos.</summary>
public class VistaSolicitudes : VistaBase
{
    private readonly List<SolicitudDemo> lista = new();
    private readonly DataGridView grid = Tema.Grid("Pasajero", "Origen", "Destino", "Km", "Tarifa", "Expira en");
    private readonly MapaControl mapa = new() { Size = new Size(560, 300), Margin = new Padding(0, 0, 16, 16) };
    private readonly System.Windows.Forms.Timer reloj = new() { Interval = 1000 };
    private readonly Random azar = new();
    private int ticks;

    public VistaSolicitudes() : base("Solicitudes cercanas", "Acepta o rechaza antes de que expiren (2 minutos)")
    {
        var u = Sesion.Actual!;
        bool puede = u.Aprobado && EstadoConductor.Disponible && EstadoConductor.Activo == null;

        if (!puede)
        {
            var aviso = new Tarjeta { Height = 64, BackColor = Color.FromArgb(0xFF, 0xF4, 0xD6) };
            aviso.Controls.Add(new Label
            {
                Text = !u.Aprobado ? "⏳ Tu cuenta aún no está aprobada, no puedes recibir solicitudes."
                     : EstadoConductor.Activo != null ? "🟡 Tienes un viaje activo. Termínalo para recibir otras solicitudes."
                     : "⚫ Ponte DISPONIBLE en la pantalla de Inicio para recibir solicitudes.",
                Font = Tema.Negrita, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            Contenido.Controls.Add(aviso);
            AnchoCompleto(aviso);
            Salto(aviso);
        }
        else
        {
            for (int i = 0; i < 3; i++) lista.Add(SolicitudDemo.Aleatoria(azar));
        }

        grid.Height = 240;
        Contenido.Controls.Add(grid);
        AnchoCompleto(grid);
        Salto(grid);

        var btnAceptar = Tema.Boton("✔ Aceptar solicitud", Tema.Exito, 220, 46);
        var btnRechazar = Tema.Boton("✖ Rechazar", Tema.Peligro, 160, 46);
        var fila = new FlowLayoutPanel { Width = 420, Height = 56 };
        fila.Controls.Add(btnAceptar);
        fila.Controls.Add(btnRechazar);
        Contenido.Controls.Add(fila);
        Salto(fila);

        Contenido.Controls.Add(mapa);
        btnAceptar.Enabled = btnRechazar.Enabled = puede;

        grid.SelectionChanged += (_, _) => MostrarMapa();
        btnAceptar.Click += (_, _) => Aceptar();
        btnRechazar.Click += (_, _) =>
        {
            var s = Seleccionada();
            if (s == null) return;
            lista.Remove(s);
            Llenar();
        };
        reloj.Tick += (_, _) => Tick();
        Llenar();
        if (puede) reloj.Start();
    }

    private static string Formato(int seg) => $"{seg / 60:0}:{seg % 60:00}";

    private SolicitudDemo? Seleccionada() => grid.CurrentRow?.Tag as SolicitudDemo;

    private void Llenar()
    {
        var previa = Seleccionada();
        grid.Rows.Clear();
        foreach (var s in lista)
        {
            int i = grid.Rows.Add(s.Pasajero, s.Origen, s.Destino, $"{s.Km:0.0}", $"${s.Tarifa:0.00}", Formato(s.Segundos));
            grid.Rows[i].Tag = s;
            if (s == previa) grid.Rows[i].Selected = true;
        }
        MostrarMapa();
    }

    private void MostrarMapa()
    {
        var s = Seleccionada();
        mapa.Origen = s?.PosOrigen;
        mapa.Destino = s?.PosDestino;
        mapa.Conductor = EstadoConductor.Disponible ? EstadoConductor.Pos : null;
        mapa.Invalidate();
    }

    private void Tick()
    {
        ticks++;
        foreach (var s in lista) s.Segundos--;
        bool cambio = lista.RemoveAll(s => s.Segundos <= 0) > 0; // RN-06: expira sola
        if (ticks % 20 == 0 && lista.Count < 5)
        {
            lista.Add(SolicitudDemo.Aleatoria(azar));
            cambio = true;
        }

        if (cambio) { Llenar(); return; }
        foreach (DataGridViewRow r in grid.Rows)
            if (r.Tag is SolicitudDemo s) r.Cells[5].Value = Formato(s.Segundos);
    }

    private void Aceptar()
    {
        var u = Sesion.Actual!;
        var s = Seleccionada();
        if (s == null) { MessageBox.Show("Selecciona una solicitud de la lista.", "Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (!u.Aprobado) { MessageBox.Show("Tu cuenta y vehículo deben estar aprobados.", "Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }        // RN-01
        if (EstadoConductor.Activo != null) { MessageBox.Show("Ya tienes un viaje activo.", "Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }   // RN-02

        reloj.Stop();
        EstadoConductor.Activo = s;
        EstadoConductor.EstadoActivo = EstadoViaje.Aceptado;   // CA-04: el conductor pasa a "ocupado"
        Navegar?.Invoke("viaje");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { reloj.Stop(); reloj.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>RF-11, RF-12, RF-13, RN-07, RN-12: iniciar/finalizar viaje, registrar pago en efectivo o cancelar con motivo.</summary>
public class VistaViajeConductor : VistaBase
{
    private readonly SolicitudDemo? s = EstadoConductor.Activo;
    private readonly MapaControl mapa = new() { Size = new Size(540, 520), Margin = new Padding(0, 0, 16, 16) };
    private readonly Label lblEstado = Tema.Etiqueta("", Tema.Subtitulo, Tema.Primario);
    private readonly Button btnSiguiente = Tema.Boton("", Tema.Primario, 380, 54);
    private readonly ComboBox cboMotivo = Tema.Combo(new[] { "El pasajero no apareció", "Problema con el vehículo", "Emergencia personal", "Otro" }, 380);
    private readonly Button btnCancelar = Tema.Boton("✖ Cancelar viaje", Tema.Peligro, 380, 42);
    private readonly Control campoMotivo;

    public VistaViajeConductor() : base("Viaje actual", "Gestiona el recorrido paso a paso")
    {
        campoMotivo = Tema.Campo("Motivo de cancelación", cboMotivo, 380);

        if (s == null)
        {
            Contenido.Controls.Add(EstadoVacio("🛺", "No tienes un viaje activo", "Acepta una solicitud para comenzar.", "📥 Ver solicitudes", "solicitudes"));
            return;
        }

        var card = new Tarjeta { Width = 420, Height = 520 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f.Controls.Add(lblEstado);
        f.Controls.Add(Tema.Etiqueta("Pasajero", Tema.Negrita, Tema.TextoSuave));
        f.Controls.Add(Tema.Etiqueta(s.Pasajero));
        f.Controls.Add(Tema.Etiqueta("Recorrido", Tema.Negrita, Tema.TextoSuave));
        f.Controls.Add(Tema.Etiqueta($"{s.Origen}  →  {s.Destino}\n{s.Km:0.0} km  ·  Cobrar ${s.Tarifa:0.00} en efectivo"));
        f.Controls.Add(btnSiguiente);
        f.Controls.Add(campoMotivo);
        f.Controls.Add(btnCancelar);
        card.Controls.Add(f);
        Contenido.Controls.Add(card);
        Contenido.Controls.Add(mapa);

        btnSiguiente.Click += (_, _) => Siguiente();
        btnCancelar.Click += (_, _) => Cancelar();
        Refrescar();
    }

    private void Refrescar()
    {
        var e = EstadoConductor.EstadoActivo;
        lblEstado.Text = "Estado: " + e.Texto();
        (btnSiguiente.Text, btnSiguiente.BackColor) = e switch
        {
            EstadoViaje.Aceptado => ("🚗 Voy en camino", Tema.Primario),
            EstadoViaje.EnCamino => ("📍 Llegué · Iniciar viaje", Tema.Exito),
            _ => ("🏁 Finalizar viaje y cobrar", Tema.Acento)
        };
        btnSiguiente.ForeColor = e == EstadoViaje.EnCurso ? Tema.Texto : Color.White;
        campoMotivo.Visible = btnCancelar.Visible = e < EstadoViaje.EnCurso; // RF-12: solo antes de iniciar

        mapa.Origen = s!.PosOrigen;
        mapa.Destino = s.PosDestino;
        mapa.Conductor = EstadoConductor.Pos;
        mapa.Invalidate();
    }

    private void Siguiente()
    {
        switch (EstadoConductor.EstadoActivo)
        {
            case EstadoViaje.Aceptado:
                EstadoConductor.EstadoActivo = EstadoViaje.EnCamino;
                EstadoConductor.Pos = Mapa.Medio(EstadoConductor.Pos, s!.PosOrigen);
                break;
            case EstadoViaje.EnCamino:
                EstadoConductor.EstadoActivo = EstadoViaje.EnCurso;
                EstadoConductor.Inicio = DateTime.Now;               // RF-11: hora de inicio
                EstadoConductor.Pos = s!.PosOrigen;
                break;
            case EstadoViaje.EnCurso:
                Finalizar();
                return;
        }
        Refrescar();
    }

    private void Finalizar()
    {
        var fin = DateTime.Now;                                       // RF-11: hora de fin
        var dur = fin - (EstadoConductor.Inicio ?? fin);
        var r = MessageBox.Show(
            $"Hora de fin: {fin:HH:mm:ss}\nDuración: {(int)dur.TotalMinutes:00}:{dur.Seconds:00}\n\nMonto a cobrar: ${s!.Tarifa:0.00}\n\n¿Recibiste el pago en efectivo?",
            "Registrar pago", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (r != DialogResult.Yes)
        {
            MessageBox.Show("El viaje no puede finalizarse sin registrar el pago.", "Pago pendiente", MessageBoxButtons.OK, MessageBoxIcon.Warning); // RN-12
            return;
        }

        EstadoConductor.GananciasHoy += s.Tarifa;
        EstadoConductor.ViajesHoy++;
        EstadoConductor.CancelacionesSeguidas = 0;
        EstadoConductor.Activo = null;
        EstadoConductor.Disponible = true;                           // CA-06: se libera al conductor
        EstadoConductor.Pos = s.PosDestino;
        MessageBox.Show("Viaje finalizado y pago registrado.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Navegar?.Invoke("inicio");
    }

    private void Cancelar()
    {
        if (cboMotivo.SelectedIndex < 0) { MessageBox.Show("Selecciona el motivo de la cancelación.", "Cancelar viaje", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (MessageBox.Show("¿Seguro que deseas cancelar el viaje?", "Cancelar viaje", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        EstadoConductor.Activo = null;
        EstadoConductor.Disponible = true;
        EstadoConductor.CancelacionesSeguidas++;                     // RN-07
        if (EstadoConductor.CancelacionesSeguidas >= 3)
            MessageBox.Show("Acumulas 3 cancelaciones seguidas. Se envió una alerta al administrador.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Navegar?.Invoke("inicio");
    }
}
