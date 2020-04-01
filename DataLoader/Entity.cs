using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede
{
    public class Entity
    {
        string type;


        private List<Parametr> parametrs;


        public Entity(string type)
        {
            this.type = type;
            parametrs = new List<Parametr>();
        }

        private int FindCollumByName(string name)
        {
            for (int i = 0; i < parametrs.Count; i++)
            {
                if (parametrs[i].name.CompareTo(name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public Parametr this[string column]
        {
            get
            {
                int index = FindCollumByName(column);
                if (index == -1) return null;
                else return parametrs[index];
            }
        }
        public void RemoveParametr(Parametr removed)
        {
            parametrs.Remove(removed);
        }

        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                this.type = value;
            }
        }
        public void AddParametr(Parametr newParametr)
        {
            parametrs.Add(newParametr);
        }

        public List<Parametr> GetParametrs()
        {
            return parametrs;
        }
    }

    public class Parametr
    {
        public string name;
        public string type;
        public Object value;

        public Parametr(string type, string name, Object value)
        {
            this.type = type;
            this.name = name;
            this.value = value;
        }
        public Parametr()
        {
            type = null;
            name = null;
            value = null;
        }

    }




}
