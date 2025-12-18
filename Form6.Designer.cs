namespace diplom
{
    partial class Form6
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            clbConfigs = new CheckedListBox();
            btnShow = new Button();
            btnCalculate = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // clbConfigs
            // 
            clbConfigs.BackColor = Color.White;
            clbConfigs.Font = new Font("Segoe UI", 10F);
            clbConfigs.FormattingEnabled = true;
            clbConfigs.Location = new Point(284, 15);
            clbConfigs.Name = "clbConfigs";
            clbConfigs.Size = new Size(300, 84);
            clbConfigs.TabIndex = 0;
            // 
            // btnShow
            // 
            btnShow.BackColor = Color.FromArgb(0, 122, 204);
            btnShow.FlatStyle = FlatStyle.Flat;
            btnShow.Font = new Font("Segoe UI", 10F);
            btnShow.ForeColor = Color.White;
            btnShow.Location = new Point(601, 38);
            btnShow.Name = "btnShow";
            btnShow.Size = new Size(190, 30);
            btnShow.TabIndex = 1;
            btnShow.Text = "Показать выбранные";
            btnShow.UseVisualStyleBackColor = false;
            btnShow.Click += BtnShow_Click;
            // 
            // btnCalculate
            // 
            btnCalculate.BackColor = Color.FromArgb(0, 122, 204);
            btnCalculate.Dock = DockStyle.Bottom;
            btnCalculate.FlatStyle = FlatStyle.Flat;
            btnCalculate.Font = new Font("Segoe UI", 10F);
            btnCalculate.ForeColor = Color.White;
            btnCalculate.Location = new Point(0, 488);
            btnCalculate.Name = "btnCalculate";
            btnCalculate.Size = new Size(1048, 40);
            btnCalculate.TabIndex = 2;
            btnCalculate.Text = "Рассчитать стоимости";
            btnCalculate.UseVisualStyleBackColor = false;
            btnCalculate.Click += BtnCalculate_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F);
            label1.Location = new Point(12, 45);
            label1.Name = "label1";
            label1.Size = new Size(266, 19);
            label1.TabIndex = 3;
            label1.Text = "Выберите конфигурации для сравнения:";
            // 
            // Form6
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            BackColor = Color.FromArgb(240, 248, 255);
            ClientSize = new Size(1048, 528);
            Controls.Add(label1);
            Controls.Add(btnCalculate);
            Controls.Add(btnShow);
            Controls.Add(clbConfigs);
            Name = "Form6";
            Text = "Сравнение конфигураций";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckedListBox clbConfigs;
        private Button btnShow;
        private Button btnCalculate;
        private Label label1;
    }
}