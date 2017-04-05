using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace EmitDemo
{
    public interface IHello
    {
        void SayHello(string toWhom);
    }

    class Program
    {
        static void Main(string[] args)
        {
            AssemblyName asmName = new AssemblyName("HelloWorld");

            AssemblyBuilder asmBuilder = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);

            ModuleBuilder modBuilder  = asmBuilder.DefineDynamicModule(asmName.Name);

            TypeBuilder typeBuilder = modBuilder.DefineType(
                                    "Hello",
                                    TypeAttributes.Public,
                                    typeof(object),
                                    new Type[] { typeof(IHello) });

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("SayHello",
                         MethodAttributes.Private | MethodAttributes.Virtual,
                         typeof(void), new Type[] { typeof(string) } );

            typeBuilder.DefineMethodOverride(methodBuilder, typeof(IHello).GetMethod("SayHello"));

            ILGenerator ilgen = methodBuilder.GetILGenerator();

            // string.Format("Hello, {0} World.", toWhom)
            //
            ilgen.Emit(OpCodes.Ldstr, "Hello, {0} World!");
            ilgen.Emit(OpCodes.Ldarg_1);
            ilgen.Emit(OpCodes.Call, typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) } ));

            // Console.WriteLine("Hello, World!");
            //
            ilgen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) } ));
            ilgen.Emit(OpCodes.Ret);

            Type mType  = typeBuilder.CreateType();

            IHello hello = (IHello)Activator.CreateInstance(mType);

            hello.SayHello("Emit");
        }
    }
}