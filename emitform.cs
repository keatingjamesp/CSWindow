// emitform.cs - Simple test of reflection.emit to generate a .Net Windows.Form.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace EmitForm
{
	class FormEmitter
	{
		static void Main()
		{	
			private readonly Assembly winformasm = Assembly.Load("System.Windows.Forms, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089");
			private readonly Assembly drawingasm = Assembly.Load("System.Drawing, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a");

            private Type WinForm = winformasm.GetType("System.Windows.Forms.Form");

            private Type applicationClass = typeof(System.Windows.Forms.Application);
			private Type winformtyp = winformasm.GetType("System.Windows.Forms.Form");
			private Type buttontyp  = winformasm.GetType("System.Windows.Forms.Button");
			private Type labeltyp   = winformasm.GetType("System.Windows.Forms.Label");
			private Type textBoxtyp = winformasm.GetType("System.Windows.Forms.TextBox");
			private Type sizetype   = drawingasm.GetType("System.Drawing.Size");
		
			// Create a (weakly named) assembly name
            AssemblyName assemblyname = new AssemblyName("FormEmitter");
            			
			// Define a new dynamic assembly (to be written to disk)
			AssemblyBuilder assemblybuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Save);
			// Define a module for this assembly
			ModuleBuilder modulebuilder = assemblybuilder.DefineDynamicModule(assemblyname.Name, "Emitted.exe", true);

			// namespace Foo { public class Bar { ... } }
			TypeBuilder typebuilder = modulebuilder.DefineType("Generated", TypeAttributes.Public);

			// The leg bone's connected to the knee bone...
			MethodBuilder methodbuilder = typebuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null );
            
			// generate form in IL
			ILGenerator ilgen = methodbuilder.GetILGenerator();

            //this.SuspendLayout();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("SuspendLayout", new Type[0]));

            // Set Visual Styles
            ilgen.Emit(OpCodes.Call, (typeof(System.Windows.Forms.Application)).GetMethod("EnableVisualStyles"));
            ilgen.Emit(OpCodes.Ldc_I4_0);
            ilgen.Emit(OpCodes.Call, (typeof(System.Windows.Forms.Application)).GetMethod("SetCompatibleTextRenderingDefault", new Type[] { typeof(bool) }));
            ilgen.Emit(OpCodes.Newobj, winformtyp.GetConstructor(new Type[0]));
            ilgen.Emit(OpCodes.Call, (typeof(System.Windows.Forms.Application)).GetMethod("Run", new Type[] { winformtyp }));
            
            // Set Form Start Position
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Ldc_I4_1);
            ilgen.Emit(OpCodes.Call, (typeof(System.Windows.Forms.Form)).GetMethod("set_StartPosition", new Type[] { typeof(System.Windows.Forms.FormStartPosition) }));

            //this.ResumeLayout(false);
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Ldc_I4_0);
            ilgen.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("ResumeLayout", new Type[] { typeof(bool) }));
            //this.PerformLayout();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("PerformLayout", new Type[0]));

            ilgen.Emit(OpCodes.Ret);
            
            // Create Constructor
            Type[] constructorArgs = null;
            ConstructorBuilder constructor = typebuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
            // ConstructorInfo frmCtor = WinForm.GetConstructor(new Type[0]);

            ilgen = constructor.GetILGenerator();
            
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Call, WinForm.GetConstructor(new Type[0]));

            ilgen.Emit(OpCodes.Ret);

			// Seal the lid on this type  (bake it ?)
			typebuilder.CreateType();

			// Set the entrypoint (thereby declaring it an EXE )
			assemblybuilder.SetEntryPoint(methodbuilder,PEFileKinds.WindowApplication);

			// Save it
			assemblybuilder.Save("Emitted.exe");

			// Done!
			Console.WriteLine("File Saved As: {0}", "Emitted.exe");
		}

        private TypeBuilder CreateClass(ModuleBuilder modulebuilder, string classname, Type parent)
        {
            return modulebuilder.DefineType(classname, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit, parent);
        }

        private ConstructorBuilder CreateConstructor(TypeBuilder typebuilder)
        {
            return typebuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, new Type[0]);
        }
	}	
}