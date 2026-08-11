namespace MuestraApp;

partial class Form2
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
        this.lblDetalle = new System.Windows.Forms.Label();
        this.txtDetalle = new System.Windows.Forms.TextBox();
        this.lblInfo = new System.Windows.Forms.Label();
        this.btnCerrar = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // lblDetalle
        // 
        this.lblDetalle.AutoSize = true;
        this.lblDetalle.Location = new System.Drawing.Point(20, 20);
        this.lblDetalle.Name = "lblDetalle";
        this.lblDetalle.Size = new System.Drawing.Size(42, 15);
        this.lblDetalle.TabIndex = 0;
        this.lblDetalle.Text = "Detail:";
        // 
        // txtDetalle
        // 
        this.txtDetalle.AccessibleName = "Detail";
        this.txtDetalle.Location = new System.Drawing.Point(100, 17);
        this.txtDetalle.Name = "txtDetalle";
        this.txtDetalle.Size = new System.Drawing.Size(240, 23);
        this.txtDetalle.TabIndex = 1;
        // 
        // lblInfo
        // 
        this.lblInfo.AutoSize = true;
        this.lblInfo.Location = new System.Drawing.Point(20, 60);
        this.lblInfo.Name = "lblInfo";
        this.lblInfo.Size = new System.Drawing.Size(210, 15);
        this.lblInfo.TabIndex = 2;
        this.lblInfo.Text = "Detail placeholder for recipe testing.";
        // 
        // btnCerrar
        // 
        this.btnCerrar.AccessibleName = "Close";
        this.btnCerrar.Location = new System.Drawing.Point(100, 100);
        this.btnCerrar.Name = "btnCerrar";
        this.btnCerrar.Size = new System.Drawing.Size(100, 30);
        this.btnCerrar.TabIndex = 3;
        this.btnCerrar.Text = "Close";
        this.btnCerrar.UseVisualStyleBackColor = true;
        this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);
        // 
        // Form2
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(360, 150);
        this.Controls.Add(this.btnCerrar);
        this.Controls.Add(this.lblInfo);
        this.Controls.Add(this.txtDetalle);
        this.Controls.Add(this.lblDetalle);
        this.Name = "Form2";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "MuestraApp - Details";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblDetalle;
    private System.Windows.Forms.TextBox txtDetalle;
    private System.Windows.Forms.Label lblInfo;
    private System.Windows.Forms.Button btnCerrar;
}
