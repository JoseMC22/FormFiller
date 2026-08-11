namespace MuestraApp;

public partial class Form1 : Form
{
    private Form2? _detailForm;

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
        txtEmail.Clear();
        txtDni.Clear();
        txtPassword.Clear();
        txtCuit.Clear();
        txtObservaciones.Clear();
        cboPais.SelectedIndex = -1;
        chkActivo.Checked = false;
        rdbPersona.Checked = true;
        dtpFechaAlta.Value = DateTime.Today;
    }

    private void BtnVerDetalle_Click(object? sender, EventArgs e)
    {
        if (_detailForm is null || _detailForm.IsDisposed)
        {
            _detailForm = new Form2();
            _detailForm.FormClosed += (_, _) => _detailForm = null;
            // Shown WITHOUT an owner so the process's main window title switches
            // to the detail window, which is what the WaitForWindowByTitle and
            // ClickIfWindowVisible recipes poll.
            _detailForm.Show();
        }
    }

    private void BtnCerrarDetalle_Click(object? sender, EventArgs e)
    {
        if (_detailForm is not null)
        {
            _detailForm.Close();
            _detailForm = null;
            lblEstado.Text = "Detail window closed";
        }
    }

    private void TxtDni_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
        }
    }

    private void TxtCuit_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
        }
    }
}
