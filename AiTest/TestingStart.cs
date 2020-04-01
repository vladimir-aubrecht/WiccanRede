using System;
using System.Collections.Generic;
using System.Text;
using Logging;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace WiccanRede.AI
{
    class TestingStart
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testovaci konzole pro knihovnu AI.");
            Logger.InitLogger();
            Logger.bWriteToOutput = true;
            
            Test test = new Test();
            //System.Windows.Forms.Application.Run(new TestForm());
        }

    }

    class Test
    {
        AICore ai;
        Grid grid;
        static LuaInterface.Lua lua;

        //DynamicProgramming dynamic;
        CharacterNPC mikelChar;
        AI.CharacterNPC[] characters;
        string skript1 = "Scripting\\Scripts\\luaFSM.l";

        public Test()
        {
            //dynamic = new DynamicProgramming();
            //mikelChar = new CharacterNPC("Settings/Mikel.xml");
            //Init();
            //Go();
            lua = new LuaInterface.Lua();
            AI.Scripting.ScriptFunctions methods = new WiccanRede.AI.Scripting.ScriptFunctions(new NPC(mikelChar, new Microsoft.DirectX.Vector3(), null, null));
            RegisterNewMethod(methods);
            lua.DoFile(skript1);
            lua.GetFunction("Update").Call();
            //RunLua();
            //ai = new AICore(grid);
            string str = Console.ReadLine();
        }

        private void RunLua()
        {
            string cmd = "";
            object[] result;
            
            //MethodInfo info = methods.GetType().GetMethod("WriteEnum", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
            //lua.RegisterFunction("WriteEnum", methods, info);
            bool a = false;
            int b = 5;
            while (cmd != "quit")
            {
                Console.Write(">");
                cmd = Console.ReadLine();
                try
                {
                    result = lua.DoString(cmd);
                    //lua.GetFunction("Out").Call(new object[] { a, b });
                    result = lua.GetFunction("Info").Call(new object[] {});
                    if (result != null)
                    {
                        foreach (object obj in result)
                        {
                            Logger.AddImportant("-" + obj.ToString());
                        } 
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddError(ex.ToString());
                }
            }
        }

        public static void RegisterNewMethod(Object target)
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

        private void Init()
        {
            grid = new Grid(new Bitmap("Settings/map.png"), null);
            ai = new AICore(grid);
            //ai.AddPlayer(mikelChar, new Microsoft.DirectX.Vector3(0, 0, 0), new Entity(), "BasicFSM");
            //ai.AddPlayer(mikelChar, new Microsoft.DirectX.Vector3(0, 0, 0), new Entity(), "BasicFSM");

            string[] npcList = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Settings\\level1npc.ini");
            characters = new WiccanRede.AI.CharacterNPC[npcList.Length];

            for (int i = 0; i < npcList.Length; i++)
            {
                try
                {
                    characters[i] = new WiccanRede.AI.CharacterNPC("Settings\\" + npcList[i] + ".xml");
                    //ai.AddPlayer(characters[i], new Microsoft.DirectX.Vector3(i,i,i), new Entity(), "BasicFSM");                    
                }
                catch (Exception ex)
                {
                    Logging.Logger.AddWarning("Chyba pri nacitani NPC: " + npcList[i] + " - " + ex.ToString());
                }
            }
        }

        private void Go()
        {
            ai.Update("Pub");
            string str = "";
            while (str.Length == 0)
            {
                ai.Update();
                System.Threading.Thread.Sleep(100);
                if (System.Console.KeyAvailable)
                {
                    str = Console.ReadLine();
                }
            }
        }

        private void TestDP()
        {
            Status status = new Status(mikelChar.hp, mikelChar.mana, 100, true, new System.Drawing.Point(0, 0), 1, 800, new List<IActing>());
            //dynamic.CreateStateSpace(new ConflictState(status, status), mikelChar, mikelChar, ActionType.Attack);

            //dynamic.Iterate(new ConflictState(status, status));
        }
    }
}
