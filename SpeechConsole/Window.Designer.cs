namespace SpeechConsole
{
    partial class Window
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.input = new System.Windows.Forms.TextBox();
            this.textChangeMonitor = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // input
            // 
            this.input.AcceptsReturn = true;
            this.input.AcceptsTab = true;
            this.input.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.input.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.input.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.input.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.input.Location = new System.Drawing.Point(0, 0);
            this.input.Multiline = true;
            this.input.Name = "input";
            this.input.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.input.Size = new System.Drawing.Size(1040, 138);
            this.input.TabIndex = 0;
            this.input.TextChanged += new System.EventHandler(this.input_TextChanged);
            // 
            // textChangeMonitor
            // 
            this.textChangeMonitor.Enabled = true;
            this.textChangeMonitor.Interval = 50;
            this.textChangeMonitor.Tick += new System.EventHandler(this.textChangeMonitor_Tick);
            // 
            // Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 138);
            this.Controls.Add(this.input);
            this.KeyPreview = true;
            this.Name = "Window";
            this.Text = "QEdit SC";
            this.Load += new System.EventHandler(this.Window_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Window_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox input;
        private System.Windows.Forms.Timer textChangeMonitor;
    }
}

