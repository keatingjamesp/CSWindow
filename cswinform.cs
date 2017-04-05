using System;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("C# Window")]
[assembly: AssemblyDescription("A C# Window")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("xTend")]
[assembly: AssemblyProduct("csw")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

namespace cswindow
{
	public class MainForm : Form
	{
		static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
		
		public MainForm() 
		{
			this.ClientSize = new System.Drawing.Size(400, 300);
			this.Text = "C# Windows Pgm";
			this.Name = "MainForm";
			
			AppDomain _appDomain = null;
			System.Reflection.Assembly[] myAssemblies = null;
			System.Reflection.Assembly myAssembly = null;

			_appDomain = AppDomain.CurrentDomain;
			myAssemblies = _appDomain.GetAssemblies();
			foreach (System.Reflection.Assembly myAssembly_loopVariable in myAssemblies)
			{
				myAssembly = myAssembly_loopVariable;
				MessageBox.Show (myAssembly.FullName);
			}
		} 
	}
}