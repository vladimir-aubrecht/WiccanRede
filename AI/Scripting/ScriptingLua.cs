using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WiccanRede.AI.Scripting
{
    class ScriptingLua : IControlMechanism
    {
        LuaInterface.Lua lua;

        public ScriptingLua()
        {
            lua = new LuaInterface.Lua();
            //AI.Scripting.LuaFsmFunctions methods = new WiccanRede.AI.Scripting.LuaFsmFunctions();
            //RegisterFunctions(methods);            
        }
            
        public void LoadScript(string path)
        {
            lua.DoFile(path);
        }

        public object RunFunction(string name, object[] par)
        {
            return lua.GetFunction(name).Call(par);
        }

        public void RegisterFunctions(Object target)
        {
            Type type = target.GetType();

            foreach (MethodInfo info in type.GetMethods())
            {
                foreach (Attribute att in Attribute.GetCustomAttributes(info))
                {
                    if (att is AI.Scripting.AttributScriptMethod)
                    {
                        lua.RegisterFunction((att as AI.Scripting.AttributScriptMethod).GetMethodName(), target, info);
                    }
                }
            }
        }

        #region IControlMechanism Members

        public void Update()
        {
            lua.GetFunction("Update").Call(20);
        }

        #endregion
    }
}
