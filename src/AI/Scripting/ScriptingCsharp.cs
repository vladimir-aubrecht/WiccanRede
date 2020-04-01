using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WiccanRede.AI.Scripting
{
    /// <summary>
    /// class offering loading c# scripts and running methods from them, one script = one instance
    /// </summary>
    class ScriptingCsharp
    {
        System.Reflection.Assembly scriptAssembly;
        Microsoft.CSharp.CSharpCodeProvider provider;
        System.CodeDom.Compiler.CompilerParameters par;
        IScripting script;
        NPC npc;

        /// <summary>
        /// ctor, init c# compiler, linkl libraries for compilation
        /// </summary>
        public ScriptingCsharp()
        {
            provider = new Microsoft.CSharp.CSharpCodeProvider();
            par = new System.CodeDom.Compiler.CompilerParameters();
            par.ReferencedAssemblies.Add("AI.dll");
            par.ReferencedAssemblies.Add("Logging.dll");
            par.ReferencedAssemblies.Add("System.Drawing.dll");
            par.GenerateExecutable = false;
#if DEBUG
            par.IncludeDebugInformation = true;
#else
            par.IncludeDebugInformation = false;
#endif
            par.GenerateInMemory = true;
        }

        /// <summary>
        /// loads the *.cs file and compile it, logs compilation result
        /// </summary>
        /// <param name="path">path to .cs file</param>
        public void LoadScript(string path)
        {
            System.CodeDom.Compiler.CompilerResults results =
                provider.CompileAssemblyFromFile(this.par, new string[] { path });
            Logging.Logger.AddImportant("kompilace skritpu, pocet chyb: " + results.Errors.Count.ToString());

            if (results.Errors.Count > 0)
            {
                foreach (System.CodeDom.Compiler.CompilerError err in results.Errors)
                {
                    Logging.Logger.AddWarning("chyba ve skriptu " + err.ErrorNumber + " na radce " + err.Line +
                        ": " + err.ErrorText);
                }
            }
            else
            {
                scriptAssembly = results.CompiledAssembly;
                try
                {
                    CreateInstance();
                }
                catch (Exception ex)
                {
                    Logging.Logger.AddInfo("Chyba pri vytvareni instance scriptu " + path + " - " + ex.ToString());
                }
                Logging.Logger.AddInfo("skript prelozen \n" + scriptAssembly.ToString());
            }
        }

        private void CreateInstance()
        {
            Type type = scriptAssembly.GetTypes()[0];
            ConstructorInfo ci = type.GetConstructors()[0];
            List<Object> constructorPars = new List<object>();
            foreach (ParameterInfo pi in ci.GetParameters())
            {
                if (pi.ParameterType.Name == "Map")
                {
                    constructorPars.Add(Map.GetInstance());
                }
            }

            if (type.GetInterface("IScripting") != null)
            {
                script = (IScripting)Activator.CreateInstance(type, constructorPars.ToArray()); 
            }
        }

        /// <summary>
        /// Run method from loaded script 
        /// </summary>
        /// <remarks>script has to be loaded and compiled without errors</remarks>
        /// <seealso cref="LoadScript"/>
        /// <param name="methodName">Method name to call</param>
        /// <param name="pars">parameters to forward to the called function</param>
        /// <returns>object which was returned by the method</returns>
        public object RunMethod(string methodName, object[] pars)
        {
            if (scriptAssembly == null)
                return null;

            Type t = scriptAssembly.GetTypes()[0];// ("WiccanRede.AI.Scripts.ScriptClass");
            object result = t.GetMethod(methodName).Invoke(null, pars);
            //Logging.Logger.AddImportant(result.ToString());
            return result;
        }

        public object[] RunMethodFromType(string MethodName, object[] pars)
        {
            if (script == null)
                return null;

            return this.script.Update(pars);
        }
    }
}
