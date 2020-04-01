using System;
using System.Collections.Generic;
using System.Text;
using Logging;

namespace WiccanRede.AI
{
    class TestingStart
    {
        static DynamicProgramming dynamic;
        static CharacterNPC mikelChar;

        public TestingStart()
        {

        }

        static void Main(string[] args)
        {
            Console.WriteLine("Testovaci konzole pro knihovnu AI.");
            Logger.InitLogger();
            Logger.bWriteToOutput = true;
            dynamic = new DynamicProgramming();
            mikelChar = new CharacterNPC("Settings/Mikel.xml");
            TestDP();

            Console.WriteLine("Stisknete libovolnou klavesu pro ukonceni... ");
            string end = Console.ReadLine();
            Logger.Save();
        }

        private static void TestDP()
        {
            Status status = new Status(mikelChar.hp, mikelChar.mana, 100, true, new System.Drawing.Point(0, 0), 1, 800);
            dynamic.CreateStateSpace(new ConflictState(status, status), mikelChar, mikelChar, ActionType.Attack);

            dynamic.Iterate(new ConflictState(status, status));
        }
    }
}
