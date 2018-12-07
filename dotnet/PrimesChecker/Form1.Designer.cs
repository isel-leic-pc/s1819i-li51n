namespace PrimesChecker {
    partial class Form1 {
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
            this.numSizeText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.startBut = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.countText = new System.Windows.Forms.TextBox();
            this.statusText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // numSizeText
            // 
            this.numSizeText.Location = new System.Drawing.Point(181, 35);
            this.numSizeText.Name = "numSizeText";
            this.numSizeText.Size = new System.Drawing.Size(97, 22);
            this.numSizeText.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(27, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(135, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "Numbers Size";
            // 
            // startBut
            // 
            this.startBut.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.startBut.Location = new System.Drawing.Point(32, 113);
            this.startBut.Name = "startBut";
            this.startBut.Size = new System.Drawing.Size(511, 51);
            this.startBut.TabIndex = 2;
            this.startBut.Text = "Start";
            this.startBut.UseVisualStyleBackColor = true;
            this.startBut.Click += new System.EventHandler(this.startBut_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(297, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(130, 25);
            this.label2.TabIndex = 3;
            this.label2.Text = "Primes Count";
            // 
            // countText
            // 
            this.countText.Location = new System.Drawing.Point(446, 31);
            this.countText.Name = "countText";
            this.countText.Size = new System.Drawing.Size(97, 22);
            this.countText.TabIndex = 4;
            // 
            // statusText
            // 
            this.statusText.Location = new System.Drawing.Point(32, 219);
            this.statusText.Name = "statusText";
            this.statusText.ReadOnly = true;
            this.statusText.Size = new System.Drawing.Size(511, 22);
            this.statusText.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(569, 253);
            this.Controls.Add(this.statusText);
            this.Controls.Add(this.countText);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.startBut);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numSizeText);
            this.Name = "Form1";
            this.Text = "Primes Counter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox numSizeText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button startBut;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox countText;
        private System.Windows.Forms.TextBox statusText;
    }
}

