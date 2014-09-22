using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Fluid {
   class Program {
      static void Main(string[] args) {
         var outputName = args[0];
         string entryPoint = null;
         for (int i = 1; i < args.Length; i++) {
            if (args[i] == "-e") {
               entryPoint = args[i + 1];
               i++;
            } else {
               throw new ArgumentException("Not sure what to do with " + args[i]);
            }
         }

         if (!outputName.Contains(".")) outputName += entryPoint != null ? ".exe" : ".dll";
         var assemblyName = outputName.Split('.')[0];
         var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
         var module = assembly.DefineDynamicModule(assemblyName, outputName);

         // TODO search the current directory for .fl files and compile them
         // TODO if the output name matches a folder in the current directory, search it recursively instead

         Compile(module, "MyFile");

         if (entryPoint != null) {
            var entryTypeLength = entryPoint.LastIndexOf('.');
            var entryTypeName = entryPoint.Substring(0, entryTypeLength);
            var entryMethodName = entryPoint.Substring(entryTypeLength + 1);
            assembly.SetEntryPoint(assembly.GetType(entryTypeName).GetMethod(entryMethodName, new Type[0]));
         }
         assembly.Save(outputName);
      }

      static void Compile(ModuleBuilder builder, string input) {
         // TODO load from files / folders
         var type = builder.DefineType(input.Split('.')[0], TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

         // TODO load this from a parse tree
         var testMethod = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(void), null);
         var generator = testMethod.GetILGenerator();
         FakeBodyHelloWorld(generator);

         type.CreateType();
      }

      static void FakeBodyHelloWorld(ILGenerator generator) {
         generator.Emit(OpCodes.Ldstr, "Hello World!");
         var method = GetTypeByName("System.Console").GetMethod("WriteLine", Args<string>());
         generator.EmitCall(OpCodes.Call, method, null); //  typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }), null);
         generator.Emit(OpCodes.Ret);
      }

      static void FakeBodyMultiReturnTest(TypeBuilder type, ILGenerator generatorForMainMethod) {
         // TODO create a new method and have it return two values
         // TODO do something with those two values on the stack from the main method.
      }

      static Type GetTypeByName(string name) {
         return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.DefinedTypes)
            .Aggregate(Enumerable.Concat)
            .Single(type => type.FullName == name);
      }

      static Type[] Args<A>() { return new Type[] { typeof(A) }; }
      static Type[] Args<A, B>() { return new Type[] { typeof(A), typeof(B) }; }
      static Type[] Args<A, B, C>() { return new Type[] { typeof(A), typeof(B), typeof(C) }; }
   }

   /*
   public interface Expression {
      public Type ExpressionResult { get; }
   }

   class BinaryExpression : Expression {
      public readonly Expression first;
      public readonly Expression second;
      public readonly string Operator;

      public Type ExpressionResult { get { return first.ExpressionResult; } } // TODO get the type's implementation of the binary method and find its result.
   }

   class UnaryExpression : Expression {
      public readonly Expression expression;
      public readonly string Operator;

      public Type ExpressionResult { get { return expression.ExpressionResult; } }
   }

   class MethodCall : Expression {
      public readonly object Instance;
      public readonly string ContainingClass;
      public readonly MethodInfo Method;
      public readonly Expression[] Args;

      public Type[] ExpressionResult { get { return Method.ReturnType; } } // what if a method returns a tuple?
   }

   class Assignment : Expression {
      public readonly object[] Variables;
      public readonly string[] Expressions;

      public Type[] ExpressionResult { get { return new Type[] { typeof(void) }; } }
   }

   // expressions of type unit can be chained together.
   // only the last result of a unit chain need not be a unit type.
   class UnitChain : Expression {
      public readonly Expression[] Chain;

      public Type ExpressionResult { get { return Chain.Last().ExpressionResult; } }
   }

   class Constant : Expression {
      public object value;

      public Type ExpressionResult { get; set; }
   }

   class LocalVariable : Expression {
      public string name;

      public Type ExpressionResult { get; set; }
   }
   //*/

   /*
    * The binary tree solution:
    * 
    * SumAndCountHelper(node)
    *    if node==null
    *       0, 1.0
    *    else
    *       leftSum, leftCount = SumAndCountHelper node.Left
    *       rightSum, rightCount = SumAndCountHelper node.Right
    *       leftSum + rightSum + node.Value, left.Count + right.Count + 1.0
    * FindAverageInTree(root)
    *    sum, count = SumAndCountHelper root
    *    sum / count
    * 
    * 
    * 
    * load arg0
    * branch if null: nullbranch
    * 
    * load arg0
    * property Left
    * call SumAndCountHelper
    * store leftSum
    * store leftCount
    * 
    * load arg0
    * property Right
    * call SumAndCountHelper
    * store rightSum
    * store rightCount
    * 
    * push leftSum
    * push rightSum
    * add
    * push arg0
    * property Value
    * add
    * push leftCount
    * push rightCount
    * add
    * push 1.0
    * add
    * 
    * 
    */

}
