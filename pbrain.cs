using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace ParksComputing.Pbrain {
   /// <summary>
   /// Compiler implements the pbrain compiler.
   /// </summary>
   class Compiler {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      static void Main(string[] args) {
         if (args.Length > 0) {
            String fileName = args[0];

            Compiler compiler;
            compiler = new Compiler(fileName);

            Type myType = compiler.Compile();
         }
      }

      private String fileName;
      private String asmName;
      private String asmFileName;

      private AssemblyBuilder myAsmBldr;

      private FieldBuilder mem;
      private FieldBuilder mp;
      private FieldBuilder tmp;
      private FieldBuilder vtbl;

      private TypeBuilder myTypeBldr;

      private MethodInfo readMI;
      private MethodInfo writeMI;
      private MethodInfo hashAddMI;
      private MethodInfo hashGetMI;

      private int methodCount;
      private int callCount;


      void Ldc(ILGenerator il, int count) {
         switch (count) {
            case 0:
               il.Emit(OpCodes.Ldc_I4_0);
               break;

            case 1:
               il.Emit(OpCodes.Ldc_I4_1);
               break;

            case 2:
               il.Emit(OpCodes.Ldc_I4_2);
               break;

            case 3:
               il.Emit(OpCodes.Ldc_I4_3);
               break;

            case 4:
               il.Emit(OpCodes.Ldc_I4_4);
               break;

            case 5:
               il.Emit(OpCodes.Ldc_I4_5);
               break;

            case 6:
               il.Emit(OpCodes.Ldc_I4_6);
               break;

            case 7:
               il.Emit(OpCodes.Ldc_I4_7);
               break;

            case 8:
               il.Emit(OpCodes.Ldc_I4_8);
               break;

            default:
               il.Emit(OpCodes.Ldc_I4, count);
               break;
         }
      }


      void Forward(ILGenerator il, int count) {
         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldc.i4 1	
         Ldc(il, count);

         //add
         il.Emit(OpCodes.Add);

         //stsfld int32 pbout.mp
         il.Emit(OpCodes.Stsfld, mp);
      }


      void Back(ILGenerator il, int count) {
         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldc.i4 1	
         Ldc(il, count);

         //sub
         il.Emit(OpCodes.Sub);

         //stsfld int32 pbout.mp
         il.Emit(OpCodes.Stsfld, mp);
      }


      void Plus(ILGenerator il, int count) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         //ldc.i4 1
         Ldc(il, count);

         //add
         il.Emit(OpCodes.Add);

         //stsfld int32 pbout.tmp
         il.Emit(OpCodes.Stsfld, tmp);

         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldsfld int32 pbout.tmp	
         il.Emit(OpCodes.Ldsfld, tmp);

         //stelem.i4
         il.Emit(OpCodes.Stelem_I4);
      }


      void Minus(ILGenerator il, int count) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         //ldc.i4 1	
         Ldc(il, count);

         //sub
         il.Emit(OpCodes.Sub);

         //stsfld int32 pbout.tmp
         il.Emit(OpCodes.Stsfld, tmp);

         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldsfld int32 pbout.tmp	
         il.Emit(OpCodes.Ldsfld, tmp);

         //stelem.i4
         il.Emit(OpCodes.Stelem_I4);
      }


      void Read(ILGenerator il) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //call void [mscorlib]System.Console.Write(char)
         il.EmitCall(OpCodes.Call, readMI, null);

         //stelem.i4
         il.Emit(OpCodes.Stelem_I4);
      }


      void Write(ILGenerator il) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         //call void [mscorlib]System.Console.Write(char)
         il.EmitCall(OpCodes.Call, writeMI, null);
      }


      void LoopBegin(ILGenerator il, Label endLabel) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         //brfalse loop_1_end
         il.Emit(OpCodes.Brfalse, endLabel);
      }


      void LoopEnd(ILGenerator il, Label beginLabel) {
         //br loop_1_start
         il.Emit(OpCodes.Br, beginLabel);
      }


      void Call(ILGenerator il) {
         //ldsfld object pbout.vtbl
         il.Emit(OpCodes.Ldsfld, vtbl);

         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         //box int32
         il.Emit(OpCodes.Box, typeof(int));

         //call instance object [mscorlib]System.Collections.Hashtable.get_Item(object)
         il.EmitCall(OpCodes.Call, hashGetMI, null);

         //calli void()
         il.EmitCalli(OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, null, null);
      }


      void Zero(ILGenerator il) {
         //ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);

         //ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);

         //ldc.i4.0
         il.Emit(OpCodes.Ldc_I4_0);

         //stelem.i4
         il.Emit(OpCodes.Stelem_I4);
      }


      Type Compile() {
         // .field private static int32 mp
         mp = myTypeBldr.DefineField("mp", typeof(int), FieldAttributes.Private | FieldAttributes.Static);

         // .field private static int32[] mem
         mem = myTypeBldr.DefineField("mem", typeof(Array), FieldAttributes.Private | FieldAttributes.Static);

         // .field private static int32 tmp
         tmp = myTypeBldr.DefineField("tmp", typeof(int), FieldAttributes.Private | FieldAttributes.Static);

         // .method private static int32 main() cil managed
         MethodBuilder mainBldr = myTypeBldr.DefineMethod(
            "main",
            (MethodAttributes)(MethodAttributes.Private | MethodAttributes.Static),
            typeof(int),
            null
            );

         ILGenerator il = mainBldr.GetILGenerator();


         // ldc.i4 30000
         il.Emit(OpCodes.Ldc_I4, 30000);
         // newarr [mscorlib]System.Int32
         il.Emit(OpCodes.Newarr, typeof(int));
         // stsfld int32[] pbout.mem
         il.Emit(OpCodes.Stsfld, mem);

         // ldc.i4 0
         il.Emit(OpCodes.Ldc_I4_0);
         // stsfld int32 pbout.mp
         il.Emit(OpCodes.Stsfld, mp);


         Parse(il);


         // ldsfld int32[] pbout.mem
         il.Emit(OpCodes.Ldsfld, mem);
         // ldsfld int32 pbout.mp
         il.Emit(OpCodes.Ldsfld, mp);
         // ldelem.i4 
         il.Emit(OpCodes.Ldelem_I4);

         // ret
         il.Emit(OpCodes.Ret);


         Type pboutType = myTypeBldr.CreateType();
         myAsmBldr.SetEntryPoint(mainBldr);
         myAsmBldr.Save(asmFileName);
         Console.WriteLine("Assembly saved as '{0}'.", asmFileName);

         return pboutType;
      }


      void Parse(ILGenerator il) {
         using (FileStream fs = File.OpenRead(fileName)) {
            char c;
            int n;

            Queue q = new Queue();

            while ((n = fs.ReadByte()) != -1) {
               c = (char)n;
               q.Enqueue(c);

               if (c == ':') {
                  ++callCount;
               }
            }

            if (callCount > 0) {
               // .field private static object vtbl
               vtbl = myTypeBldr.DefineField("vtbl", typeof(Object), FieldAttributes.Private | FieldAttributes.Static);

               //newobj instance void [mscorlib]System.Collections.Hashtable..ctor()
               Type hashtableType = typeof(System.Collections.Hashtable);
               ConstructorInfo constructorInfo = hashtableType.GetConstructor(
                  (BindingFlags.Instance | BindingFlags.Public),
                  null,
                  CallingConventions.HasThis,
                  System.Type.EmptyTypes,
                  null
                  );
               il.Emit(OpCodes.Newobj, constructorInfo);
               //stsfld object pbout.vtbl
               il.Emit(OpCodes.Stsfld, vtbl);
            }

            Interpret(q, il);
         }
      }


      MethodBuilder Procedure(Queue q) {
         Type[] temp0 = { myTypeBldr };
         StringBuilder sb = new StringBuilder();
         sb.Append("pb_");
         sb.Append(methodCount);
         String name = sb.ToString();

         MethodBuilder procBldr = myTypeBldr.DefineMethod(
            name,
            (MethodAttributes.Private | MethodAttributes.Static),
            null,
            System.Type.EmptyTypes
            );

         ILGenerator il = procBldr.GetILGenerator();

         Interpret(q, il);

         // ret
         il.Emit(OpCodes.Ret);

         return procBldr;
      }


      int CountDuplicates(Queue q, char c) {
         int count = 1;
         char inst = c;

         while (c == inst && q.Count > 0) {
            c = (char)q.Peek();

            if (c == inst) {
               c = (char)q.Dequeue();
               ++count;
            }
         }

         return count;
      }


      void Interpret(Queue q, ILGenerator il) {
         System.Collections.IEnumerator myEnumerator = q.GetEnumerator();

         char c;
         byte b;

         while (q.Count > 0) {
            c = (char)q.Dequeue();

            switch (c) {
               case '+':
                  Plus(il, CountDuplicates(q, c));
                  break;

               case '-':
                  Minus(il, CountDuplicates(q, c));
                  break;

               case '>':
                  Forward(il, CountDuplicates(q, c));
                  break;

               case '<':
                  Back(il, CountDuplicates(q, c));
                  break;

               case ',':
                  Read(il);
                  break;

               case '.':
                  Write(il);
                  break;

               case '[': {
                     if (q.Count > 0) {
                        Queue lq = new Queue();

                        int nest = 0;
                        int startPos = q.Count;
                        bool pair = false;
                        bool zero = false;
                        bool opt = true;

                        // Find the matching ]
                        while (q.Count > 0) {
                           c = (char)q.Dequeue();

                           if (c == '[') {
                              ++nest;
                           }
                           else if (c == ']') {
                              if (nest > 0) {
                                 --nest;
                              }
                              else {
                                 pair = true;
                                 break;
                              }
                           }
                           // Check for null loop, [-], which set the current cell
                           // to zero. There's no need to loop. Just store a zero
                           // and move on.
                           else if (opt && c == '-' && (startPos - q.Count) == 1) {
                              opt = false;

                              // If the next character is the end of the loop...
                              if ((char)q.Peek() == ']') {
                                 // Eat the ] and stop the loop
                                 c = (char)q.Dequeue();
                                 zero = true;
                                 break;
                              }
                           }

                           lq.Enqueue(c);
                        }

                        if (zero) {
                           Zero(il);
                           break;
                        }

                        // If no matching ] is found in source block, report error.
                        if (q.Count != 0 && !pair) {
                           // throw System.Exception();
                        }

                        Label beginLabel = il.DefineLabel();
                        Label endLabel = il.DefineLabel();

                        il.MarkLabel(beginLabel);
                        LoopBegin(il, endLabel);

                        Interpret(lq, il);
                        LoopEnd(il, beginLabel);
                        il.MarkLabel(endLabel);
                     }
                  }
                  break;

               case '(': {
                     // LoopBegin(il, endLabel);

                     if (q.Count > 0) {
                        bool pair = false;

                        Queue lq = new Queue();

                        int nest = 0;

                        // Find the matching )
                        while (q.Count > 0) {
                           c = (char)q.Dequeue();

                           if (c == '(') {
                              ++nest;
                           }
                           else if (c == ')') {
                              if (nest > 0) {
                                 --nest;
                              }
                              else {
                                 pair = true;
                                 break;
                              }
                           }

                           lq.Enqueue(c);
                        }

                        // If no matching ) is found in source block, report error.
                        if (q.Count != 0 && !pair) {
                           // throw 5;
                        }

                        MethodBuilder procBldr = Procedure(lq);

                        //ldsfld object pbout.vtbl
                        il.Emit(OpCodes.Ldsfld, vtbl);

                        //ldsfld int32[] pbout.mem
                        il.Emit(OpCodes.Ldsfld, mem);

                        //ldsfld int32 pbout.mp
                        il.Emit(OpCodes.Ldsfld, mp);

                        //ldelem.i4 
                        il.Emit(OpCodes.Ldelem_I4);

                        //box int32
                        il.Emit(OpCodes.Box, typeof(int));

                        //ldftn void pbout.pb_0()
                        il.Emit(OpCodes.Ldftn, procBldr);

                        //call instance void [mscorlib]System.Collections.Hashtable.Add(object,object)
                        il.EmitCall(OpCodes.Call, hashAddMI, null);
                     }

                     ++methodCount;
                  }
                  break;

               case ':':
                  Call(il);
                  break;

               default:
                  break;
            }
         }
      }


      Compiler(String fileNameInit) {
         fileName = fileNameInit;
         methodCount = 0;
         callCount = 0;
         asmName = Path.GetFileNameWithoutExtension(fileName);
         asmFileName = Path.GetFileName(Path.ChangeExtension(fileName, ".exe"));

         AssemblyName myAsmName = new AssemblyName();
         myAsmName.Name = asmName;

         myAsmBldr = Thread.GetDomain().DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);

         Type[] temp1 = { typeof(Char) };
         writeMI = typeof(Console).GetMethod("Write", temp1);
         readMI = typeof(Console).GetMethod("Read");

         Type[] temp2 = { typeof(Object), typeof(Object) };
         hashAddMI = typeof(System.Collections.Hashtable).GetMethod("Add", temp2);

         Type[] temp3 = { typeof(Object) };
         hashGetMI = typeof(System.Collections.Hashtable).GetMethod("get_Item", temp3);

         // .class private auto ansi pbout extends [mscorlib]System.Object
         ModuleBuilder myModuleBldr = myAsmBldr.DefineDynamicModule(asmFileName, asmFileName);
         myTypeBldr = myModuleBldr.DefineType(asmName);
      }
   };
}
