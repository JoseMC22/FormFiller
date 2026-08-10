namespace MuestraApp;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblCodigo = new System.Windows.Forms.Label();
        this.txtCodigo = new System.Windows.Forms.TextBox();
        this.lblNombre = new System.Windows.Forms.Label();
        this.txtNombre = new System.Windows.Forms.TextBox();
        this.lblDireccion = new System.Windows.Forms.Label();
        this.txtDireccion = new System.Windows.Forms.TextBox();
        this.lblTelefono = new System.Windows.Forms.Label();
        this.txtTelefono = new System.Windows.Forms.TextBox();
        this.lblCiudad = new System.Windows.Forms.Label();
        this.txtCiudad = new System.Windows.Forms.TextBox();
        this.btnGuardar = new System.Windows.Forms.Button();
        this.lblEstado = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // lblCodigo
        // 
        this.lblCodigo.AutoSize = true;
        this.lblCodigo.Location = new System.Drawing.Point(20, 20);
        this.lblCodigo.Name = "lblCodigo";
        this.lblCodigo.Size = new System.Drawing.Size(57, 15);
        this.lblCodigo.TabIndex = 0;
        this.lblCodigo.Text = "Codigo:";
        // 
        // txtCodigo
        // 
        this.txtCodigo.AccessibleName = "Codigo";
        this.txtCodigo.Location = new System.Drawing.Point(110, 17);
        this.txtCodigo.Name = "txtCodigo";
        this.txtCodigo.Size = new System.Drawing.Size(330, 23);
        this.txtCodigo.TabIndex = 1;
        // 
        // lblNombre
        // 
        this.lblNombre.AutoSize = true;
        this.lblNombre.Location = new System.Drawing.Point(20, 55);
        this.lblNombre.Name = "lblNombre";
        this.lblNombre.Size = new System.Drawing.Size(51, 15);
        this.lblNombre.TabIndex = 2;
        this.lblNombre.Text = "Nombre:";
        // 
        // txtNombre
        // 
        this.txtNombre.AccessibleName = "Nombre";
        this.txtNombre.Location = new System.Drawing.Point(110, 52);
        this.txtNombre.Name = "txtNombre";
        this.txtNombre.Size = new System.Drawing.Size(330, 23);
        this.txtNombre.TabIndex = 3;
        // 
        // lblDireccion
        // 
        this.lblDireccion.AutoSize = true;
        this.lblDireccion.Location = new System.Drawing.Point(20, 90);
        this.lblDireccion.Name = "lblDireccion";
        this.lblDireccion.Size = new System.Drawing.Size(67, 15);
        this.lblDireccion.TabIndex = 4;
        this.lblDireccion.Text = "Direccion:";
        // 
        // txtDireccion
        // 
        this.txtDireccion.AccessibleName = "Direccion";
        this.txtDireccion.Location = new System.Drawing.Point(110, 87);
        this.txtDireccion.Name = "txtDireccion";
        this.txtDireccion.Size = new System.Drawing.Size(330, 23);
        this.txtDireccion.TabIndex = 5;
        // 
        // lblTelefono
        // 
        this.lblTelefono.AutoSize = true;
        this.lblTelefono.Location = new System.Drawing.Point(20, 125);
        this.lblTelefono.Name = "lblTelefono";
        this.lblTelefono.Size = new System.Drawing.Size(58, 15);
        this.lblTelefono.TabIndex = 6;
        this.lblTelefono.Text = "Telefono:";
        // 
        // txtTelefono
        // 
        this.txtTelefono.AccessibleName = "Telefono";
        this.txtTelefono.Location = new System.Drawing.Point(110, 122);
        this.txtTelefono.Name = "txtTelefono";
        this.txtTelefono.Size = new System.Drawing.Size(330, 23);
        this.txtTelefono.TabIndex = 7;
        // 
        // lblCiudad
        // 
        this.lblCiudad.AutoSize = true;
        this.lblCiudad.Location = new System.Drawing.Point(20, 160);
        this.lblCiudad.Name = "lblCiudad";
        this.lblCiudad.Size = new System.Drawing.Size(45, 15);
        this.lblCiudad.TabIndex = 8;
        this.lblCiudad.Text = "Ciudad:";
        // 
        // txtCiudad
        // 
        this.txtCiudad.AccessibleName = "Ciudad";
        this.txtCiudad.Location = new System.Drawing.Point(110, 157);
        this.txtCiudad.Name = "txtCiudad";
        this.txtCiudad.Size = new System.Drawing.Size(330, 23);
        this.txtCiudad.TabIndex = 9;
        // 
        // btnGuardar
        // 
        this.btnGuardar.AccessibleName = "Guardar";
        this.btnGuardar.Location = new System.Drawing.Point(110, 200);
        this.btnGuardar.Name = "btnGuardar";
        this.btnGuardar.Size = new System.Drawing.Size(100, 30);
        this.btnGuardar.TabIndex = 10;
        this.btnGuardar.Text = "Guardar";
        this.btnGuardar.UseVisualStyleBackColor = true;
        this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);
        // 
        // lblEstado
        // 
        this.lblEstado.AccessibleName = "Estado";
        this.lblEstado.AutoSize = true;
        this.lblEstado.Location = new System.Drawing.Point(20, 250);
        this.lblEstado.Name = "lblEstado";
        this.lblEstado.Size = new System.Drawing.Size(100, 15);
        this.lblEstado.TabIndex = 11;
        this.lblEstado.Text = "(sin guardar)";
        // 
        // Form1
        // 
        this.Controls.Add(this.lblEstado);
        this.Controls.Add(this.btnGuardar);
        this.Controls.Add(this.txtCiudad);
        this.Controls.Add(this.lblCiudad);
        this.Controls.Add(this.txtTelefono);
        this.Controls.Add(this.lblTelefono);
        this.Controls.Add(this.txtDireccion);
        this.Controls.Add(this.lblDireccion);
        this.Controls.Add(this.txtNombre);
        this.Controls.Add(this.lblNombre);
        this.Controls.Add(this.txtCodigo);
        this.Controls.Add(this.lblCodigo);
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(460, 290);
        this.Name = "Form1";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "MuestraApp - Carga de Datos";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblCodigo;
    private System.Windows.Forms.TextBox txtCodigo;
    private System.Windows.Forms.Label lblNombre;
    private System.Windows.Forms.TextBox txtNombre;
    private System.Windows.Forms.Label lblDireccion;
    private System.Windows.Forms.TextBox txtDireccion;
    private System.Windows.Forms.Label lblTelefono;
    private System.Windows.Forms.TextBox txtTelefono;
    private System.Windows.Forms.Label lblCiudad;
    private System.Windows.Forms.TextBox txtCiudad;
    private System.Windows.Forms.Button btnGuardar;
    private System.Windows.Forms.Label lblEstado;
}
