using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormEmitter
{
    class Program
    { 
        static void Main(string[] args)
        {
            string asmname;

            if (args.Length < 1)
            {
                asmname = "Emitted";
            }
            else
            {
                asmname = args[0];
            }

            GenCode cls = new GenCode();
            bool retcode = cls.CreateForm(asmname);
        }

    }

    // ========================================  Class to Generate the program ==================================================

    class GenCode 
    {
        private readonly Assembly _winformAssembly = Assembly.Load("System.Windows.Forms,Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        private readonly Assembly _drawingAssembly = Assembly.Load("System.Drawing,Version=4.0.0.0,Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

        private Type _winformType;
        private Type _numericUpDownType;
        private Type _buttonType;
        private Type _labelType;
        private Type _textBoxType;
        private Type _iContainerType;
        private Type _pointType;
        private Type _sizeType;
        private Type _sizefType;

        // properties get/set method attributes
        private MethodAttributes _getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        public bool CreateForm(string asmname)
        {  
            _winformType = _winformAssembly.GetType("System.Windows.Forms.Form");
            _numericUpDownType = _winformAssembly.GetType("System.Windows.Forms.NumericUpDown");
            _buttonType = _winformAssembly.GetType("System.Windows.Forms.Button");
            _labelType = _winformAssembly.GetType("System.Windows.Forms.Label");
            _textBoxType = _winformAssembly.GetType("System.Windows.Forms.TextBox");
            _iContainerType = _winformAssembly.GetType("System.ComponentModel.IContainer");
            _pointType = _drawingAssembly.GetType("System.Drawing.Point");
            _sizeType = _drawingAssembly.GetType("System.Drawing.Size");
            _sizefType = _drawingAssembly.GetType("System.Drawing.SizeF");

            string objectname = asmname + ".exe";

            // Create a (weakly named) assembly name
            AssemblyName assemblyname = new AssemblyName(asmname);

            // Define a new dynamic assembly (to be written to disk)
            AssemblyBuilder assemblybuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Save);

            // Define a module for this assembly
            ModuleBuilder modulebuilder = assemblybuilder.DefineDynamicModule(assemblyname.Name, objectname, true);

            // Create IOperation interface
            var operationinterface = CreateIOperationInterface(modulebuilder);
            operationinterface.CreateType();

            // Create Operation class, implementing IOperation.
            var implementadoperationclass = CreateImplementedOperationClass(modulebuilder, operationinterface);
            implementadoperationclass.CreateType();

            // Create ILCreatedForm class
            var formclass = CreateILCreatedFormClass(modulebuilder, operationinterface, implementadoperationclass);

            // Create program class
            var programclass = CreateProgramClass(modulebuilder, formclass);

            // bake it
            programclass.CreateType();
            // set the entry point for the application and save it
            assemblybuilder.SetEntryPoint((programclass).GetMethod("Main"), PEFileKinds.WindowApplication);
            assemblybuilder.Save(objectname);

            return true;

        }

        // ========================================  Create the Program Class ==================================================

        private TypeBuilder CreateProgramClass(ModuleBuilder modulebuilder, TypeBuilder formclass)
        {
            var programclass = CreateClass(modulebuilder, "Program", null);

            var programclassrunmethod = CreateClassMethod(programclass, "Main", null, null, MethodAttributes.Public | MethodAttributes.Static);
            programclassrunmethod.SetCustomAttribute(new CustomAttributeBuilder(typeof(STAThreadAttribute).GetConstructor(new Type[] { }), new object[] { }));

            var ilgenerator = programclassrunmethod.GetILGenerator();

            ilgenerator.Emit(OpCodes.Call, (typeof(Application)).GetMethod("EnableVisualStyles"));

            ilgenerator.Emit(OpCodes.Ldc_I4_0);
            ilgenerator.Emit(OpCodes.Call, (typeof(Application)).GetMethod("SetCompatibleTextRenderingDefault", new Type[] { typeof(bool) }));


            ilgenerator.Emit(OpCodes.Newobj, formclass.GetConstructor(new Type[0]));

            ilgenerator.Emit(OpCodes.Call, (typeof(Application)).GetMethod("Run", new Type[] { _winformType }));
            ilgenerator.Emit(OpCodes.Ret);

            return programclass;
        }

        // ========================================  Operation Class =================================================

        private TypeBuilder CreateImplementedOperationClass(ModuleBuilder modulebuilder, TypeBuilder operationinterface)
        {
            ILGenerator ilgenerator;
            var implementedoperationclass = CreateClass(modulebuilder, "ImplementedOperation", null);
            implementedoperationclass.AddInterfaceImplementation(operationinterface);


            var _namefield = CreateField(implementedoperationclass, "_name", typeof(string), FieldAttributes.Private);
            // Name property
            var nameProperty = CreateProperty(implementedoperationclass, "Name", typeof(string), PropertyAttributes.None);
            var get_name = CreateClassMethod(implementedoperationclass, "get_Name", typeof(string), new Type[0], _getSetAttr | MethodAttributes.Virtual);
            ilgenerator = get_name.GetILGenerator();
            ilgenerator.DeclareLocal(typeof(string));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, _namefield);
            ilgenerator.Emit(OpCodes.Stloc_0);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ret);
            nameProperty.SetGetMethod(get_name);

            var set_name = CreateClassMethod(implementedoperationclass, "set_Name", null, new Type[] { typeof(string) }, _getSetAttr | MethodAttributes.Virtual);
            set_name.DefineParameter(1, ParameterAttributes.None, "value");
            ilgenerator = set_name.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Stfld, _namefield);
            ilgenerator.Emit(OpCodes.Ret);
            nameProperty.SetSetMethod(set_name);

            var operation = CreateClassMethod(implementedoperationclass, "Operation", typeof(decimal), new Type[] { typeof(decimal), typeof(decimal) }, MethodAttributes.Public | MethodAttributes.Virtual);
            ilgenerator = operation.GetILGenerator();
            var local = ilgenerator.DeclareLocal(typeof(Exception));
            ilgenerator.DeclareLocal(typeof(decimal));
            ilgenerator.BeginExceptionBlock();

            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Ldarg_2);
            ilgenerator.Emit(OpCodes.Call, (typeof(decimal)).GetMethod("op_Addition", new Type[] { typeof(decimal), typeof(decimal) }));

            ilgenerator.Emit(OpCodes.Stloc_1);
            ilgenerator.BeginCatchBlock(typeof(Exception));
            ilgenerator.Emit(OpCodes.Stloc_0);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Callvirt, (typeof(Exception)).GetMethod("get_Message", new Type[0]));
            ilgenerator.Emit(OpCodes.Call, (typeof(System.Windows.Forms.MessageBox)).GetMethod("Show", new Type[] { typeof(string) }));
            ilgenerator.Emit(OpCodes.Pop);
            ilgenerator.Emit(OpCodes.Ldc_I4_0);
            ilgenerator.Emit(OpCodes.Newobj, (typeof(decimal)).GetConstructor(new Type[] { typeof(int) }));
            ilgenerator.Emit(OpCodes.Stloc_1);
            ilgenerator.EndExceptionBlock();
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ret);

            var interfaceoperationMethod = operationinterface.GetMethod("Operation");
            implementedoperationclass.DefineMethodOverride(operation, interfaceoperationMethod);
            
            // Constructor
            var constructorMethod = CreateConstructor(implementedoperationclass);
            ilgenerator = constructorMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldstr, "Calculate");
            ilgenerator.Emit(OpCodes.Stfld, _namefield);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Ret);

            return implementedoperationclass;
        }

        // ====================================== Create the form and components =================================================

        private TypeBuilder CreateILCreatedFormClass(ModuleBuilder modulebuilder, TypeBuilder operationinterface, TypeBuilder implementedinterface)
        {
            ILGenerator ilgenerator;
            LocalBuilder localbuilder;
            // Create class that heritates from Form
            var formclass = CreateClass(modulebuilder, "ILCreatedForm", _winformType);

            // create fields
            var button1Field = CreateField(formclass, "buttonOperation", typeof(Button), FieldAttributes.Private);
            var numericupdown1Field = CreateField(formclass, "numericupdown1", typeof(NumericUpDown), FieldAttributes.Private);
            var numericupdown2Field = CreateField(formclass, "numericupdown2", typeof(NumericUpDown), FieldAttributes.Private);
            var textboxResultField = CreateField(formclass, "textboxResult", typeof(TextBox), FieldAttributes.Private);
            var componentsField = CreateField(formclass, "components", typeof(IContainer), FieldAttributes.Private);
            var ioperationField = CreateField(formclass, "_theoperation", operationinterface, FieldAttributes.Private);

            // create properties
            // Number1
            var number1Property = CreateProperty(formclass, "Number1", typeof(decimal), PropertyAttributes.None);
            var get_number1 = CreateClassMethod(formclass, "get_Number1", typeof(decimal), new Type[0], _getSetAttr);
            ilgenerator = get_number1.GetILGenerator();
            ilgenerator.DeclareLocal(typeof(decimal));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Callvirt, _numericUpDownType.GetMethod("get_Value", new Type[0]));
            ilgenerator.Emit(OpCodes.Stloc_0);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ret);
            number1Property.SetGetMethod(get_number1);

            var set_number1 = CreateClassMethod(formclass, "set_Number1", null, new Type[] { typeof(decimal) }, _getSetAttr);
            set_number1.DefineParameter(1, ParameterAttributes.None, "value");
            ilgenerator = set_number1.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Callvirt, _numericUpDownType.GetMethod("set_Value", new Type[] { typeof(decimal) }));
            ilgenerator.Emit(OpCodes.Ret);
            number1Property.SetSetMethod(set_number1);

            // Number2
            var number2Property = CreateProperty(formclass, "Number2", typeof(decimal), PropertyAttributes.None);
            var get_number2 = CreateClassMethod(formclass, "get_Number2", typeof(decimal), new Type[0], _getSetAttr);
            ilgenerator = get_number2.GetILGenerator();
            ilgenerator.DeclareLocal(typeof(decimal));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Callvirt, _numericUpDownType.GetMethod("get_Value", new Type[0]));
            ilgenerator.Emit(OpCodes.Stloc_0);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ret);
            number2Property.SetGetMethod(get_number2);

            var set_number2 = CreateClassMethod(formclass, "set_Number2", null, new Type[] { typeof(decimal) }, _getSetAttr);
            set_number2.DefineParameter(1, ParameterAttributes.None, "value");
            ilgenerator = set_number2.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Callvirt, _numericUpDownType.GetMethod("set_Value", new Type[] { typeof(decimal) }));
            ilgenerator.Emit(OpCodes.Ret);
            number2Property.SetSetMethod(set_number2);

            // Result
            var resultProperty = CreateProperty(formclass, "Result", typeof(decimal), PropertyAttributes.None);
            var get_result = CreateClassMethod(formclass, "get_Result", typeof(decimal), new Type[0], _getSetAttr);
            ilgenerator = get_result.GetILGenerator();
            localbuilder = ilgenerator.DeclareLocal(typeof(decimal));
            ilgenerator.DeclareLocal(typeof(decimal));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("get_Text", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldloca_S, localbuilder);
            ilgenerator.Emit(OpCodes.Call, (typeof(decimal)).GetMethod("TryParse", new Type[] { typeof(string), typeof(decimal).MakeByRefType() }));
            ilgenerator.Emit(OpCodes.Pop);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Stloc_1);
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ret);
            resultProperty.SetGetMethod(get_result);

            var set_result = CreateClassMethod(formclass, "set_Result", null, new Type[] { typeof(decimal) }, _getSetAttr);
            ilgenerator = set_result.GetILGenerator();
            set_result.DefineParameter(1, ParameterAttributes.None, "value");
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldarga_S, 1);
            ilgenerator.Emit(OpCodes.Call, (typeof(decimal)).GetMethod("ToString", new Type[0]));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));
            ilgenerator.Emit(OpCodes.Ret);
            //resultProperty.SetSetMethod(set_result);

            // Create buttonOperation_Click method
            var buttonOperation_Clickmethod = CreateClassMethod(formclass, "buttonOperation_Click", null, new Type[] { typeof(object), typeof(EventArgs) }, MethodAttributes.Public);
            ilgenerator = buttonOperation_Clickmethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, ioperationField);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, get_number1);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, get_number2);
            ilgenerator.Emit(OpCodes.Callvirt, operationinterface.GetMethod("Operation", new Type[] { typeof(decimal), typeof(decimal) }));
            ilgenerator.Emit(OpCodes.Call, set_result);
            ilgenerator.Emit(OpCodes.Ret);

            // Create InitializeComponent method
            var initializecomponentmethod = CreateClassMethod(formclass, "InitializeComponent", null, null, MethodAttributes.Private | MethodAttributes.HideBySig);
            ilgenerator = initializecomponentmethod.GetILGenerator();

            //System.Windows.Forms.Label label1;
            localbuilder = ilgenerator.DeclareLocal(_labelType);

            // System.Windows.Forms.Label label2;
            localbuilder = ilgenerator.DeclareLocal(_labelType);
  
            // System.Windows.Forms.Label label3;
            localbuilder = ilgenerator.DeclareLocal(_labelType);

            // this.button1 = new System.Windows.Forms.Button();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Newobj, _buttonType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stfld, button1Field);

            // this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Newobj, _numericUpDownType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stfld, numericupdown1Field);

            // this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Newobj, _numericUpDownType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stfld, numericupdown2Field);

            // this.textBox1 = new System.Windows.Forms.TextBox();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Newobj, _textBoxType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stfld, textboxResultField);

            // label1 = new System.Windows.Forms.Label();
            ilgenerator.Emit(OpCodes.Newobj, _labelType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stloc_0);

            // label2 = new System.Windows.Forms.Label();
            ilgenerator.Emit(OpCodes.Newobj, _labelType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stloc_1);

            // label3 = new System.Windows.Forms.Label();
            ilgenerator.Emit(OpCodes.Newobj, _labelType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stloc_2);

            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.ISupportInitialize).GetMethod("BeginInit", new Type[0]));

            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.ISupportInitialize).GetMethod("BeginInit", new Type[0]));

            //this.SuspendLayout();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("SuspendLayout", new Type[0]));
            // 
            // label1
            //
            //label1.AutoSize = true;
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_AutoSize", new Type[] { typeof(bool) }));
            //label1.Location = new System.Drawing.Point(19, 26);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 19);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 26);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //label1.Name = "label1";
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldstr, "label1");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //label1.Size = new System.Drawing.Size(53, 13);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 53);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 13);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //label1.TabIndex = 3;
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_3);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //label1.Text = "Number 1";
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Ldstr, "Number 1");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));
            //// 
            //// button1
            //// 
            //this.button1.Location = new System.Drawing.Point(141, 96);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4, 136);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 96);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //this.button1.Name = "button1";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldstr, "buttonOperation");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //this.button1.Size = new System.Drawing.Size(75, 23);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 85);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 23);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //this.button1.TabIndex = 0;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_0);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //this.button1.Text = "button1";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldstr, "button1");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));
            //this.button1.UseVisualStyleBackColor = true;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.ButtonBase).GetMethod("set_UseVisualStyleBackColor", new Type[] { typeof(bool) }));
            //this.buttonOperation.Click += new System.EventHandler(this.buttonOperation_Click);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldftn, buttonOperation_Clickmethod);
            ilgenerator.Emit(OpCodes.Newobj, (typeof(EventHandler)).GetConstructors()[0]);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("add_Click", new Type[] { typeof(EventHandler) }));
            //// 
            //// numericUpDown1
            //// 
            //this.numericUpDown1.Location = new System.Drawing.Point(19, 45);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4, 19);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 45);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //this.numericUpDown1.Name = "numericUpDown1";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Ldstr, "numericUpDown1");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 120);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 20);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //this.numericUpDown1.TabIndex = 1;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //// 
            //// numericUpDown2
            //// 
            //this.numericUpDown2.Location = new System.Drawing.Point(217, 45);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Ldc_I4, 217);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 45);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //this.numericUpDown2.Name = "numericUpDown2";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Ldstr, "numericUpDown1");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //this.numericUpDown2.Size = new System.Drawing.Size(120, 20);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 120);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 20);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //this.numericUpDown2.TabIndex = 2;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Ldc_I4_2);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //// 
            //// label2
            //// 
            //label2.AutoSize = true;
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_AutoSize", new Type[] { typeof(bool) }));
            //label2.Location = new System.Drawing.Point(217, 26);
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldc_I4, 217);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 26);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //label2.Name = "label2";
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldstr, "label2");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //label2.Size = new System.Drawing.Size(53, 13);
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 53);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 13);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //label2.TabIndex = 4;
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldc_I4_4);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //label2.Text = "Number 2";
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Ldstr, "Number 2");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));
            //// 
            //// label3
            //// 
            //label3.AutoSize = true;
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_AutoSize", new Type[] { typeof(bool) }));
            //label3.Location = new System.Drawing.Point(160, 140);
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldc_I4, 160);
            ilgenerator.Emit(OpCodes.Ldc_I4, 140);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //label3.Name = "label3";
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldstr, "label3");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //label3.Size = new System.Drawing.Size(37, 13);
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 37);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 13);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //label3.TabIndex = 5;
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldc_I4_5);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //label3.Text = "Result";
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Ldstr, "Result");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));
            //// 
            //// textBox1
            //// 
            //this.textBox1.Location = new System.Drawing.Point(128, 165);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldc_I4, 128);
            ilgenerator.Emit(OpCodes.Ldc_I4, 165);
            ilgenerator.Emit(OpCodes.Newobj, _pointType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Location", new Type[] { typeof(System.Drawing.Point) }));
            //this.textBox1.Name = "textBox1";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldstr, "textBoxResult");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //this.textBox1.ReadOnly = true;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.TextBoxBase).GetMethod("set_ReadOnly", new Type[] { typeof(bool) }));
            //this.textBox1.Size = new System.Drawing.Size(100, 20);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 100);
            ilgenerator.Emit(OpCodes.Ldc_I4_S, 20);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Size", new Type[] { typeof(System.Drawing.Size) }));
            //this.textBox1.TabIndex = 6;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Ldc_I4_6);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_TabIndex", new Type[] { typeof(int) }));
            //// 
            //// ILCreatedForm
            //// 
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldc_R4, 6F);
            ilgenerator.Emit(OpCodes.Ldc_R4, 13F);
            ilgenerator.Emit(OpCodes.Newobj, _sizefType.GetConstructor(new Type[] { typeof(float), typeof(float) }));
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.ContainerControl).GetMethod("set_AutoScaleDimensions", new Type[] { typeof(SizeF) }));
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.ContainerControl).GetMethod("set_AutoScaleMode", new Type[] { typeof(System.Windows.Forms.AutoScaleMode) }));
            //this.ClientSize = new System.Drawing.Size(357, 221);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldc_I4, 357);
            ilgenerator.Emit(OpCodes.Ldc_I4, 221);
            ilgenerator.Emit(OpCodes.Newobj, _sizeType.GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("set_ClientSize", new Type[] { typeof(System.Drawing.Size) }));
            //this.Controls.Add(this.textBox1);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, textboxResultField);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(label3);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldloc_2);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(label2);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldloc_1);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(label1);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(this.numericUpDown2);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(this.numericUpDown1);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Controls.Add(this.button1);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("get_Controls", new Type[0]));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control.ControlCollection).GetMethod("Add", new Type[] { typeof(System.Windows.Forms.Control) }));
            //this.Name = "ILCreatedForm";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldstr, "ILCreatedForm");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Name", new Type[] { typeof(string) }));
            //this.Text = "ILCreatedForm {0}";
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldstr, "Calculator");
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.Windows.Forms.Control).GetMethod("set_Text", new Type[] { typeof(string) }));

            //Set the Forms icon
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldstr, "calculator.ico");
            ilgenerator.Emit(OpCodes.Newobj, typeof(System.Drawing.Icon).GetConstructor(new Type[] { typeof(string) }));
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Form).GetMethod("set_Icon", new Type[] { typeof(System.Drawing.Icon) }));

            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown1Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.ISupportInitialize).GetMethod("EndInit", new Type[0]));
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, numericupdown2Field);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.ISupportInitialize).GetMethod("EndInit", new Type[0]));
            //this.ResumeLayout(false);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldc_I4_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("ResumeLayout", new Type[] { typeof(bool) }));
            //this.PerformLayout();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, typeof(System.Windows.Forms.Control).GetMethod("PerformLayout", new Type[0]));

            ilgenerator.Emit(OpCodes.Ret);

            // Create Constructor
            var formclassconstructor = CreateConstructor(formclass);
            ilgenerator = formclassconstructor.GetILGenerator();

            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldnull);
            ilgenerator.Emit(OpCodes.Stfld, componentsField);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, _winformType.GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, initializecomponentmethod);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Newobj, (implementedinterface).GetConstructor(new Type[0]));
            ilgenerator.Emit(OpCodes.Stfld, ioperationField);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, button1Field);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, ioperationField);
            ilgenerator.Emit(OpCodes.Callvirt, (implementedinterface).GetMethod("get_Name"));
            ilgenerator.Emit(OpCodes.Callvirt, (typeof(System.Windows.Forms.ButtonBase)).GetMethod("set_Text"));
            ilgenerator.Emit(OpCodes.Ret);

            // Create Dispose method
            var formclassdisposemethod = CreateClassMethod(formclass, "Dispose", null, new Type[] { typeof(bool) }, MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
            formclassdisposemethod.DefineParameter(1, ParameterAttributes.None, "disposing");
            ilgenerator = formclassdisposemethod.GetILGenerator();
            var boolfalse = ilgenerator.DefineLabel();
            var componentnull = ilgenerator.DefineLabel();
            var label1 = ilgenerator.DefineLabel();
            // System.Windows.Forms.Label label2;
            localbuilder = ilgenerator.DeclareLocal(typeof(bool));
            // bool parameter false?
            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Brfalse_S, boolfalse);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            // components null?
            ilgenerator.Emit(OpCodes.Ldfld, componentsField);
            ilgenerator.Emit(OpCodes.Ldnull);
            ilgenerator.Emit(OpCodes.Ceq);
            ilgenerator.Emit(OpCodes.Br_S, label1);
            ilgenerator.MarkLabel(boolfalse);
            ilgenerator.Emit(OpCodes.Ldc_I4_1);
            ilgenerator.MarkLabel(label1);
            ilgenerator.Emit(OpCodes.Stloc_0);
            ilgenerator.Emit(OpCodes.Ldloc_0);
            ilgenerator.Emit(OpCodes.Brtrue_S, componentnull);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, componentsField);
            ilgenerator.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose", new Type[0]));
            ilgenerator.MarkLabel(componentnull);
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            ilgenerator.Emit(OpCodes.Call, (typeof(System.Windows.Forms.Form)).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance)); // protected method
            ilgenerator.Emit(OpCodes.Ret);

            formclass.CreateType();

            return formclass;
        }

        // ========================================  Interface  ====================================================

        private  TypeBuilder CreateIOperationInterface(ModuleBuilder modulebuilder)
        {
            var operationinterface = CreateInterface(modulebuilder, "IOperation");

            // Operation Method
            CreateInterfaceMethod(operationinterface, "Operation", typeof(decimal), new Type[] { typeof(decimal), typeof(decimal) });

            // Name string
            var nameproperty = CreateProperty(operationinterface, "Name", typeof(string), PropertyAttributes.None);

            var get_name = CreateClassMethod(operationinterface, "get_Name", typeof(string), new Type[0], MethodAttributes.Abstract | MethodAttributes.Virtual);
            nameproperty.SetGetMethod(get_name);

            var set_name = CreateClassMethod(operationinterface, "set_Name", null, new Type[] { typeof(string) }, MethodAttributes.Abstract | MethodAttributes.Virtual);
            nameproperty.SetSetMethod(set_name);

            return operationinterface;
        }

        // ========================================  Builder types ====================================================

        private TypeBuilder CreateInterface(ModuleBuilder modulebuilder, string interfacename)
        {
            return modulebuilder.DefineType(interfacename, TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract);
        }

        private MethodBuilder CreateInterfaceMethod(TypeBuilder typebuilder, string methodname, Type methodreturntype, Type[] methodparameters)
        {
            return typebuilder.DefineMethod(methodname, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.HasThis,
                                            methodreturntype, methodparameters);
        }

        private TypeBuilder CreateClass(ModuleBuilder modulebuilder, string classname, Type parent)
        {
            return modulebuilder.DefineType(classname, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit, parent);
        }

        private MethodBuilder CreateClassMethod(TypeBuilder typebuilder, string methodname, Type methodreturntype, Type[] methodparameters, MethodAttributes methodattributes)
        {
            return typebuilder.DefineMethod(methodname, methodattributes, methodreturntype, methodparameters);
        }

        private ConstructorBuilder CreateConstructor(TypeBuilder typebuilder)
        {
            return typebuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, new Type[0]);
        }

        private FieldBuilder CreateField(TypeBuilder typebuilder, string fieldname, Type fieldtype, FieldAttributes fieldattributes)
        {
            return typebuilder.DefineField(fieldname, fieldtype, fieldattributes);
        }

        private PropertyBuilder CreateProperty(TypeBuilder typebuilder, string propertyname, Type fieldtype, PropertyAttributes propertyattributes)
        {
            return typebuilder.DefineProperty(propertyname, propertyattributes, fieldtype, null);
        }

    }
}
