// Compile using : csc /out:C:\Temp\cswindow.exe /target:winexe C:\Temp\cswindow.cs

using System;
using System.Windows.Forms;

	public class MainForm : Form
	{
		[STAThread]
		static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
		
		public MainForm() 
		{
			this.lblTitle = new System.Windows.Forms.Label();
			lblTitle.Text = "Hello Windows";
			this.statusStripMain = new System.Windows.Forms.StatusStrip();
			this.ClientSize = new System.Drawing.Size(400, 300);
			this.Text = "C# Windows Pgm";
			this.Name = "MainForm";
			this.Controls.Add(this.lblTitle);
			this.Controls.Add(this.statusStripMain);
		} 
		
		private System.Windows.Forms.Label lblTitle;
		private System.Windows.Forms.StatusStrip statusStripMain;
	} 