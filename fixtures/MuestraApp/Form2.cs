namespace MuestraApp;

public partial class Form2 : Form
{
    public Form2()
    {
        InitializeComponent();

        // .NET 6+ WinForms creates most controls as HWND-less. Force handle
        // creation so UI Automation clients (FlaUI) can see the full tree.
        foreach (Control c in Controls)
        {
            _ = c.Handle;
        }
    }

    private void BtnCerrar_Click(object? sender, EventArgs e)
    {
        Close();
    }
}
