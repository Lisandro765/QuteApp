using System.Drawing;
using System.Windows.Forms;
using QuteApp.Servicios;
using QuteApp.UI;

namespace QuteApp.Forms;

/// <summary>RF-02: registro de conductores con datos personales, DUI, licencia y datos del vehículo. Queda pendiente de aprobación (RN-01).</summary>
public class FrmRegistroConductor : Form
{
    private readonly TextBox txtNombre = Tema.Caja(), txtApellido = Tema.Caja(),
        txtTelefono = Tema.Caja("8 dígitos, ej. 72345678"), txtCorreo = Tema.Caja("Opcional"),
        txtPass = Tema.Caja("Mínimo 6 caracteres", true), txtPass2 = Tema.Caja("Repite la contraseña", true),
        txtDui = Tema.Caja("00000000-0"), txtLicencia = Tema.Caja("Número de licencia"),
        txtPlaca = Tema.Caja("Ej. P512-345"), txtUnidad = Tema.Caja("Opcional, ej. 07"), txtColor = Tema.Caja("Ej. Rojo");
    private readonly NumericUpDown numCapacidad = new() { Minimum = 1, Maximum = 4, Value = 3 };
    private readonly CheckBox chkAcepto = new()
    {
        Text = "Declaro que mis datos son verdaderos y que cuento con los permisos de circulación vigentes.",
        AutoSize = false, Size = new Size(690, 44), Font = Tema.Pequena
    };
    private readonly Label lblError = new() { ForeColor = Tema.Peligro, AutoSize = false, Width = 690, Height = 24, Font = Tema.Pequena };

    public string TelefonoCreado { get; private set; } = "";

    public FrmRegistroConductor()
    {
        Text = "Registro de conductor";
        ClientSize = new Size(780, 740);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = Tema.Normal;

        var flujo = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoScroll = true, Padding = new Padding(30, 16, 30, 10) };

        Encabezado(flujo, "🛺 Registro de conductor", Tema.Subtitulo, Tema.Primario);
        Encabezado(flujo, "El administrador revisará tus datos antes de que puedas recibir viajes.", Tema.Pequena, Tema.TextoSuave);

        Encabezado(flujo, "Datos personales", Tema.Negrita, Tema.Primario);
        flujo.Controls.Add(Tema.Campo("Nombres *", txtNombre, 330));
        flujo.Controls.Add(Tema.Campo("Apellidos *", txtApellido, 330));
        flujo.Controls.Add(Tema.Campo("Teléfono *", txtTelefono, 330));
        flujo.Controls.Add(Tema.Campo("Correo electrónico", txtCorreo, 330));
        flujo.Controls.Add(Tema.Campo("Contraseña *", txtPass, 330));
        flujo.Controls.Add(Tema.Campo("Confirmar contraseña *", txtPass2, 330));

        Encabezado(flujo, "Documentos", Tema.Negrita, Tema.Primario);
        flujo.Controls.Add(Tema.Campo("DUI *", txtDui, 330));
        flujo.Controls.Add(Tema.Campo("Número de licencia *", txtLicencia, 330));

        Encabezado(flujo, "Datos del vehículo (qute)", Tema.Negrita, Tema.Primario);
        flujo.Controls.Add(Tema.Campo("Placa *", txtPlaca, 330));
        flujo.Controls.Add(Tema.Campo("Número de unidad", txtUnidad, 330));
        flujo.Controls.Add(Tema.Campo("Color *", txtColor, 330));
        flujo.Controls.Add(Tema.Campo("Capacidad de pasajeros", numCapacidad, 330));

        flujo.Controls.Add(chkAcepto);
        flujo.SetFlowBreak(chkAcepto, true);
        flujo.Controls.Add(lblError);
        flujo.SetFlowBreak(lblError, true);

        var enviar = Tema.Boton("Enviar solicitud", Tema.Acento, 200, 46, Tema.Texto);
        var cancelar = Tema.Boton("Cancelar", Tema.Gris, 160, 46);
        flujo.Controls.Add(enviar);
        flujo.Controls.Add(cancelar);
        Controls.Add(flujo);

        CancelButton = cancelar;
        cancelar.Click += (_, _) => DialogResult = DialogResult.Cancel;
        enviar.Click += (_, _) => Enviar();
    }

    private static void Encabezado(FlowLayoutPanel flujo, string texto, Font fuente, Color color)
    {
        var l = new Label { Text = texto, Font = fuente, ForeColor = color, AutoSize = false, Size = new Size(690, 28), Margin = new Padding(0, 8, 0, 4) };
        flujo.Controls.Add(l);
        flujo.SetFlowBreak(l, true);
    }

    private void Enviar()
    {
        if (!chkAcepto.Checked) { lblError.Text = "Debes aceptar la declaración para continuar."; return; }

        var r = AuthService.RegistrarConductor(txtNombre.Text, txtApellido.Text, txtTelefono.Text, txtCorreo.Text, txtPass.Text, txtPass2.Text,
            txtDui.Text, txtLicencia.Text, txtPlaca.Text, txtUnidad.Text, txtColor.Text, (int)numCapacidad.Value);
        if (!r.Ok) { lblError.Text = r.Mensaje; return; }

        TelefonoCreado = txtTelefono.Text.Trim();
        MessageBox.Show(r.Mensaje, "Solicitud enviada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
    }
}
