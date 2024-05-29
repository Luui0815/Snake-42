
namespace Schnittlängenberechnung
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.tb_AnzLeisten = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tb_LängeStangen = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lb_Stangenlängen = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lb_erzeugteStangen = new System.Windows.Forms.ListBox();
            this.bt_berechne = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.tb_Mindestlänge = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tb_Maximallänge = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.lb_fazit = new System.Windows.Forms.Label();
            this.bt_Zufallsleisten = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.tb_Genauigkeit = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.tb_restmüllgrenze = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.lb_Reststangen = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(47, 61);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Anzahl der Leisten: ";
            // 
            // tb_AnzLeisten
            // 
            this.tb_AnzLeisten.Location = new System.Drawing.Point(165, 58);
            this.tb_AnzLeisten.Name = "tb_AnzLeisten";
            this.tb_AnzLeisten.Size = new System.Drawing.Size(100, 23);
            this.tb_AnzLeisten.TabIndex = 1;
            this.tb_AnzLeisten.Text = "5";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(48, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Länge der Stangen:";
            // 
            // tb_LängeStangen
            // 
            this.tb_LängeStangen.Location = new System.Drawing.Point(165, 22);
            this.tb_LängeStangen.Name = "tb_LängeStangen";
            this.tb_LängeStangen.Size = new System.Drawing.Size(100, 23);
            this.tb_LängeStangen.TabIndex = 3;
            this.tb_LängeStangen.Text = "100";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(271, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "mm";
            // 
            // lb_Stangenlängen
            // 
            this.lb_Stangenlängen.FormattingEnabled = true;
            this.lb_Stangenlängen.ItemHeight = 15;
            this.lb_Stangenlängen.Location = new System.Drawing.Point(466, 38);
            this.lb_Stangenlängen.Name = "lb_Stangenlängen";
            this.lb_Stangenlängen.ScrollAlwaysVisible = true;
            this.lb_Stangenlängen.Size = new System.Drawing.Size(179, 424);
            this.lb_Stangenlängen.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(466, 17);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(180, 15);
            this.label5.TabIndex = 8;
            this.label5.Text = "zufällig generierte Leistenlängen:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(27, 285);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(137, 15);
            this.label6.TabIndex = 9;
            this.label6.Text = "erzeugte Stangenlängen:";
            // 
            // lb_erzeugteStangen
            // 
            this.lb_erzeugteStangen.FormattingEnabled = true;
            this.lb_erzeugteStangen.ItemHeight = 15;
            this.lb_erzeugteStangen.Location = new System.Drawing.Point(27, 307);
            this.lb_erzeugteStangen.Name = "lb_erzeugteStangen";
            this.lb_erzeugteStangen.ScrollAlwaysVisible = true;
            this.lb_erzeugteStangen.Size = new System.Drawing.Size(410, 154);
            this.lb_erzeugteStangen.TabIndex = 10;
            // 
            // bt_berechne
            // 
            this.bt_berechne.Location = new System.Drawing.Point(201, 237);
            this.bt_berechne.Name = "bt_berechne";
            this.bt_berechne.Size = new System.Drawing.Size(100, 45);
            this.bt_berechne.TabIndex = 11;
            this.bt_berechne.Text = "Berechne";
            this.bt_berechne.UseVisualStyleBackColor = true;
            this.bt_berechne.Click += new System.EventHandler(this.bt_berechne_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 95);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(142, 15);
            this.label7.TabIndex = 12;
            this.label7.Text = "Mindestlänge der Leisten:";
            // 
            // tb_Mindestlänge
            // 
            this.tb_Mindestlänge.Location = new System.Drawing.Point(165, 92);
            this.tb_Mindestlänge.Name = "tb_Mindestlänge";
            this.tb_Mindestlänge.Size = new System.Drawing.Size(100, 23);
            this.tb_Mindestlänge.TabIndex = 13;
            this.tb_Mindestlänge.Text = "10";
            this.tb_Mindestlänge.TextChanged += new System.EventHandler(this.tb_Mindestlänge_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(271, 95);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(29, 15);
            this.label8.TabIndex = 14;
            this.label8.Text = "mm";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(271, 131);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(29, 15);
            this.label9.TabIndex = 17;
            this.label9.Text = "mm";
            // 
            // tb_Maximallänge
            // 
            this.tb_Maximallänge.Location = new System.Drawing.Point(165, 128);
            this.tb_Maximallänge.Name = "tb_Maximallänge";
            this.tb_Maximallänge.Size = new System.Drawing.Size(100, 23);
            this.tb_Maximallänge.TabIndex = 16;
            this.tb_Maximallänge.Text = "50";
            this.tb_Maximallänge.TextChanged += new System.EventHandler(this.tb_Maximallänge_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 131);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(145, 15);
            this.label10.TabIndex = 15;
            this.label10.Text = "Maximallänge der Leisten:";
            // 
            // lb_fazit
            // 
            this.lb_fazit.AutoSize = true;
            this.lb_fazit.Location = new System.Drawing.Point(27, 476);
            this.lb_fazit.Name = "lb_fazit";
            this.lb_fazit.Size = new System.Drawing.Size(0, 15);
            this.lb_fazit.TabIndex = 18;
            // 
            // bt_Zufallsleisten
            // 
            this.bt_Zufallsleisten.Location = new System.Drawing.Point(95, 237);
            this.bt_Zufallsleisten.Name = "bt_Zufallsleisten";
            this.bt_Zufallsleisten.Size = new System.Drawing.Size(100, 45);
            this.bt_Zufallsleisten.TabIndex = 19;
            this.bt_Zufallsleisten.Text = "Generiere neue Zufallsleisten";
            this.bt_Zufallsleisten.UseVisualStyleBackColor = true;
            this.bt_Zufallsleisten.Click += new System.EventHandler(this.bt_Zufallsleisten_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(83, 165);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 15);
            this.label4.TabIndex = 20;
            this.label4.Text = "Genauigkeit:";
            // 
            // tb_Genauigkeit
            // 
            this.tb_Genauigkeit.Location = new System.Drawing.Point(165, 162);
            this.tb_Genauigkeit.Name = "tb_Genauigkeit";
            this.tb_Genauigkeit.Size = new System.Drawing.Size(100, 23);
            this.tb_Genauigkeit.TabIndex = 21;
            this.tb_Genauigkeit.Text = "100";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(271, 166);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(17, 15);
            this.label11.TabIndex = 22;
            this.label11.Text = "%";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(271, 200);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(29, 15);
            this.label12.TabIndex = 25;
            this.label12.Text = "mm";
            // 
            // tb_restmüllgrenze
            // 
            this.tb_restmüllgrenze.Location = new System.Drawing.Point(165, 197);
            this.tb_restmüllgrenze.Name = "tb_restmüllgrenze";
            this.tb_restmüllgrenze.Size = new System.Drawing.Size(100, 23);
            this.tb_restmüllgrenze.TabIndex = 24;
            this.tb_restmüllgrenze.Text = "100";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(65, 200);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(91, 15);
            this.label13.TabIndex = 23;
            this.label13.Text = "Restmüllgrenze:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(684, 17);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(74, 15);
            this.label14.TabIndex = 27;
            this.label14.Text = "Reststangen:";
            // 
            // lb_Reststangen
            // 
            this.lb_Reststangen.FormattingEnabled = true;
            this.lb_Reststangen.ItemHeight = 15;
            this.lb_Reststangen.Location = new System.Drawing.Point(684, 38);
            this.lb_Reststangen.Name = "lb_Reststangen";
            this.lb_Reststangen.ScrollAlwaysVisible = true;
            this.lb_Reststangen.Size = new System.Drawing.Size(179, 424);
            this.lb_Reststangen.TabIndex = 26;
            this.lb_Reststangen.DoubleClick += new System.EventHandler(this.lb_Reststangen_DoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(901, 497);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.lb_Reststangen);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.tb_restmüllgrenze);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tb_Genauigkeit);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.bt_Zufallsleisten);
            this.Controls.Add(this.lb_fazit);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.tb_Maximallänge);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tb_Mindestlänge);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.bt_berechne);
            this.Controls.Add(this.lb_erzeugteStangen);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lb_Stangenlängen);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_LängeStangen);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tb_AnzLeisten);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tb_AnzLeisten;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tb_LängeStangen;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lb_Stangenlängen;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox lb_erzeugteStangen;
        private System.Windows.Forms.Button bt_berechne;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tb_Mindestlänge;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tb_Maximallänge;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lb_fazit;
        private System.Windows.Forms.Button bt_Zufallsleisten;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tb_Genauigkeit;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tb_restmüllgrenze;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ListBox lb_Reststangen;
    }
}

