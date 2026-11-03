using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace QuteApp.Servicios;

public record Resultado(bool Ok, string Mensaje);

/// <summary>
/// Servicio de autenticación DE DEMOSTRACIÓN (datos en memoria).
/// Cuando conecten EF Core + SQL Server, solo hay que reemplazar el interior de estos métodos
/// por consultas al DbContext; las pantallas no cambian.
/// Contraseñas con PBKDF2 + sal (RNF-03: nunca en texto plano).
/// </summary>
public static class AuthService
{
    private sealed class Cuenta
    {
        public UsuarioSesion Usuario { get; init; } = new();
        public string Hash { get; init; } = "";
    }

    private static readonly List<Cuenta> Cuentas = new();
    private static int _siguienteId = 1;

    static AuthService()
    {
        Agregar(new UsuarioSesion { Nombre = "Admin", Apellido = "Cooperativa", Telefono = "70000000", Correo = "admin@qute.sv", Rol = RolUsuario.Admin }, "admin123");
        Agregar(new UsuarioSesion { Nombre = "María", Apellido = "López", Telefono = "71111111", Correo = "maria@correo.com", Rol = RolUsuario.Pasajero }, "123456");
        Agregar(new UsuarioSesion
        {
            Nombre = "Carlos", Apellido = "Hernández", Telefono = "72222222", Rol = RolUsuario.Conductor,
            Dui = "01234567-8", Licencia = "L-123456", EstadoAprobacion = Aprobacion.Aprobado,
            Placa = "P512-345", NumeroUnidad = "07", ColorQute = "Rojo", Capacidad = 3, Calificacion = 4.8
        }, "123456");
        Agregar(new UsuarioSesion
        {
            Nombre = "José", Apellido = "Ramírez", Telefono = "73333333", Rol = RolUsuario.Conductor,
            Dui = "09876543-2", Licencia = "L-654321", EstadoAprobacion = Aprobacion.Pendiente,
            Placa = "P777-888", NumeroUnidad = "12", ColorQute = "Azul", Capacidad = 3, Calificacion = 0
        }, "123456");
    }

    public static IReadOnlyList<UsuarioSesion> Usuarios => Cuentas.Select(c => c.Usuario).ToList();

    // ---------- Validaciones reutilizables ----------
    public static bool EsTelefonoValido(string t) => Regex.IsMatch(t, @"^[267]\d{7}$");
    public static bool EsCorreoValido(string c) => Regex.IsMatch(c, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    public static bool EsDuiValido(string d) => Regex.IsMatch(d, @"^\d{8}-\d$");

    // ---------- Inicio de sesión (RF-03) ----------
    public static (UsuarioSesion? Usuario, string Mensaje) IniciarSesion(string login, string pass)
    {
        login = login.Trim();
        var c = Cuentas.FirstOrDefault(x =>
            x.Usuario.Telefono == login ||
            (x.Usuario.Correo != null && x.Usuario.Correo.Equals(login, StringComparison.OrdinalIgnoreCase)));

        if (c == null || !Verificar(pass, c.Hash)) return (null, "Teléfono/correo o contraseña incorrectos.");
        if (c.Usuario.Suspendido) return (null, "Tu cuenta está suspendida. Contacta al administrador.");
        return (c.Usuario, "");
    }

    // ---------- Registro de pasajero (RF-01, CA-01) ----------
    public static Resultado RegistrarPasajero(string nombre, string apellido, string telefono, string? correo, string pass, string pass2)
    {
        var error = ValidarCuenta(nombre, apellido, telefono, correo, pass, pass2);
        if (error != null) return new Resultado(false, error);

        Agregar(new UsuarioSesion
        {
            Nombre = nombre.Trim(), Apellido = apellido.Trim(), Telefono = telefono.Trim(),
            Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim(), Rol = RolUsuario.Pasajero
        }, pass);
        return new Resultado(true, "Cuenta creada correctamente.");
    }

    // ---------- Registro de conductor (RF-02, queda PENDIENTE hasta que el admin apruebe: RN-01) ----------
    public static Resultado RegistrarConductor(string nombre, string apellido, string telefono, string? correo, string pass, string pass2,
        string dui, string licencia, string placa, string unidad, string color, int capacidad)
    {
        var error = ValidarCuenta(nombre, apellido, telefono, correo, pass, pass2);
        if (error != null) return new Resultado(false, error);
        if (!EsDuiValido(dui.Trim())) return new Resultado(false, "El DUI debe tener el formato 00000000-0.");
        if (Cuentas.Any(c => c.Usuario.Dui == dui.Trim())) return new Resultado(false, "Ese DUI ya está registrado.");
        if (string.IsNullOrWhiteSpace(licencia)) return new Resultado(false, "Ingresa el número de licencia.");
        if (!Regex.IsMatch(placa.Trim(), @"^[A-Za-z0-9\- ]{5,10}$")) return new Resultado(false, "Ingresa una placa válida (5 a 10 caracteres).");
        if (Cuentas.Any(c => c.Usuario.Placa.Equals(placa.Trim(), StringComparison.OrdinalIgnoreCase))) // RN-10
            return new Resultado(false, "Esa placa ya está asociada a otro conductor.");
        if (string.IsNullOrWhiteSpace(color)) return new Resultado(false, "Ingresa el color del qute.");

        Agregar(new UsuarioSesion
        {
            Nombre = nombre.Trim(), Apellido = apellido.Trim(), Telefono = telefono.Trim(),
            Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim(), Rol = RolUsuario.Conductor,
            Dui = dui.Trim(), Licencia = licencia.Trim(), Placa = placa.Trim().ToUpper(), NumeroUnidad = unidad.Trim(),
            ColorQute = color.Trim(), Capacidad = capacidad, EstadoAprobacion = Aprobacion.Pendiente, Calificacion = 0
        }, pass);
        return new Resultado(true, "Solicitud enviada. Podrás recibir viajes cuando el administrador apruebe tu cuenta y vehículo.");
    }

    // ---------- Internos ----------
    private static string? ValidarCuenta(string nombre, string apellido, string telefono, string? correo, string pass, string pass2)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido)) return "Ingresa tu nombre y apellido.";
        if (!EsTelefonoValido(telefono.Trim())) return "El teléfono debe tener 8 dígitos (ej. 71234567).";
        if (!string.IsNullOrWhiteSpace(correo) && !EsCorreoValido(correo.Trim())) return "El correo no tiene un formato válido.";
        if (pass.Length < 6) return "La contraseña debe tener al menos 6 caracteres.";
        if (pass != pass2) return "Las contraseñas no coinciden.";
        if (Cuentas.Any(c => c.Usuario.Telefono == telefono.Trim())) return "Ese número de teléfono ya está registrado."; // RN-11
        return null;
    }

    private static UsuarioSesion Agregar(UsuarioSesion u, string pass)
    {
        u.Id = _siguienteId++;
        Cuentas.Add(new Cuenta { Usuario = u, Hash = Hashear(pass) });
        return u;
    }

    private static string Hashear(string pass)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] h = Rfc2898DeriveBytes.Pbkdf2(pass, sal, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(sal) + ":" + Convert.ToBase64String(h);
    }

    private static bool Verificar(string pass, string guardado)
    {
        var partes = guardado.Split(':');
        byte[] sal = Convert.FromBase64String(partes[0]);
        byte[] esperado = Convert.FromBase64String(partes[1]);
        byte[] h = Rfc2898DeriveBytes.Pbkdf2(pass, sal, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(h, esperado);
    }
}
