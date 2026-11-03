using System.Drawing;

namespace QuteApp.Servicios;

/// <summary>Tarifa vigente (RN-04): base + costo/km × distancia, nunca menor a la mínima.</summary>
public static class Tarifas
{
    public static decimal Base { get; set; } = 0.75m;
    public static decimal CostoKm { get; set; } = 0.50m;
    public static decimal Minima { get; set; } = 1.00m;
    public static DateTime VigenteDesde { get; set; } = DateTime.Today;

    public static decimal Calcular(double km) => Math.Max(Minima, Base + CostoKm * (decimal)km);
}

/// <summary>Puntos de referencia de Aguilares con posición normalizada (0..1) sobre el mapa de demostración.</summary>
public static class Mapa
{
    public static readonly Dictionary<string, PointF> Puntos = new()
    {
        ["Mercado Municipal"] = new PointF(0.28f, 0.38f),
        ["Parque Central"] = new PointF(0.48f, 0.50f),
        ["Iglesia"] = new PointF(0.50f, 0.28f),
        ["Alcaldía"] = new PointF(0.60f, 0.52f),
        ["Unidad de Salud"] = new PointF(0.70f, 0.28f),
        ["Terminal de buses"] = new PointF(0.20f, 0.72f),
        ["Colonia Norte"] = new PointF(0.38f, 0.12f),
        ["Cantón Sur"] = new PointF(0.82f, 0.82f),
    };

    public static readonly PointF Gps = new(0.44f, 0.62f);

    public static double Km(PointF a, PointF b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Round(Math.Max(0.4, Math.Sqrt(dx * dx + dy * dy) * 5.0), 1);
    }

    public static PointF Medio(PointF a, PointF b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
}

public enum EstadoViaje { Solicitado, Aceptado, EnCamino, EnCurso, Finalizado, Cancelado, Expirado }

public static class EstadoViajeExt
{
    public static string Texto(this EstadoViaje e) => e switch
    {
        EstadoViaje.Solicitado => "Solicitado",
        EstadoViaje.Aceptado => "Aceptado",
        EstadoViaje.EnCamino => "En camino",
        EstadoViaje.EnCurso => "En curso",
        EstadoViaje.Finalizado => "Finalizado",
        EstadoViaje.Cancelado => "Cancelado",
        _ => "Expirado"
    };

    public static bool EstaActivo(this EstadoViaje e) =>
        e is EstadoViaje.Solicitado or EstadoViaje.Aceptado or EstadoViaje.EnCamino or EstadoViaje.EnCurso;
}

/// <summary>Viaje del lado del pasajero (demo).</summary>
public class ViajeDemo
{
    public string Origen { get; set; } = "";
    public string Destino { get; set; } = "";
    public PointF PosOrigen { get; set; }
    public PointF PosDestino { get; set; }
    public PointF PosConductor { get; set; }
    public double Km { get; set; }
    public decimal Tarifa { get; set; }
    public EstadoViaje Estado { get; set; } = EstadoViaje.Solicitado;
    public string Conductor { get; set; } = "";
    public string Placa { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Color { get; set; } = "";
    public double Calificacion { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fin { get; set; }
}

public static class ViajeActual
{
    public static ViajeDemo? Pasajero { get; set; }
}

/// <summary>Solicitud que le llega al conductor (demo).</summary>
public class SolicitudDemo
{
    private static readonly string[] Nombres = { "Ana Martínez", "Luis Pérez", "Rosa Gómez", "Mateo Flores", "Sofía Rivas", "Diego Cruz" };

    public string Pasajero { get; set; } = "";
    public string Origen { get; set; } = "";
    public string Destino { get; set; } = "";
    public PointF PosOrigen { get; set; }
    public PointF PosDestino { get; set; }
    public double Km { get; set; }
    public decimal Tarifa { get; set; }
    public int Segundos { get; set; } = 120; // RN-06

    public static SolicitudDemo Aleatoria(Random r)
    {
        var claves = Mapa.Puntos.Keys.ToList();
        string o = claves[r.Next(claves.Count)], d;
        do { d = claves[r.Next(claves.Count)]; } while (d == o);
        double km = Mapa.Km(Mapa.Puntos[o], Mapa.Puntos[d]);
        return new SolicitudDemo
        {
            Pasajero = Nombres[r.Next(Nombres.Length)],
            Origen = o, Destino = d,
            PosOrigen = Mapa.Puntos[o], PosDestino = Mapa.Puntos[d],
            Km = km, Tarifa = Tarifas.Calcular(km)
        };
    }
}

/// <summary>Estado del conductor logueado (demo).</summary>
public static class EstadoConductor
{
    public static bool Disponible { get; set; }
    public static SolicitudDemo? Activo { get; set; }
    public static EstadoViaje EstadoActivo { get; set; }
    public static DateTime? Inicio { get; set; }
    public static PointF Pos { get; set; } = Mapa.Gps;
    public static decimal GananciasHoy { get; set; } = 12.50m;
    public static int ViajesHoy { get; set; } = 7;
    public static int CancelacionesSeguidas { get; set; } // RN-07
}

public class IncidenteDemo
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Reporta { get; set; } = "";
    public string Viaje { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Estado { get; set; } = "pendiente";
}

public static class Incidentes
{
    public static readonly List<IncidenteDemo> Lista = new()
    {
        new IncidenteDemo { Id = 1, Fecha = DateTime.Now.AddDays(-2), Reporta = "María López", Tipo = "Objeto olvidado", Viaje = "#1031", Descripcion = "Dejé una mochila azul en el qute.", Estado = "en_revision" },
        new IncidenteDemo { Id = 2, Fecha = DateTime.Now.AddDays(-1), Reporta = "Luis Pérez", Tipo = "Cobro incorrecto", Viaje = "#1044", Descripcion = "Me cobraron más de la tarifa estimada.", Estado = "pendiente" },
        new IncidenteDemo { Id = 3, Fecha = DateTime.Now.AddHours(-5), Reporta = "Rosa Gómez", Tipo = "Seguridad", Viaje = "#1050", Descripcion = "El conductor tomó un camino distinto.", Estado = "pendiente" },
    };
}
