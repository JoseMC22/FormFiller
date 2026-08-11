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
        this.lblEmail = new System.Windows.Forms.Label();
        this.txtEmail = new System.Windows.Forms.TextBox();
        this.lblDni = new System.Windows.Forms.Label();
        this.txtDni = new System.Windows.Forms.TextBox();
        this.lblPassword = new System.Windows.Forms.Label();
        this.txtPassword = new System.Windows.Forms.TextBox();
        this.lblPais = new System.Windows.Forms.Label();
        this.cboPais = new System.Windows.Forms.ComboBox();
        this.chkActivo = new System.Windows.Forms.CheckBox();
        this.lblTipoCliente = new System.Windows.Forms.Label();
        this.rdbPersona = new System.Windows.Forms.RadioButton();
        this.rdbEmpresa = new System.Windows.Forms.RadioButton();
        this.lblFechaAlta = new System.Windows.Forms.Label();
        this.dtpFechaAlta = new System.Windows.Forms.DateTimePicker();
        this.lblCuit = new System.Windows.Forms.Label();
        this.txtCuit = new System.Windows.Forms.TextBox();
        this.lblObservaciones = new System.Windows.Forms.Label();
        this.txtObservaciones = new System.Windows.Forms.TextBox();
        this.btnGuardar = new System.Windows.Forms.Button();
        this.btnVerDetalle = new System.Windows.Forms.Button();
        this.btnCerrarDetalle = new System.Windows.Forms.Button();
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
        this.txtCodigo.Size = new System.Drawing.Size(150, 23);
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
        this.txtNombre.Size = new System.Drawing.Size(150, 23);
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
        this.txtDireccion.Size = new System.Drawing.Size(150, 23);
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
        this.txtTelefono.Size = new System.Drawing.Size(150, 23);
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
        this.txtCiudad.Size = new System.Drawing.Size(150, 23);
        this.txtCiudad.TabIndex = 9;
        // 
        // lblEmail
        // 
        this.lblEmail.AutoSize = true;
        this.lblEmail.Location = new System.Drawing.Point(20, 195);
        this.lblEmail.Name = "lblEmail";
        this.lblEmail.Size = new System.Drawing.Size(39, 15);
        this.lblEmail.TabIndex = 10;
        this.lblEmail.Text = "Email:";
        // 
        // txtEmail
        // 
        this.txtEmail.AccessibleName = "Email";
        this.txtEmail.Location = new System.Drawing.Point(110, 192);
        this.txtEmail.Name = "txtEmail";
        this.txtEmail.Size = new System.Drawing.Size(150, 23);
        this.txtEmail.TabIndex = 11;
        // 
        // lblDni
        // 
        this.lblDni.AutoSize = true;
        this.lblDni.Location = new System.Drawing.Point(20, 230);
        this.lblDni.Name = "lblDni";
        this.lblDni.Size = new System.Drawing.Size(31, 15);
        this.lblDni.TabIndex = 12;
        this.lblDni.Text = "DNI:";
        // 
        // txtDni
        // 
        this.txtDni.AccessibleName = "DNI";
        this.txtDni.Location = new System.Drawing.Point(110, 227);
        this.txtDni.Name = "txtDni";
        this.txtDni.Size = new System.Drawing.Size(150, 23);
        this.txtDni.TabIndex = 13;
        this.txtDni.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtDni_KeyPress);
        // 
        // lblPassword
        // 
        this.lblPassword.AutoSize = true;
        this.lblPassword.Location = new System.Drawing.Point(20, 265);
        this.lblPassword.Name = "lblPassword";
        this.lblPassword.Size = new System.Drawing.Size(60, 15);
        this.lblPassword.TabIndex = 14;
        this.lblPassword.Text = "Password:";
        // 
        // txtPassword
        // 
        this.txtPassword.AccessibleName = "Password";
        this.txtPassword.Location = new System.Drawing.Point(110, 262);
        this.txtPassword.Name = "txtPassword";
        this.txtPassword.Size = new System.Drawing.Size(150, 23);
        this.txtPassword.TabIndex = 15;
        this.txtPassword.UseSystemPasswordChar = true;
        // 
        // lblPais
        // 
        this.lblPais.AutoSize = true;
        this.lblPais.Location = new System.Drawing.Point(300, 20);
        this.lblPais.Name = "lblPais";
        this.lblPais.Size = new System.Drawing.Size(53, 15);
        this.lblPais.TabIndex = 16;
        this.lblPais.Text = "Country:";
        // 
        // cboPais
        // 
        this.cboPais.AccessibleName = "Country";
        this.cboPais.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboPais.Items.AddRange(new object[] {
            "Argentina",
            "Brazil",
            "Chile",
            "Uruguay",
            "Peru"});
        this.cboPais.Location = new System.Drawing.Point(390, 17);
        this.cboPais.Name = "cboPais";
        this.cboPais.Size = new System.Drawing.Size(160, 23);
        this.cboPais.TabIndex = 17;
        // 
        // chkActivo
        // 
        this.chkActivo.AccessibleName = "Active";
        this.chkActivo.AutoSize = true;
        this.chkActivo.Location = new System.Drawing.Point(390, 54);
        this.chkActivo.Name = "chkActivo";
        this.chkActivo.Size = new System.Drawing.Size(60, 19);
        this.chkActivo.TabIndex = 18;
        this.chkActivo.Text = "Active";
        this.chkActivo.UseVisualStyleBackColor = true;
        // 
        // lblTipoCliente
        // 
        this.lblTipoCliente.AutoSize = true;
        this.lblTipoCliente.Location = new System.Drawing.Point(300, 92);
        this.lblTipoCliente.Name = "lblTipoCliente";
        this.lblTipoCliente.Size = new System.Drawing.Size(73, 15);
        this.lblTipoCliente.TabIndex = 19;
        this.lblTipoCliente.Text = "Client type:";
        // 
        // rdbPersona
        // 
        this.rdbPersona.AccessibleName = "Person";
        this.rdbPersona.AutoSize = true;
        this.rdbPersona.Checked = true;
        this.rdbPersona.Location = new System.Drawing.Point(390, 90);
        this.rdbPersona.Name = "rdbPersona";
        this.rdbPersona.Size = new System.Drawing.Size(60, 19);
        this.rdbPersona.TabIndex = 20;
        this.rdbPersona.TabStop = true;
        this.rdbPersona.Text = "Person";
        this.rdbPersona.UseVisualStyleBackColor = true;
        // 
        // rdbEmpresa
        // 
        this.rdbEmpresa.AccessibleName = "Company";
        this.rdbEmpresa.AutoSize = true;
        this.rdbEmpresa.Location = new System.Drawing.Point(460, 90);
        this.rdbEmpresa.Name = "rdbEmpresa";
        this.rdbEmpresa.Size = new System.Drawing.Size(76, 19);
        this.rdbEmpresa.TabIndex = 21;
        this.rdbEmpresa.Text = "Company";
        this.rdbEmpresa.UseVisualStyleBackColor = true;
        // 
        // lblFechaAlta
        // 
        this.lblFechaAlta.AutoSize = true;
        this.lblFechaAlta.Location = new System.Drawing.Point(300, 132);
        this.lblFechaAlta.Name = "lblFechaAlta";
        this.lblFechaAlta.Size = new System.Drawing.Size(35, 15);
        this.lblFechaAlta.TabIndex = 22;
        this.lblFechaAlta.Text = "Date:";
        // 
        // dtpFechaAlta
        // 
        this.dtpFechaAlta.AccessibleName = "Date";
        this.dtpFechaAlta.CustomFormat = "dd/MM/yyyy";
        this.dtpFechaAlta.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this.dtpFechaAlta.Location = new System.Drawing.Point(390, 129);
        this.dtpFechaAlta.Name = "dtpFechaAlta";
        this.dtpFechaAlta.Size = new System.Drawing.Size(160, 23);
        this.dtpFechaAlta.TabIndex = 23;
        // 
        // lblCuit
        // 
        this.lblCuit.AutoSize = true;
        this.lblCuit.Location = new System.Drawing.Point(300, 167);
        this.lblCuit.Name = "lblCuit";
        this.lblCuit.Size = new System.Drawing.Size(34, 15);
        this.lblCuit.TabIndex = 24;
        this.lblCuit.Text = "CUIT:";
        // 
        // txtCuit
        // 
        this.txtCuit.AccessibleName = "CUIT";
        this.txtCuit.Location = new System.Drawing.Point(390, 164);
        this.txtCuit.Name = "txtCuit";
        this.txtCuit.Size = new System.Drawing.Size(160, 23);
        this.txtCuit.TabIndex = 25;
        this.txtCuit.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtCuit_KeyPress);
        // 
        // lblObservaciones
        // 
        this.lblObservaciones.AutoSize = true;
        this.lblObservaciones.Location = new System.Drawing.Point(300, 202);
        this.lblObservaciones.Name = "lblObservaciones";
        this.lblObservaciones.Size = new System.Drawing.Size(81, 15);
        this.lblObservaciones.TabIndex = 26;
        this.lblObservaciones.Text = "Observations:";
        // 
        // txtObservaciones
        // 
        this.txtObservaciones.AccessibleName = "Observations";
        this.txtObservaciones.Location = new System.Drawing.Point(390, 199);
        this.txtObservaciones.Multiline = true;
        this.txtObservaciones.Name = "txtObservaciones";
        this.txtObservaciones.Size = new System.Drawing.Size(160, 70);
        this.txtObservaciones.TabIndex = 27;
        // 
        // btnGuardar
        // 
        this.btnGuardar.AccessibleName = "Guardar";
        this.btnGuardar.Location = new System.Drawing.Point(110, 300);
        this.btnGuardar.Name = "btnGuardar";
        this.btnGuardar.Size = new System.Drawing.Size(100, 30);
        this.btnGuardar.TabIndex = 28;
        this.btnGuardar.Text = "Guardar";
        this.btnGuardar.UseVisualStyleBackColor = true;
        this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);
        // 
        // btnVerDetalle
        // 
        this.btnVerDetalle.AccessibleName = "View Details";
        this.btnVerDetalle.Location = new System.Drawing.Point(220, 300);
        this.btnVerDetalle.Name = "btnVerDetalle";
        this.btnVerDetalle.Size = new System.Drawing.Size(120, 30);
        this.btnVerDetalle.TabIndex = 29;
        this.btnVerDetalle.Text = "View Details";
        this.btnVerDetalle.UseVisualStyleBackColor = true;
        this.btnVerDetalle.Click += new System.EventHandler(this.BtnVerDetalle_Click);
        // 
        // btnCerrarDetalle
        // 
        this.btnCerrarDetalle.AccessibleName = "Close Details";
        this.btnCerrarDetalle.Location = new System.Drawing.Point(350, 300);
        this.btnCerrarDetalle.Name = "btnCerrarDetalle";
        this.btnCerrarDetalle.Size = new System.Drawing.Size(130, 30);
        this.btnCerrarDetalle.TabIndex = 30;
        this.btnCerrarDetalle.Text = "Close Details";
        this.btnCerrarDetalle.UseVisualStyleBackColor = true;
        this.btnCerrarDetalle.Click += new System.EventHandler(this.BtnCerrarDetalle_Click);
        // 
        // lblEstado
        // 
        this.lblEstado.AccessibleName = "Estado";
        this.lblEstado.AutoSize = true;
        this.lblEstado.Location = new System.Drawing.Point(20, 350);
        this.lblEstado.Name = "lblEstado";
        this.lblEstado.Size = new System.Drawing.Size(100, 15);
        this.lblEstado.TabIndex = 31;
        this.lblEstado.Text = "(sin guardar)";
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(580, 385);
        this.Controls.Add(this.lblEstado);
        this.Controls.Add(this.btnCerrarDetalle);
        this.Controls.Add(this.btnVerDetalle);
        this.Controls.Add(this.btnGuardar);
        this.Controls.Add(this.txtObservaciones);
        this.Controls.Add(this.lblObservaciones);
        this.Controls.Add(this.txtCuit);
        this.Controls.Add(this.lblCuit);
        this.Controls.Add(this.dtpFechaAlta);
        this.Controls.Add(this.lblFechaAlta);
        this.Controls.Add(this.rdbEmpresa);
        this.Controls.Add(this.rdbPersona);
        this.Controls.Add(this.lblTipoCliente);
        this.Controls.Add(this.chkActivo);
        this.Controls.Add(this.cboPais);
        this.Controls.Add(this.lblPais);
        this.Controls.Add(this.txtPassword);
        this.Controls.Add(this.lblPassword);
        this.Controls.Add(this.txtDni);
        this.Controls.Add(this.lblDni);
        this.Controls.Add(this.txtEmail);
        this.Controls.Add(this.lblEmail);
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
    private System.Windows.Forms.Label lblEmail;
    private System.Windows.Forms.TextBox txtEmail;
    private System.Windows.Forms.Label lblDni;
    private System.Windows.Forms.TextBox txtDni;
    private System.Windows.Forms.Label lblPassword;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.Label lblPais;
    private System.Windows.Forms.ComboBox cboPais;
    private System.Windows.Forms.CheckBox chkActivo;
    private System.Windows.Forms.Label lblTipoCliente;
    private System.Windows.Forms.RadioButton rdbPersona;
    private System.Windows.Forms.RadioButton rdbEmpresa;
    private System.Windows.Forms.Label lblFechaAlta;
    private System.Windows.Forms.DateTimePicker dtpFechaAlta;
    private System.Windows.Forms.Label lblCuit;
    private System.Windows.Forms.TextBox txtCuit;
    private System.Windows.Forms.Label lblObservaciones;
    private System.Windows.Forms.TextBox txtObservaciones;
    private System.Windows.Forms.Button btnGuardar;
    private System.Windows.Forms.Button btnVerDetalle;
    private System.Windows.Forms.Button btnCerrarDetalle;
    private System.Windows.Forms.Label lblEstado;
}
