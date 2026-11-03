using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Vistas;

/// <summary>Inicio del pasajero: acceso rápido, resumen, puntos de referencia, últimos viajes y consejos.</summary>
public class VistaInicioPasajero : VistaBase
{
    public VistaInicioPasajero() : base($"¡Hola, {Sesion.Actual?.Nombre}!", "¿A dónde vamos hoy? Pide tu qute en pocos pasos")
    {
        var viaje = ViajeActual.Pasajero;
        bool activo = viaje != null && viaje.Estado.EstaActivo();

        var hero = new Tarjeta { Width = 520, Height = 160, BackColor = Tema.Primario };
        hero.Controls.Add(new Label { Text = "Pide tu qute ahora", Font = Tema.Subtitulo, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) });
        hero.Controls.Add(new Label { Text = "Verás la tarifa antes de confirmar y los datos del conductor.", Font = Tema.Normal, ForeColor = Color.FromArgb(0xD7, 0xE2, 0xFB), AutoSize = true, Location = new Point(20, 52) });
        var btn = Tema.Boton(activo ? "📍 Ver mi viaje" : "🛺 Pedir qute", Tema.Acento, 220, 48, Tema.Texto);
        btn.Location = new Point(20, 92);
        btn.Click += (_, _) => Navegar?.Invoke(activo ? "miviaje" : "pedir");
        hero.Controls.Add(btn);
        Contenido.Controls.Add(hero);
        Salto(hero);

        Contenido.Controls.Add(Tema.Kpi("Viajes realizados", "12", Tema.Primario, "Desde que te uniste"));
        Contenido.Controls.Add(Tema.Kpi("Mi calificación", "★ 4.9", Tema.Acento, "Promedio de conductores"));
        Contenido.Controls.Add(Tema.Kpi("Gasto del mes", "$18.50", Tema.Exito, "Pago en efectivo"));
        var kEstado = Tema.Kpi("Viaje activo", activo ? viaje!.Estado.Texto() : "Ninguno", activo ? Tema.Exito : Tema.TextoSuave, activo ? $"{viaje!.Origen} → {viaje.Destino}" : "Pide uno cuando quieras");
        Contenido.Controls.Add(kEstado);
        Salto(kEstado);

        // Últimos viajes
        var tViajes = new Tarjeta { Width = 520, Height = 230 };
        var g = Tema.Grid("Fecha", "Ruta", "Monto");
        g.Rows.Add(DateTime.Today.ToString("dd/MM"), "Mercado → Colonia Norte", "$1.75");
        g.Rows.Add(DateTime.Today.AddDays(-1).ToString("dd/MM"), "Parque → Unidad de Salud", "$1.50");
        g.Rows.Add(DateTime.Today.AddDays(-3).ToString("dd/MM"), "Terminal → Cantón Sur", "$3.25");
        g.Dock = DockStyle.Fill;
        tViajes.Controls.Add(g);
        tViajes.Controls.Add(new Label { Text = "Últimos viajes", Font = Tema.Subtitulo, ForeColor = Tema.Primario, Dock = DockStyle.Top, Height = 34 });
        Contenido.Controls.Add(tViajes);

        // Puntos de referencia
        var tPuntos = new Tarjeta { Width = 320, Height = 230 };
        var lista = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = Tema.Normal };
        foreach (var k in Mapa.Puntos.Keys) lista.Items.Add("📌  " + k);
        lista.DoubleClick += (_, _) => Navegar?.Invoke("pedir");
        tPuntos.Controls.Add(lista);
        tPuntos.Controls.Add(new Label { Text = "Puntos de referencia", Font = Tema.Subtitulo, ForeColor = Tema.Primario, Dock = DockStyle.Top, Height = 34 });
        Contenido.Controls.Add(tPuntos);

        // Seguridad
        var tSeg = new Tarjeta { Width = 340, Height = 230 };
        tSeg.Controls.Add(new Label
        {
            Text = "✔  Verifica la placa y el número de unidad antes de subir.\n\n✔  Comparte tu viaje con un contacto de confianza.\n\n✔  Si algo sale mal, repórtalo desde «Reportar incidente».",
            Font = Tema.Normal, ForeColor = Tema.TextoSuave, Dock = DockStyle.Fill
        });
        tSeg.Controls.Add(new Label { Text = "Viaja seguro", Font = Tema.Subtitulo, ForeColor = Tema.Primario, Dock = DockStyle.Top, Height = 34 });
        Contenido.Controls.Add(tSeg);
    }
}

/// <summary>RF-05, RF-06, RN-03, RN-06: solicitar un viaje, ver la tarifa estimada antes de confirmar y esperar a un conductor (2 min).</summary>
public class VistaPedirQute : VistaBase
{
    private readonly ComboBox cboOrigen = Tema.Combo(Mapa.Puntos.Keys, 380);
    private readonly ComboBox cboDestino = Tema.Combo(Mapa.Puntos.Keys, 380);
    private readonly CheckBox chkGps = new() { Text = "📡 Usar mi ubicación (GPS)", AutoSize = true, Checked = true, Font = Tema.Normal };
    private readonly MapaControl mapa = new() { Size = new Size(540, 580), Margin = new Padding(0, 0, 16, 16) };
    private readonly Tarjeta pnlResultado = new() { Width = 380, Height = 96, Visible = false, BackColor = Color.FromArgb(0xF3, 0xF7, 0xFF) };
    private readonly Label lblDistancia = new() { AutoSize = true, Font = Tema.Normal, Location = new Point(14, 10) };
    private readonly Label lblTarifa = new() { AutoSize = true, Font = Tema.Grande, ForeColor = Tema.Exito, Location = new Point(12, 36) };
    private readonly Button btnCalcular = Tema.Boton("💲 Calcular tarifa", Tema.Primario, 380, 46);
    private readonly Button btnConfirmar = Tema.Boton("✔ Confirmar solicitud", Tema.Exito, 380, 50);
    private readonly Panel pnlBusqueda = new() { Width = 380, Height = 100, Visible = false };
    private readonly Label lblBuscando = new() { AutoSize = true, Font = Tema.Negrita, ForeColor = Tema.Primario, Location = new Point(0, 4) };
    private readonly System.Windows.Forms.Timer reloj = new() { Interval = 1000 };

    private double km;
    private decimal tarifa;
    private int segundos, hastaAcepta;

    public VistaPedirQute() : base("Pedir qute", "Elige origen y destino. Verás la tarifa antes de confirmar")
    {
        var card = new Tarjeta { Width = 420, Height = 580 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f.Controls.Add(chkGps);
        f.Controls.Add(Tema.Campo("Origen", cboOrigen, 380));
        f.Controls.Add(Tema.Campo("Destino", cboDestino, 380));
        f.Controls.Add(btnCalcular);

        pnlResultado.Controls.Add(lblDistancia);
        pnlResultado.Controls.Add(lblTarifa);
        f.Controls.Add(pnlResultado);
        f.Controls.Add(btnConfirmar);

        var btnCancelar = Tema.Boton("✖ Cancelar solicitud", Tema.Peligro, 380, 42);
        pnlBusqueda.Controls.Add(lblBuscando);
        pnlBusqueda.Controls.Add(new Label { Text = "No cambies de pantalla mientras buscamos.", Font = Tema.Pequena, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(0, 30) });
        btnCancelar.Location = new Point(0, 54);
        pnlBusqueda.Controls.Add(btnCancelar);
        f.Controls.Add(pnlBusqueda);
        card.Controls.Add(f);

        Contenido.Controls.Add(card);
        Contenido.Controls.Add(mapa);

        btnConfirmar.Enabled = false;
        chkGps.CheckedChanged += (_, _) => { cboOrigen.Enabled = !chkGps.Checked; ActualizarMapa(); };
        cboOrigen.Enabled = false;
        cboOrigen.SelectedIndexChanged += (_, _) => ActualizarMapa();
        cboDestino.SelectedIndexChanged += (_, _) => { pnlResultado.Visible = false; btnConfirmar.Enabled = false; ActualizarMapa(); };
        btnCalcular.Click += (_, _) => Calcular();
        btnConfirmar.Click += (_, _) => Confirmar();
        btnCancelar.Click += (_, _) => CancelarBusqueda();
        reloj.Tick += (_, _) => Tick();
        ActualizarMapa();
    }

    private PointF PosOrigen() => chkGps.Checked || cboOrigen.SelectedIndex < 0 ? Mapa.Gps : Mapa.Puntos[cboOrigen.Text];
    private string TextoOrigen() => chkGps.Checked || cboOrigen.SelectedIndex < 0 ? "Mi ubicación actual" : cboOrigen.Text;

    private void ActualizarMapa()
    {
        mapa.Origen = PosOrigen();
        mapa.Destino = cboDestino.SelectedIndex >= 0 ? Mapa.Puntos[cboDestino.Text] : null;
        mapa.Invalidate();
    }

    private void Calcular()
    {
        if (cboDestino.SelectedIndex < 0) { Aviso("Elige un destino."); return; }
        if (!chkGps.Checked && cboOrigen.SelectedIndex < 0) { Aviso("Elige un origen o activa el GPS."); return; }
        if (!chkGps.Checked && cboOrigen.Text == cboDestino.Text) { Aviso("El origen y el destino no pueden ser el mismo."); return; }

        km = Mapa.Km(PosOrigen(), Mapa.Puntos[cboDestino.Text]);
        tarifa = Tarifas.Calcular(km); // RN-04
        lblDistancia.Text = $"Distancia estimada: {km:0.0} km · Pago en efectivo";
        lblTarifa.Text = $"${tarifa:0.00}";
        pnlResultado.Visible = true;
        btnConfirmar.Enabled = true;
    }

    private void Confirmar()
    {
        var previo = ViajeActual.Pasajero;
        if (previo != null && previo.Estado.EstaActivo()) // RN-03
        {
            Aviso("Ya tienes una solicitud o viaje activo. Cancélalo antes de pedir otro.");
            return;
        }

        ViajeActual.Pasajero = new ViajeDemo
        {
            Origen = TextoOrigen(), Destino = cboDestino.Text, PosOrigen = PosOrigen(), PosDestino = Mapa.Puntos[cboDestino.Text],
            Km = km, Tarifa = tarifa, Estado = EstadoViaje.Solicitado
        };
        segundos = 120;                           // RN-06
        hastaAcepta = new Random().Next(6, 12);   // simulación: un conductor acepta en unos segundos
        pnlBusqueda.Visible = true;
        btnCalcular.Enabled = false;
        btnConfirmar.Enabled = false;
        lblBuscando.Text = "Buscando conductores cercanos…  02:00";
        reloj.Start();
    }

    private void Tick()
    {
        segundos--;
        hastaAcepta--;
        lblBuscando.Text = $"Buscando conductores cercanos…  {segundos / 60:00}:{segundos % 60:00}";
        var v = ViajeActual.Pasajero!;

        if (hastaAcepta <= 0)
        {
            reloj.Stop();
            v.Estado = EstadoViaje.Aceptado;
            v.Conductor = "Carlos Hernández"; v.Placa = "P512-345"; v.Unidad = "07"; v.Color = "Rojo"; v.Calificacion = 4.8;
            v.PosConductor = new PointF(Math.Min(0.95f, v.PosOrigen.X + 0.14f), Math.Min(0.95f, v.PosOrigen.Y + 0.12f));
            MessageBox.Show("¡Un conductor aceptó tu solicitud!", "Qute en camino", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Navegar?.Invoke("miviaje");
            return;
        }

        if (segundos <= 0) // CA-05
        {
            reloj.Stop();
            v.Estado = EstadoViaje.Expirado;
            MessageBox.Show("Ningún conductor aceptó a tiempo. Intenta de nuevo.", "Solicitud expirada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reiniciar();
        }
    }

    private void CancelarBusqueda()
    {
        reloj.Stop();
        if (ViajeActual.Pasajero != null) ViajeActual.Pasajero.Estado = EstadoViaje.Cancelado;
        Reiniciar();
    }

    private void Reiniciar()
    {
        pnlBusqueda.Visible = false;
        btnCalcular.Enabled = true;
        btnConfirmar.Enabled = pnlResultado.Visible;
    }

    private static void Aviso(string m) => MessageBox.Show(m, "Pedir qute", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reloj.Stop();
            reloj.Dispose();
            var v = ViajeActual.Pasajero;
            if (v != null && v.Estado == EstadoViaje.Solicitado) v.Estado = EstadoViaje.Cancelado;
        }
        base.Dispose(disposing);
    }
}

/// <summary>RF-09, RF-12, RF-14, RF-21: seguimiento del viaje, datos del conductor, cancelar, compartir y calificar.</summary>
public class VistaMiViaje : VistaBase
{
    private static readonly EstadoViaje[] Pasos = { EstadoViaje.Aceptado, EstadoViaje.EnCamino, EstadoViaje.EnCurso, EstadoViaje.Finalizado };

    private readonly ViajeDemo? viaje = ViajeActual.Pasajero;
    private readonly MapaControl mapa = new() { Size = new Size(540, 600), Margin = new Padding(0, 0, 16, 16) };
    private readonly Label lblEstado = Tema.Etiqueta("", Tema.Subtitulo, Tema.Primario);
    private readonly Label lblPasos = Tema.Etiqueta("");
    private readonly Button btnAvanzar = Tema.Boton("▶ Simular avance (demo)", Tema.Primario, 380, 42);
    private readonly Button btnCancelar = Tema.Boton("✖ Cancelar viaje", Tema.Peligro, 380, 42);
    private readonly Tarjeta pnlCalificar = new() { Width = 420, Height = 280, Visible = false };

    public VistaMiViaje() : base("Mi viaje", "Seguimiento de tu qute")
    {
        if (viaje == null || viaje.Estado is EstadoViaje.Cancelado or EstadoViaje.Expirado or EstadoViaje.Solicitado)
        {
            Contenido.Controls.Add(EstadoVacio("📍", "Aún no tienes un viaje activo", "Cuando un conductor acepte tu solicitud lo verás aquí.", "🛺 Pedir qute", "pedir"));
            return;
        }

        var v = viaje;
        var card = new Tarjeta { Width = 420, Height = 600 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        f.Controls.Add(lblEstado);
        f.Controls.Add(lblPasos);
        f.Controls.Add(Tema.Etiqueta("Tu conductor", Tema.Negrita, Tema.TextoSuave));
        f.Controls.Add(Tema.Etiqueta($"{v.Conductor}   ★ {v.Calificacion:0.0}\nPlaca: {v.Placa}   ·   Unidad: {v.Unidad}   ·   Color: {v.Color}"));
        f.Controls.Add(Tema.Etiqueta("Recorrido", Tema.Negrita, Tema.TextoSuave));
        f.Controls.Add(Tema.Etiqueta($"{v.Origen}  →  {v.Destino}\n{v.Km:0.0} km  ·  Tarifa estimada ${v.Tarifa:0.00} (efectivo)"));
        f.Controls.Add(btnAvanzar);
        f.Controls.Add(btnCancelar);

        var txtContacto = Tema.Caja("Teléfono del contacto de confianza");
        var btnCompartir = Tema.Boton("📤 Compartir mi viaje", Tema.Exito, 380, 42);
        f.Controls.Add(Tema.Campo("Compartir con un contacto de confianza", txtContacto, 380));
        f.Controls.Add(btnCompartir);
        card.Controls.Add(f);

        Contenido.Controls.Add(card);
        Contenido.Controls.Add(mapa);
        Salto(mapa);

        // Calificación (RF-14, RN-08: solo una vez y solo con viaje finalizado)
        var cboPunt = Tema.Combo(new[] { "5 ★★★★★", "4 ★★★★", "3 ★★★", "2 ★★", "1 ★" }, 380);
        cboPunt.SelectedIndex = 0;
        var txtCom = Tema.Caja("Comentario (opcional)");
        var btnEnviar = Tema.Boton("Enviar calificación", Tema.Acento, 380, 42, Tema.Texto);
        var fc = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        fc.Controls.Add(Tema.Etiqueta("Califica a tu conductor", Tema.Subtitulo, Tema.Primario));
        fc.Controls.Add(Tema.Campo("Puntuación", cboPunt, 380));
        fc.Controls.Add(Tema.Campo("Comentario", txtCom, 380));
        fc.Controls.Add(btnEnviar);
        pnlCalificar.Controls.Add(fc);
        Contenido.Controls.Add(pnlCalificar);

        btnAvanzar.Click += (_, _) => Avanzar();
        btnCancelar.Click += (_, _) => Cancelar();
        btnCompartir.Click += (_, _) =>
        {
            if (!AuthService.EsTelefonoValido(txtContacto.Text.Trim())) { MessageBox.Show("Ingresa un teléfono válido de 8 dígitos.", "Compartir viaje", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            MessageBox.Show($"(Demo) Se enviaría el enlace de seguimiento al {txtContacto.Text.Trim()} por SMS/WhatsApp.", "Compartir viaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        btnEnviar.Click += (_, _) =>
        {
            MessageBox.Show("¡Gracias! Tu calificación fue registrada.", "Calificación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnEnviar.Enabled = false; cboPunt.Enabled = false; txtCom.Enabled = false;
        };

        Refrescar();
    }

    private void Refrescar()
    {
        var v = viaje!;
        lblEstado.Text = "Estado: " + v.Estado.Texto();
        lblPasos.Text = string.Join("\n", Pasos.Select(p => (p <= v.Estado ? "●   " : "○   ") + p.Texto()));
        mapa.Origen = v.PosOrigen;
        mapa.Destino = v.PosDestino;
        mapa.Conductor = v.PosConductor;
        mapa.Invalidate();
        btnCancelar.Visible = v.Estado < EstadoViaje.EnCurso;        // RF-12: solo antes de iniciar
        btnAvanzar.Visible = v.Estado < EstadoViaje.Finalizado;
        pnlCalificar.Visible = v.Estado == EstadoViaje.Finalizado;
    }

    private void Avanzar()
    {
        var v = viaje!;
        switch (v.Estado)
        {
            case EstadoViaje.Aceptado:
                v.Estado = EstadoViaje.EnCamino;
                v.PosConductor = Mapa.Medio(v.PosConductor, v.PosOrigen);
                break;
            case EstadoViaje.EnCamino:
                v.Estado = EstadoViaje.EnCurso;
                v.Inicio = DateTime.Now;
                v.PosConductor = v.PosOrigen;
                break;
            case EstadoViaje.EnCurso:
                v.Estado = EstadoViaje.Finalizado;
                v.Fin = DateTime.Now;
                v.PosConductor = v.PosDestino;
                MessageBox.Show($"Llegaste a tu destino. Paga ${v.Tarifa:0.00} en efectivo al conductor.", "Viaje finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
        }
        Refrescar();
    }

    private void Cancelar()
    {
        if (MessageBox.Show("¿Seguro que deseas cancelar este viaje?", "Cancelar viaje", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        viaje!.Estado = EstadoViaje.Cancelado; // CA-07
        Navegar?.Invoke("inicio");
    }
}
