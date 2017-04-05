// emitform.cs
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EmitForm
{
	class FormEmitter
	{
		static void Main()
		{			
			// Create a (weak) assembly name
			AssemblyName asmnam = new AssemblyName();
			asmnam.Name = "FormEmitter";
			
			// Define a new dynamic assembly (to be written to disk)
			AppDomain appdmn = AppDomain.CurrentDomain;
			AssemblyBuilder asmbld =  appdmn.DefineDynamicAssembly(asmnam, AssemblyBuilderAccess.Save);

			// Define a module for this assembly
			ModuleBuilder modbld = asmbld.DefineDynamicModule(asmnam.Name, "Emitted.exe");

			// namespace Foo { public class Bar { ... } }
			TypeBuilder typbld = modbld.DefineType("Foo.Bar", TypeAttributes.Public| TypeAttributes.Class);

			// The leg bone's connected to the knee bone...
			MethodBuilder fb = typbld.DefineMethod("Main", MethodAttributes.Public| MethodAttributes.Static, typeof(int),  new Type[] {typeof(string[])});

			// Write a method, in IL
			ILGenerator ilg = fb.GetILGenerator();
			
			ilg.Emit(OpCodes.Ldstr, "Emitted .Net 4.0 Executable");
			ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] {typeof(string)} ));

			ilg.Emit(OpCodes.Ldc_I4_0);
			ilg.Emit(OpCodes.Ret);
			
			// Seal the lid on this type  (bake it ?)
			Type typ = typbld.CreateType();

			// Set the entrypoint (thereby declaring it an EXE)
			asmbld.SetEntryPoint(fb,PEFileKinds.ConsoleApplication);

			// Save it
			asmbld.Save("Emitter.exe");

			// Done!
			Console.WriteLine("File Saved As : {0}", "Emitter.exe");
		}
	}	
}