using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Forms;

/// <summary>Inicio de sesión (RF-03) con acceso al registro como pasajero o como conductor.</summary>
public class FrmLogin : Form
{
    private readonly TextBox txtUsuario = Tema.Caja("Ej. 71234567 o correo@ejemplo.com");
    private readonly TextBox txtPass = Tema.Caja("Tu contraseña", true);
    private readonly CheckBox chkMostrar = new() { Text = "Mostrar contraseña", AutoSize = true, Font = Tema.Pequena };
    private readonly Label lblError = new() { ForeColor = Tema.Peligro, AutoSize = false, Size = new Size(360, 24), Font = Tema.Pequena };
    private readonly Button btnIngresar = Tema.Boton("Ingresar", Tema.Primario, 360, 48);
    private readonly Button btnPasajero = Tema.Boton("🧍 Registrarme\ncomo pasajero", Tema.Exito, 175, 58);
    private readonly Button btnConductor = Tema.Boton("🛺 Registrarme\ncomo conductor", Tema.Acento, 175, 58, Tema.Texto);

    public FrmLogin()
    {
        Text = "Qute Aguilares – Iniciar sesión";
        ClientSize = new Size(900, 600);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = Tema.Normal;

        var derecha = ConstruirFormulario();
        var izquierda = ConstruirPanelMarca();
        Controls.Add(derecha);
        Controls.Add(izquierda);
        AcceptButton = btnIngresar;

        chkMostrar.CheckedChanged += (_, _) => txtPass.UseSystemPasswordChar = !chkMostrar.Checked;
        btnIngresar.Click += (_, _) => Ingresar();
        btnPasajero.Click += (_, _) => AbrirRegistro(new FrmRegistroPasajero());
        btnConductor.Click += (_, _) => AbrirRegistro(new FrmRegistroConductor());
    }

    private Panel ConstruirPanelMarca()
    {
        var p = new Panel { Dock = DockStyle.Left, Width = 400, BackColor = Tema.Primario };
        p.Controls.Add(new Label { Text = "🛺", Font = new Font("Segoe UI", 54f), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 60) });
        p.Controls.Add(new Label { Text = "Qute Aguilares", Font = new Font("Segoe UI", 26f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 185) });
        p.Controls.Add(new Label
        {
            Text = "Pide tu qute rápido, seguro y con tarifa transparente.",
            Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(0xD7, 0xE2, 0xFB),
            AutoSize = false, Size = new Size(320, 52), Location = new Point(44, 245)
        });

        string[] ventajas = { "✔  Conductores identificados y unidades registradas", "✔  Conoces la tarifa antes de viajar", "✔  Pago en efectivo, sin sorpresas", "✔  Califica el servicio y reporta incidentes" };
        for (int i = 0; i < ventajas.Length; i++)
            p.Controls.Add(new Label { Text = ventajas[i], Font = Tema.Normal, ForeColor = Color.White, AutoSize = false, Size = new Size(340, 24), Location = new Point(44, 330 + i * 34) });
        return p;
    }

    private Panel ConstruirFormulario()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        const int x = 70;

        p.Controls.Add(new Label { Text = "Bienvenido", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = Tema.Texto, AutoSize = true, Location = new Point(x, 40) });
        p.Controls.Add(new Label { Text = "Inicia sesión para continuar", Font = Tema.Normal, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(x + 2, 88) });

        p.Controls.Add(new Label { Text = "Teléfono o correo", Font = Tema.Negrita, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(x, 136) });
        txtUsuario.SetBounds(x, 162, 360, 30);
        p.Controls.Add(txtUsuario);

        p.Controls.Add(new Label { Text = "Contraseña", Font = Tema.Negrita, ForeColor = Tema.TextoSuave, AutoSize = true, Location = new Point(x, 206) });
        txtPass.SetBounds(x, 232, 360, 30);
        p.Controls.Add(txtPass);

        chkMostrar.Location = new Point(x, 270);
        p.Controls.Add(chkMostrar);

        lblError.Location = new Point(x, 298);
        p.Controls.Add(lblError);

        btnIngresar.Location = new Point(x, 328);
        p.Controls.Add(btnIngresar);

        p.Controls.Add(new Label { Text = "────────  ¿Aún no tienes cuenta?  ────────", Font = Tema.Pequena, ForeColor = Tema.TextoSuave, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(360, 22), Location = new Point(x, 400) });

        btnPasajero.Location = new Point(x, 432);
        btnConductor.Location = new Point(x + 185, 432);
        p.Controls.Add(btnPasajero);
        p.Controls.Add(btnConductor);

        p.Controls.Add(new Label
        {
            Text = "Demo → admin: 70000000 / admin123\npasajero: 71111111 / 123456 · conductor: 72222222 / 123456",
            Font = new Font("Segoe UI", 8.5f), ForeColor = Tema.Gris, AutoSize = false, Size = new Size(380, 40), Location = new Point(x, 525)
        });
        return p;
    }

    private void Ingresar()
    {
        lblError.Text = "";
        if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrEmpty(txtPass.Text))
        {
            lblError.Text = "Ingresa tu teléfono o correo y tu contraseña.";
            return;
        }

        var (usuario, mensaje) = AuthService.IniciarSesion(txtUsuario.Text, txtPass.Text);
        if (usuario == null)
        {
            lblError.Text = mensaje;
            txtPass.Clear();
            txtPass.Focus();
            return;
        }

        Sesion.Actual = usuario;
        txtPass.Clear();

        var principal = new FrmPrincipal();
        principal.FormClosed += (_, _) =>
        {
            if (principal.CerroSesion) { Show(); txtUsuario.Focus(); }
            else Close();
        };
        Hide();
        principal.Show();
    }

    private void AbrirRegistro(Form registro)
    {
        using (registro)
        {
            if (registro.ShowDialog(this) == DialogResult.OK)
            {
                txtUsuario.Text = registro is FrmRegistroPasajero rp ? rp.TelefonoCreado : ((FrmRegistroConductor)registro).TelefonoCreado;
                txtPass.Focus();
            }
        }
    }
}
