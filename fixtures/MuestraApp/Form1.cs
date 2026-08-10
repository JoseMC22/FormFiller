namespace MuestraApp;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        // .NET 6+ WinForms creates most controls as HWND-less. Force handle
        // creation so UI Automation clients (FlaUI) can see the full tree.
        foreach (Control c in Controls)
        {
            _ = c.Handle;
        }
    }

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            MessageBox.Show(
                "El nombre es obligatorio",
                "Validacion",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        lblEstado.Text = "Registro guardado: " + txtCodigo.Text;

        txtCodigo.Clear();
        txtNombre.Clear();
        txtDireccion.Clear();
        txtTelefono.Clear();
        txtCiudad.Clear();
    }
}
