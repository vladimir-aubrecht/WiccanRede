using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.AI.Scripting
{
    public class AttributScriptMethod: Attribute
    {
        string name;
        string doc;
        string[] pars;

        public AttributScriptMethod(string name, string doc, string [] pars)
        {
            this.name = name;
            this.doc = doc;
            this.pars = pars;
        }
        public AttributScriptMethod(string name, string doc)
        {
            this.name = name;
            this.doc = doc;
            this.pars = new string[] { };
        }

        public string GetMethodName()
        {
            return this.name;
        }

        public string GetDoc()
        {
            return this.doc;
        }
        public string[] GetParameters()
        {
            return this.pars;
        }
    }
}
