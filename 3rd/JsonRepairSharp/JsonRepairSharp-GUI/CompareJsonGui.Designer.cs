namespace JsonRepairSharp_GUI
{
    partial class CompareJsonGui
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CompareJsonGui));
            label6 = new Label();
            label7 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            tbSecondFile = new TextBox();
            btSecond = new Button();
            label2 = new Label();
            tbFirstFile = new TextBox();
            btFirst = new Button();
            ofdFile = new OpenFileDialog();
            fctb1 = new FastColoredTextBoxNS.FastColoredTextBox();
            fctb2 = new FastColoredTextBoxNS.FastColoredTextBox();
            splitContainer1 = new SplitContainer();
            buttonSave = new Button();
            buttonOpen = new Button();
            checkBoxIsLLM = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)fctb1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fctb2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label6.AutoSize = true;
            label6.Location = new Point(159, 479);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(74, 15);
            label6.TabIndex = 24;
            label6.Text = "Deleted lines";
            // 
            // label7
            // 
            label7.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label7.BackColor = Color.Pink;
            label7.Location = new Point(138, 479);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(14, 15);
            label7.TabIndex = 23;
            label7.Text = " ";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label5.AutoSize = true;
            label5.Location = new Point(35, 479);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(76, 15);
            label5.TabIndex = 22;
            label5.Text = "Inserted lines";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label4.BackColor = Color.PaleGreen;
            label4.Location = new Point(14, 479);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(14, 15);
            label4.TabIndex = 21;
            label4.Text = " ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(15, 46);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(84, 15);
            label3.TabIndex = 20;
            label3.Text = "Repaired JSON";
            // 
            // tbSecondFile
            // 
            tbSecondFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbSecondFile.Enabled = false;
            tbSecondFile.Location = new Point(107, 43);
            tbSecondFile.Margin = new Padding(4, 3, 4, 3);
            tbSecondFile.Name = "tbSecondFile";
            tbSecondFile.Size = new Size(633, 23);
            tbSecondFile.TabIndex = 19;
            // 
            // btSecond
            // 
            btSecond.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btSecond.Enabled = false;
            btSecond.Location = new Point(748, 40);
            btSecond.Margin = new Padding(4, 3, 4, 3);
            btSecond.Name = "btSecond";
            btSecond.Size = new Size(35, 27);
            btSecond.TabIndex = 18;
            btSecond.Text = "...";
            btSecond.UseVisualStyleBackColor = true;
            btSecond.Click += btSecond_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(36, 15);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(66, 15);
            label2.TabIndex = 17;
            label2.Text = "Input JSON";
            // 
            // tbFirstFile
            // 
            tbFirstFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbFirstFile.Location = new Point(107, 13);
            tbFirstFile.Margin = new Padding(4, 3, 4, 3);
            tbFirstFile.Name = "tbFirstFile";
            tbFirstFile.Size = new Size(633, 23);
            tbFirstFile.TabIndex = 16;
            tbFirstFile.TextChanged += tbFirstFile_TextChanged;
            // 
            // btFirst
            // 
            btFirst.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btFirst.Location = new Point(748, 10);
            btFirst.Margin = new Padding(4, 3, 4, 3);
            btFirst.Name = "btFirst";
            btFirst.Size = new Size(35, 27);
            btFirst.TabIndex = 15;
            btFirst.Text = "...";
            btFirst.UseVisualStyleBackColor = true;
            btFirst.Click += btFirst_Click;
            // 
            // fctb1
            // 
            fctb1.AutoCompleteBracketsList = (new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' });
            fctb1.AutoIndentCharsPatterns = "^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;=]+);\r\n^\\s*(case|default)\\s*[^:]*(?<range>:)\\s*(?<range>[^;]+);";
            fctb1.AutoScrollMinSize = new Size(27, 14);
            fctb1.BackBrush = null;
            fctb1.CharHeight = 14;
            fctb1.CharWidth = 8;
            fctb1.Cursor = Cursors.IBeam;
            fctb1.DefaultMarkerSize = 8;
            fctb1.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fctb1.Dock = DockStyle.Fill;
            fctb1.FindForm = null;
            fctb1.GoToForm = null;
            fctb1.Hotkeys = resources.GetString("fctb1.Hotkeys");
            fctb1.IsReplaceMode = false;
            fctb1.Location = new Point(0, 0);
            fctb1.Margin = new Padding(4, 3, 4, 3);
            fctb1.Name = "fctb1";
            fctb1.Paddings = new Padding(0);
            fctb1.ReadOnly = true;
            fctb1.ReplaceForm = null;
            fctb1.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            fctb1.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fctb1.ServiceColors");
            fctb1.Size = new Size(386, 359);
            fctb1.TabIndex = 26;
            fctb1.Zoom = 100;
            fctb1.SelectionChanged += tb_VisibleRangeChanged;
            fctb1.VisibleRangeChanged += tb_VisibleRangeChanged;
            // 
            // fctb2
            // 
            fctb2.AutoCompleteBracketsList = (new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' });
            fctb2.AutoIndentCharsPatterns = "^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;=]+);\r\n^\\s*(case|default)\\s*[^:]*(?<range>:)\\s*(?<range>[^;]+);";
            fctb2.AutoScrollMinSize = new Size(27, 14);
            fctb2.BackBrush = null;
            fctb2.CharHeight = 14;
            fctb2.CharWidth = 8;
            fctb2.Cursor = Cursors.IBeam;
            fctb2.DefaultMarkerSize = 8;
            fctb2.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fctb2.Dock = DockStyle.Fill;
            fctb2.FindForm = null;
            fctb2.GoToForm = null;
            fctb2.Hotkeys = resources.GetString("fctb2.Hotkeys");
            fctb2.IsReplaceMode = false;
            fctb2.Location = new Point(0, 0);
            fctb2.Margin = new Padding(4, 3, 4, 3);
            fctb2.Name = "fctb2";
            fctb2.Paddings = new Padding(0);
            fctb2.ReadOnly = true;
            fctb2.ReplaceForm = null;
            fctb2.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            fctb2.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fctb2.ServiceColors");
            fctb2.Size = new Size(423, 359);
            fctb2.TabIndex = 27;
            fctb2.Zoom = 100;
            fctb2.SelectionChanged += tb_VisibleRangeChanged;
            fctb2.VisibleRangeChanged += tb_VisibleRangeChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(14, 106);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(fctb1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(fctb2);
            splitContainer1.Size = new Size(814, 359);
            splitContainer1.SplitterDistance = 386;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 28;
            // 
            // buttonSave
            // 
            buttonSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSave.Enabled = false;
            buttonSave.Image = Properties.Resources.Save;
            buttonSave.Location = new Point(791, 40);
            buttonSave.Margin = new Padding(4, 3, 4, 3);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(35, 27);
            buttonSave.TabIndex = 29;
            buttonSave.Text = "...";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += buttonSave_Click;
            // 
            // buttonOpen
            // 
            buttonOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpen.Image = Properties.Resources.Open;
            buttonOpen.Location = new Point(791, 10);
            buttonOpen.Margin = new Padding(4, 3, 4, 3);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(35, 27);
            buttonOpen.TabIndex = 30;
            buttonOpen.Text = "...";
            buttonOpen.UseVisualStyleBackColor = true;
            buttonOpen.Click += buttonOpen_Click;
            // 
            // checkBoxIsLLM
            // 
            checkBoxIsLLM.AutoSize = true;
            checkBoxIsLLM.CheckAlign = ContentAlignment.MiddleRight;
            checkBoxIsLLM.Location = new Point(28, 74);
            checkBoxIsLLM.Name = "checkBoxIsLLM";
            checkBoxIsLLM.Size = new Size(92, 19);
            checkBoxIsLLM.TabIndex = 31;
            checkBoxIsLLM.Text = "LLM context";
            checkBoxIsLLM.UseVisualStyleBackColor = true;
            checkBoxIsLLM.CheckedChanged += checkBoxIsLLM_CheckedChanged;
            // 
            // CompareJsonGui
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(842, 504);
            Controls.Add(checkBoxIsLLM);
            Controls.Add(buttonOpen);
            Controls.Add(buttonSave);
            Controls.Add(splitContainer1);
            Controls.Add(label6);
            Controls.Add(label7);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(tbSecondFile);
            Controls.Add(btSecond);
            Controls.Add(label2);
            Controls.Add(tbFirstFile);
            Controls.Add(btFirst);
            Margin = new Padding(4, 3, 4, 3);
            Name = "CompareJsonGui";
            Text = "JSon Repair Json Gui";
            ((System.ComponentModel.ISupportInitialize)fctb1).EndInit();
            ((System.ComponentModel.ISupportInitialize)fctb2).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbSecondFile;
        private System.Windows.Forms.Button btSecond;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbFirstFile;
        private System.Windows.Forms.Button btFirst;
        private System.Windows.Forms.OpenFileDialog ofdFile;
        private FastColoredTextBoxNS.FastColoredTextBox fctb1;
        private FastColoredTextBoxNS.FastColoredTextBox fctb2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private Button buttonSave;
        private Button buttonOpen;
        private CheckBox checkBoxIsLLM;
    }
}