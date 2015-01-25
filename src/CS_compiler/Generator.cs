using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace BrainfCompiler
{
    class Generator
    {
        private String destination;
        private AssemblyBuilder assembly_builder;
        private TypeBuilder main_class;
        private MethodBuilder main_function;
        private ILGenerator ilg;
        private FieldBuilder ptr, mem;

        private Generator(string destination)
        {
            this.destination = destination;
        }

        public static bool run(String destination, ASTNode tree)
        {
            Generator generator = new Generator(destination);
            generator.genPre();
            generator.go(tree);
            generator.genPost();

            return true;
        }

        private void genPre()
        {
            AssemblyName assembly_name = new AssemblyName();
            assembly_name.Name = "BrainfuckCode";
            AppDomain app_domain = AppDomain.CurrentDomain;
            this.assembly_builder = app_domain.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Save);

            ModuleBuilder module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name, this.destination);

            this.main_class = module_builder.DefineType("Brainfuck.Code", TypeAttributes.Public | TypeAttributes.Class);

            this.mem = main_class.DefineField("mem", typeof(Array), FieldAttributes.Private | FieldAttributes.Static);
            this.ptr = main_class.DefineField("ptr", typeof(int), FieldAttributes.Private | FieldAttributes.Static);

            this.main_function = main_class.DefineMethod("Main",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(int),
                new Type[] { typeof(string[]) });

            this.ilg = main_function.GetILGenerator();

            ilg.Emit(OpCodes.Ldc_I4, 30000);
            ilg.Emit(OpCodes.Newarr, typeof(int));
            ilg.Emit(OpCodes.Stsfld, mem);

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Stsfld, ptr);
        }

        private void genPost()
        {
            ilg.EmitWriteLine("");

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);
            
            this.main_class.CreateType();
            this.assembly_builder.SetEntryPoint(this.main_function, PEFileKinds.ConsoleApplication);
            this.assembly_builder.Save(this.destination);
        }

        private void go(ASTNode node)
        {
            while (node.nodeType != ASTNodeType.Leaf)
            {
                switch (node.nodeType)
                {
                    case ASTNodeType.Right:
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        ilg.Emit(OpCodes.Ldc_I4, (int)node.amount);
                        ilg.Emit(OpCodes.Add);
                        ilg.Emit(OpCodes.Stsfld, ptr);

                        node = node.childLeft;
                        break;
                    case ASTNodeType.Plus:
                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        // stack: mem, ptr

                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        ilg.Emit(OpCodes.Ldelem_I4);
                        // stack: mem, ptr, mem[ptr]

                        ilg.Emit(OpCodes.Ldc_I4, (int)node.amount);
                        ilg.Emit(OpCodes.Add);
                        // stack: mem, ptr, mem[ptr]+val

                        ilg.Emit(OpCodes.Stelem_I4);

                        node = node.childLeft;
                        break;
                    case ASTNodeType.Read:
                        Label read_store = ilg.DefineLabel();

                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);

                        ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("Read"));
                        // stack: mem, ptr, val

                        ilg.Emit(OpCodes.Dup);
                        ilg.Emit(OpCodes.Ldc_I4_M1);
                        // stack: mem, ptr, val, val, -1

                        ilg.Emit(OpCodes.Bne_Un, read_store);
                        // stack: mem, ptr, val

                        ilg.Emit(OpCodes.Pop);
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        // stack: mem, ptr, 0

                        ilg.MarkLabel(read_store);

                        ilg.Emit(OpCodes.Stelem_I4);

                        node = node.childLeft;
                        break;
                    case ASTNodeType.Write:
                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        ilg.Emit(OpCodes.Ldelem_I4);
                        ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[] { typeof(char) }));
                        
                        //ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("get_Out"));
                        //ilg.Emit(OpCodes.Call, typeof(TextWriter).GetMethod("Flush"));

                        node = node.childLeft;
                        break;
                    case ASTNodeType.Loop:
                        Label loopstart = ilg.DefineLabel();
                        Label loopend = ilg.DefineLabel();

                        ilg.Emit(OpCodes.Br, loopend);

                        ilg.MarkLabel(loopstart);
                        this.go(node.childLeft);
                        ilg.MarkLabel(loopend);

                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        ilg.Emit(OpCodes.Ldelem_I4);
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Bne_Un, loopstart);

                        node = node.childRight;
                        break;

                    case ASTNodeType.SetZero:
                        ilg.Emit(OpCodes.Ldsfld, mem);
                        ilg.Emit(OpCodes.Ldsfld, ptr);
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Stelem_I4);

                        node = node.childLeft;
                        break;
                }
            }
        }
    }
}
