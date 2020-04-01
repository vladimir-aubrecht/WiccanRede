using System;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using System.Drawing;
using d3d = Microsoft.DirectX.Direct3D;
using System.Collections.Generic;

namespace WiccanRede.Graphics
{
    public class Console //: Logging.IConsole
    {
        private static Device dev = null;
        private static d3d.Font f = null;
        private static Sprite console = null;
        private static Sprite window = null;
        private static Texture background = null;
        private static Rectangle srcRectangle = new Rectangle(0, 0, 1024, 298);
        private static SizeF size = new SizeF(1024, 298);
        private static int fontsize = 13;
        private static int xmargin = 15;
        private static int ymargin = 15;
        private static Color defaultWindowColor = Color.Black;

        private static List<string> consoleTexts = new List<string>();
        private static List<string> consoleCommands = new List<string>();
        private static String command = String.Empty;

        private static bool showConsole = false;

        /// <summary>
        /// Postara se o uvolneni alokovane pameti
        /// </summary>
        public static void Dispose()
        {
            if (!f.Disposed)
                f.Dispose();

            if (!background.Disposed)
                background.Dispose();

            if (!window.Disposed)
                window.Dispose();

            if (!console.Disposed)
                console.Dispose();

        }

        /// <summary>
        /// Nastavi objekt zarizeni, ktere se bude pouzivat pro vykreslovani
        /// </summary>
        /// <param name="device">Objekt zarizeni</param>
        public static void ConsoleSetDevice(Device device)
        {
            Console.dev = device;
            console = new Sprite(dev);
            window = new Sprite(dev);
            background = TextureLoader.FromFile(dev, @"Resources/Textures/consolebackground.dds");

            f = new d3d.Font(dev, new System.Drawing.Font(System.Drawing.FontFamily.GenericSerif, fontsize));
            size = new SizeF(dev.PresentationParameters.BackBufferWidth, size.Height);
        }

        /// <summary>
        /// Zjisti, zda je mozno pouzivat konzoli
        /// </summary>
        /// <returns>Vraci true, pokud je mozno pouzivat konzoli</returns>
        public static bool ConsoleCanBeUsed()
        {
            return (dev != null && !f.Disposed) ? true : false;
        }

        /// <summary>
        /// Urcuje, zda ma byt konzole videt, nebo ne
        /// </summary>
        /// <param name="show">Parametr bude nastavenej na true, pokud ma byt konzole videt, jinak false</param>
        public static void ConsoleShow(bool show)
        {
            Console.showConsole = show;
        }

        /// <summary>
        /// Ukaze konzoli
        /// </summary>
        public static void ConsoleShow()
        {
            ConsoleShow(true);
        }

        /// <summary>
        /// Zjisti, zda je konzole videt
        /// </summary>
        /// <returns>Vraci true, pokud je konzole videt, jinak false</returns>
        public static bool ConsoleIsShowed()
        {
            return showConsole;
        }

        /// <summary>
        /// Zapise do konzole text
        /// </summary>
        /// <param name="text">Text, ktery ma byt vypsan konzoli</param>
        public static void ConsoleWriteLine(String text)
        {
            consoleTexts.Add(text);

            if (consoleTexts.Count > 13)
                consoleTexts.RemoveAt(0);

        }

        /// <summary>
        /// Zapise znak, popr. skupinu znaku na prikazovou radku konzole
        /// </summary>
        /// <param name="command">Retezec, ktery se ma pripsat na prikazovou radku konzole</param>
        public static void ConsoleWriteCharOnCommandLine(String command)
        {
            Console.command += command;
        }

        /// <summary>
        /// Odstrani posledni znak z prikazove konzole
        /// </summary>
        public static void ConsoleRemoveLastCharOnCommandLine()
        {
            if (Console.command.Length > 0)
                Console.command = Console.command.Remove(Console.command.Length - 1);
        }

        /// <summary>
        /// Odesle prikaz z prikazovy radky ke zpracovani
        /// </summary>
        public static void ConsoleSendCommandFromBuffer()
        {
            if (Console.command != String.Empty)
            {
                Console.ConsoleWriteLine("> " + Console.command);
                Console.consoleCommands.Add(Console.command);
                Console.command = String.Empty;
            }
        }

        /// <summary>
        /// Vrati seznam prikazu, ktere se maji provest
        /// </summary>
        /// <returns>Vrati seznam prikazu, ktere se maji provest</returns>
        public static List<String> ConsoleGetCommands()
        {
            return consoleCommands;
        }

        #region WindowWriteLine

        /// <summary>
        /// Nastavi horizontalni okraj pro vypisovani do okna
        /// </summary>
        /// <param name="xmargin">Velikost horizontalniho okraje</param>
        public static void SetWindowXMargin(int xmargin)
        {
            Console.xmargin = xmargin;
        }

        /// <summary>
        /// Nastavi vertikalni okraj pro vypisovani do okna
        /// </summary>
        /// <param name="ymargin">Velikost vertikalniho okraje</param>
        public static void SetWindowYMargin(int ymargin)
        {
            Console.ymargin = ymargin;
        }

        /// <summary>
        /// Vrati horizontalni okraj
        /// </summary>
        /// <returns>Vrati horizontalni okraj</returns>
        public static int GetWindowXMargin()
        {
            return xmargin;
        }

        /// <summary>
        /// Vrati vertikalni okraj
        /// </summary>
        /// <returns>Vrati vertikalni okraj</returns>
        public static int GetWindowYMargin()
        {
            return ymargin;
        }

        /// <summary>
        /// Nastavi defaultni barvu textu
        /// </summary>
        /// <param name="color">Barva textu</param>
        public static void SetDefaultWindowColor(Color color)
        {
            defaultWindowColor = color;
        }

        /// <summary>
        /// Vrati defaultni barvu textu
        /// </summary>
        /// <returns>Vrati defaultni barvu textu</returns>
        public static Color GetDefaultWindowColor()
        {
            return defaultWindowColor;
        }

        public static void WindowWriteLine(String message, System.Drawing.Point position, System.Drawing.Color color)
        {
            WindowWriteLine(message, position.X, position.Y, color);
        }

        public static void WindowWriteLine(String message, int x, int y, System.Drawing.Color color)
        {
            window.Begin(SpriteFlags.AlphaBlend);
            f.DrawText(window, message, x, y, color);
            window.End();
        }

        public static void WindowWriteLine(String message, int x, int y)
        {
            WindowWriteLine(message, x, y, defaultWindowColor);
        }

        public static void WindowWriteLine(String message)
        {
            WindowWriteLine(message, xmargin, ymargin);
        }

        public static void WindowWriteLine(String message, int y)
        {
            WindowWriteLine(message, xmargin, y);
        }

        public static void WindowWriteLine(String message, int y, Color color)
        {
            WindowWriteLine(message, xmargin, y, color);
        }

        public static void WindowWriteLine(int message, int x, int y)
        {
            WindowWriteLine(message.ToString(), x, y);
        }

        public static void WindowWriteLine(float message, int x, int y)
        {
            WindowWriteLine(message.ToString(), x, y);
        }

        public static void WindowWriteLine(Vector3 message, int x, int y)
        {
            WindowWriteLine(message.ToString(), x, y);
        }
        
        #endregion

        /// <summary>
        /// Funkce zajisti vykreslovani konzole
        /// </summary>
        public static void RenderConsole()
        {
            console.Begin(SpriteFlags.AlphaBlend);
            console.Draw2D(background, srcRectangle, size, new System.Drawing.PointF(), Color.FromArgb(170, 255, 255, 255));

            for (int t = 0; t < consoleTexts.Count; t++)
            {
                f.DrawText(console, consoleTexts[t], 15, t * (int)(3f / 2f * (float)fontsize) + 8, Color.White);
            }

            f.DrawText(console, "> " + command + "_", 15, (int)(size.Height - 3f / 2f * (float)fontsize), Color.White);

            console.End();
        }
    }

}
