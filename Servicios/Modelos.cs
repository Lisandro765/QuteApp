namespace QuteApp.Servicios;

public enum RolUsuario { Pasajero, Conductor, Admin }
public enum Aprobacion { Pendiente, Aprobado, Rechazado }

/// <summary>Datos del usuario con sesión iniciada (mapea USUARIO + CONDUCTOR + VEHICULO del diccionario de datos).</summary>
public class UsuarioSesion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Correo { get; set; }
    public RolUsuario Rol { get; set; }
    public bool Suspendido { get; set; }

    // Solo conductor
    public string Dui { get; set; } = "";
    public string Licencia { get; set; } = "";
    public Aprobacion EstadoAprobacion { get; set; } = Aprobacion.Aprobado;
    public string Placa { get; set; } = "";
    public string NumeroUnidad { get; set; } = "";
    public string ColorQute { get; set; } = "";
    public int Capacidad { get; set; } = 3;
    public double Calificacion { get; set; } = 5.0;

    public bool Aprobado => EstadoAprobacion == Aprobacion.Aprobado; // RN-01
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string RolTexto => Rol == RolUsuario.Admin ? "Administrador" : Rol.ToString();
}

public static class Sesion
{
    public static UsuarioSesion? Actual { get; set; }
}
