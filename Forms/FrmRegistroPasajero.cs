using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Forms;

/// <summary>RF-01: registro de pasajeros con nombre, teléfono, correo y contraseña.</summary>
public class FrmRegistroPasajero : Form
{
    private readonly TextBox txtNombre = Tema.Caja(), txtApellido = Tema.Caja(),
        txtTelefono = Tema.Caja("8 dígitos, ej. 71234567"), txtCorreo = Tema.Caja("Opcional"),
        txtPass = Tema.Caja("Mínimo 6 caracteres", true), txtPass2 = Tema.Caja("Repite la contraseña", true);
    private readonly Label lblError = new() { ForeColor = Tema.Peligro, AutoSize = false, Width = 370, Height = 36, Font = Tema.Pequena };

    public string TelefonoCreado { get; private set; } = "";

    public FrmRegistroPasajero()
    {
        Text = "Registro de pasajero";
        ClientSize = new Size(440, 700);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = Tema.Normal;

        var flujo = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(30, 20, 30, 10) };
        flujo.Controls.Add(Tema.Etiqueta("🧍 Crear cuenta de pasajero", Tema.Subtitulo, Tema.Primario));
        flujo.Controls.Add(Tema.Etiqueta("Completa tus datos para pedir qutes.", Tema.Pequena, Tema.TextoSuave));
        flujo.Controls.Add(Tema.Campo("Nombres *", txtNombre, 370));
        flujo.Controls.Add(Tema.Campo("Apellidos *", txtApellido, 370));
        flujo.Controls.Add(Tema.Campo("Teléfono *", txtTelefono, 370));
        flujo.Controls.Add(Tema.Campo("Correo electrónico", txtCorreo, 370));
        flujo.Controls.Add(Tema.Campo("Contraseña *", txtPass, 370));
        flujo.Controls.Add(Tema.Campo("Confirmar contraseña *", txtPass2, 370));
        flujo.Controls.Add(lblError);

        var crear = Tema.Boton("Crear cuenta", Tema.Exito, 185, 46);
        var cancelar = Tema.Boton("Cancelar", Tema.Gris, 175, 46);
        var fila = new FlowLayoutPanel { Width = 380, Height = 56 };
        fila.Controls.Add(crear);
        fila.Controls.Add(cancelar);
        flujo.Controls.Add(fila);
        Controls.Add(flujo);

        AcceptButton = crear;
        CancelButton = cancelar;
        cancelar.Click += (_, _) => DialogResult = DialogResult.Cancel;
        crear.Click += (_, _) => Crear();
    }

    private void Crear()
    {
        var r = AuthService.RegistrarPasajero(txtNombre.Text, txtApellido.Text, txtTelefono.Text, txtCorreo.Text, txtPass.Text, txtPass2.Text);
        if (!r.Ok) { lblError.Text = r.Mensaje; return; }

        TelefonoCreado = txtTelefono.Text.Trim();
        MessageBox.Show("¡Cuenta creada! Ya puedes iniciar sesión.", "Registro exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
    }
}
