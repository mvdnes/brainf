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
        private LocalBuilder ptr, mem, tmp;

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

            this.main_function = main_class.DefineMethod("Main",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(int),
                new Type[] { typeof(string[]) });

            this.ilg = main_function.GetILGenerator();

            this.mem = ilg.DeclareLocal(typeof(Array));
            this.ptr = ilg.DeclareLocal(typeof(int));
            this.tmp = ilg.DeclareLocal(typeof(int));

            ilg.Emit(OpCodes.Ldc_I4, 30000);
            ilg.Emit(OpCodes.Newarr, typeof(int));
            ilg.Emit(OpCodes.Stloc, mem);

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Stloc, ptr);
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
                        var right_node = node as ASTNodeRight;
                        if (right_node == null) throw new InvalidCastException("Node had nodeType right but was another type.");

                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldc_I4, (int)right_node.amount);
                        ilg.Emit(OpCodes.Add);
                        ilg.Emit(OpCodes.Stloc, ptr);
                        break;
                    case ASTNodeType.Plus:
                        var plus_node = node as ASTNodePlus;
                        if (plus_node == null) throw new InvalidCastException("Node had nodeType plus but was another type.");

                        ilg.Emit(OpCodes.Ldloc, mem);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        if (plus_node.offset != 0) {
                            ilg.Emit(OpCodes.Ldc_I4, (int)plus_node.offset);
                            ilg.Emit(OpCodes.Add);
                        }
                        // stack: mem, ptr

                        ilg.Emit(OpCodes.Ldloc, mem);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        if (plus_node.offset != 0) {
                            ilg.Emit(OpCodes.Ldc_I4, (int)plus_node.offset);
                            ilg.Emit(OpCodes.Add);
                        }
                        ilg.Emit(OpCodes.Ldelem_I4);
                        // stack: mem, ptr, mem[ptr]

                        ilg.Emit(OpCodes.Ldc_I4, (int)plus_node.amount);
                        ilg.Emit(OpCodes.Add);
                        // stack: mem, ptr, mem[ptr]+val

                        ilg.Emit(OpCodes.Stelem_I4);
                        break;
                    case ASTNodeType.Read:
                        Label read_store = ilg.DefineLabel();

                        ilg.Emit(OpCodes.Ldloc, mem);
                        ilg.Emit(OpCodes.Ldloc, ptr);

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
                        break;
                    case ASTNodeType.Write:
                        ilg.Emit(OpCodes.Ldloc, mem);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelem_I4);
                        ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[] { typeof(char) }));
                        break;
                    case ASTNodeType.Loop:
                        var loop_node = node as ASTNodeLoop;
                        if (loop_node == null) throw new InvalidCastException("Node had nodeType loop but was another type.");

                        Label loopstart = ilg.DefineLabel();
                        Label loopend = ilg.DefineLabel();

                        ilg.Emit(OpCodes.Br, loopend);

                        ilg.MarkLabel(loopstart);
                        this.go(loop_node.innerChild());
                        ilg.MarkLabel(loopend);

                        ilg.Emit(OpCodes.Ldloc, mem);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelem_I4);
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Bne_Un, loopstart);
                        break;

                    case ASTNodeType.CopyLoop:
                        var node_copyloop = (ASTNodeCopyLoop)node;

                        if (node_copyloop.isEmpty()) {
                            ilg.Emit(OpCodes.Ldloc, mem);
                            ilg.Emit(OpCodes.Ldloc, ptr);
                            ilg.Emit(OpCodes.Ldc_I4_0);
                            ilg.Emit(OpCodes.Stelem_I4);
                        }
                        else {
                            Label end_of_copy_loop = ilg.DefineLabel();

                            ilg.Emit(OpCodes.Ldloc, mem);
                            ilg.Emit(OpCodes.Ldloc, ptr);
                            ilg.Emit(OpCodes.Ldelem_I4);

                            ilg.Emit(OpCodes.Dup); // save one for the Beq
                            ilg.Emit(OpCodes.Stloc, tmp);

                            ilg.Emit(OpCodes.Ldloc, mem);
                            ilg.Emit(OpCodes.Ldloc, ptr);
                            ilg.Emit(OpCodes.Ldc_I4_0);
                            ilg.Emit(OpCodes.Stelem_I4);

                            ilg.Emit(OpCodes.Ldc_I4_0);
                            ilg.Emit(OpCodes.Beq, end_of_copy_loop);

                            foreach (var entry in node_copyloop)
                            {
                                ilg.Emit(OpCodes.Ldloc, mem);
                                ilg.Emit(OpCodes.Ldloc, ptr);
                                ilg.Emit(OpCodes.Ldc_I4, entry.offset);
                                ilg.Emit(OpCodes.Add);
                                // mem, ptr+o
                            
                                ilg.Emit(OpCodes.Ldloc, tmp);
                                ilg.Emit(OpCodes.Ldc_I4, entry.factor);
                                ilg.Emit(OpCodes.Mul);
                                // mem, ptr+o, mem[ptr]*f

                                ilg.Emit(OpCodes.Ldloc, mem);
                                ilg.Emit(OpCodes.Ldloc, ptr);
                                ilg.Emit(OpCodes.Ldc_I4, entry.offset);
                                ilg.Emit(OpCodes.Add);
                                // mem, ptr+o, mem[ptr]*f, mem, ptr+o

                                ilg.Emit(OpCodes.Ldelem_I4);
                                // mem, ptr+o, mem[ptr]*f, mem[ptr+o]

                                ilg.Emit(OpCodes.Add);
                                ilg.Emit(OpCodes.Stelem_I4);
                            }

                            ilg.MarkLabel(end_of_copy_loop);
                        }
                        break;
                }
                node = node.nextChild();
            }
        }
    }
}
