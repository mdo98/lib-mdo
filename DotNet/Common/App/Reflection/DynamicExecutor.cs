using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CSharp;

namespace System.Reflection
{
    public abstract class DynamicExecutor
    {
        public abstract object Execute(object[] args);

        public const string ParameterNamePrefix = "arg";
        public const string ReturnValueParamName = "returnValue";

        private const string DynamicExecutorImplTypeName = "DynamicExecutorImpl";
        private const string CodeTemplate = @"
namespace $(0)
{
    public class $(1) : DynamicExecutor
    {
        public override object Execute(object[] args)
        {
            return this.ExecuteImpl($(2));
        }

        private object ExecuteImpl($(3))
        {
            object $(4) = null;
            $(5)
            return $(4);
        }
    }
}
";
        private static readonly CompilerParameters CompilerOptions = new CompilerParameters()
        {
            GenerateExecutable = false,
            GenerateInMemory = true,
            TreatWarningsAsErrors = false,
        };
        private static readonly CodeDomProvider Compiler = new CSharpCodeProvider();
        private static readonly string Namespace = typeof(DynamicExecutor).Namespace;
        private static readonly object SyncRoot = new object();

        public static string GetParameterName(int index)
        {
            return string.Format("{0}_{1}", ParameterNamePrefix, index);
        }

        private static string GetMacroSymbol(int index)
        {
            return string.Format("$({0})", index);
        }

        public static DynamicExecutor Create(string codeSnippet, Type[] paramTypes)
        {
            const string paramSeparator = ", ";
            StringBuilder parameterDeclaration = new StringBuilder(),
                          parameterInvocation  = new StringBuilder();
            if (paramTypes != null && paramTypes != Type.EmptyTypes)
            {
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    string paramName = GetParameterName(i);
                    parameterDeclaration.AppendFormat("{0} {1}", paramTypes[i].FullName, paramName);
                    parameterInvocation.AppendFormat("({0})args[{1}]", paramTypes[i].FullName, i);

                    if (i < paramTypes.Length-1)
                    {
                        parameterDeclaration.Append(paramSeparator);
                        parameterInvocation.Append(paramSeparator);
                    }
                }
            }
            string code = CodeTemplate;
            string[] macros = new string[]
            {
                Namespace,
                DynamicExecutorImplTypeName,
                parameterInvocation.ToString(),
                parameterDeclaration.ToString(),
                ReturnValueParamName,
                codeSnippet,
            };
            for (int i = 0; i < macros.Length; i++)
            {
                code = code.Replace(GetMacroSymbol(i), macros[i]);
            }

            CompilerResults compilerResults;
            lock (SyncRoot)
            {
                CompilerOptions.ReferencedAssemblies.Clear();
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    CompilerOptions.ReferencedAssemblies.Add(asm.Location);
                }
                compilerResults = Compiler.CompileAssemblyFromSource(CompilerOptions, code);
            }

            if (compilerResults.Errors.Count > 0 || compilerResults.CompiledAssembly == null)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine,
                    new[] { "Error compiling code.", code }.Union(
                    compilerResults.Errors.Cast<CompilerError>().Select(item => string.Format(
                        "{0}: {1} (ln {2}, col {3})", item.ErrorNumber, item.ErrorText, item.Line, item.Column)))));
            }

            Type dynamicExecutorImplType = compilerResults.CompiledAssembly.GetType(string.Join(".", Namespace, DynamicExecutorImplTypeName));
            return (DynamicExecutor)Activator.CreateInstance(dynamicExecutorImplType);
        }
    }
}
