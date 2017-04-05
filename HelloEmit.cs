using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;

class Program
{
    static void Main(string[] args)
    {
        var name = "HelloEmit.exe";
        var assemblyname = new AssemblyName(name);
        var assemblybuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.RunAndSave);
        var modulebuilder = assemblybuilder.DefineDynamicModule(name);
        var programmclass = modulebuilder.DefineType("Program",TypeAttributes.Public);

        // Add a member (a method) to the type
        var mainmethod = programmclass.DefineMethod("Main",MethodAttributes.Public | MethodAttributes.Static,null, new Type[]{typeof(string[])});

        // Generate MSIL.
        var ilgenerator = mainmethod.GetILGenerator();
    	ilgenerator.Emit(OpCodes.Ldstr,"Hello Emit World!");
        ilgenerator.Emit(OpCodes.Call, (typeof(Console)).GetMethod("WriteLine", new Type[]{typeof(string)}));
        ilgenerator.Emit(OpCodes.Call, (typeof(Console)).GetMethod("ReadKey",new Type[0]));
        ilgenerator.Emit(OpCodes.Pop);
        ilgenerator.Emit(OpCodes.Ret);
 
        programmclass.CreateType();
 
        assemblybuilder.SetEntryPoint(((Type) programmclass).GetMethod("Main"));
 
        assemblybuilder.Save(name);
 
        Console.WriteLine("EXE Generated. Press a key to quit.");
        Console.ReadKey(); 
    }
}