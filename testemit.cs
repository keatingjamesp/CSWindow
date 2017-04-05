using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Diagnostics;

namespace ILCompileTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const string ASSEMBLY_NAME = "IL_Test";

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Save);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(ASSEMBLY_NAME, "testemit.exe", false);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Class | TypeAttributes.Public);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "Main", MethodAttributes.HideBySig|MethodAttributes.Public | MethodAttributes.Static,typeof(void), new Type[] { typeof(string[]) });
				
            ILGenerator ilgen = methodBuilder.GetILGenerator();

            ilgen.Emit(OpCodes.Ldstr, "Hello, World!");
            ilgen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            ilgen.Emit(OpCodes.Ldc_I4_1);
            ilgen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", new Type[] { typeof(bool) }));
			ilgen.Emit(OpCodes.Ret);
			
			// bake it
            typeBuilder.CreateType();
			
			// Set an entry point
            assemblyBuilder.SetEntryPoint(methodBuilder, PEFileKinds.ConsoleApplication);
            // File.Delete("testemit.exe");
            assemblyBuilder.Save("testemit.exe");

            // Process.Start("testemit.exe");
        }
    }
}