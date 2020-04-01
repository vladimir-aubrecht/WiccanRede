using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    public class MenuManager
    {
        MenusParent currentMenu;
        MenusParent previusMenu;
        MainMenu main;
        OptionsMenu options;
        CreditsMenu credits;
        GameOver gameover;
        ToBeContinue tobecontinue;
        Pub pub;

        MenuResult menuState;

        public MenuResult MenuState
        {
            get { return menuState; }
            set { menuState = value; }
        }

        public MenuResult CurrentMenuResult
        {
            get { return currentMenu.Result; }
            set { currentMenu.Result = value; }
        }
        public MenuManager(Device device)
        {
            this.main = new MainMenu(device);
            this.options = new OptionsMenu(device);
            this.credits = new CreditsMenu(device);
            this.gameover = new GameOver(device);
            this.tobecontinue = new ToBeContinue(device);
            this.pub = new Pub(device);

            this.currentMenu = main;
            this.previusMenu = main;
        }

        public MenuResult Update(System.Drawing.Point mousePosition, bool clicked)
        {
            WiccanRede.MenuResult result = this.currentMenu.Update(mousePosition, clicked);
            switch (result)
            {
                case MenuResult.New:
                    this.currentMenu = this.pub;
                    this.previusMenu = this.main;
                    break;
                case MenuResult.Restart:
                    this.currentMenu = this.main;
                    this.previusMenu = this.main;
                    break;
                case MenuResult.OK:
                    this.currentMenu = previusMenu;
                    break;
                case MenuResult.Cancel:
                    this.currentMenu = previusMenu;
                    break;
                case MenuResult.Options:
                    this.previusMenu = this.currentMenu;
                    this.currentMenu = options;
                    break;
                case MenuResult.Credits:
                    this.previusMenu = this.currentMenu;
                    this.currentMenu = credits;
                    break;
                case MenuResult.Multiplayer:
                    break;
                case MenuResult.Exit:
                    break;
                case MenuResult.None:
                    break;
                case MenuResult.Gameover:
                    this.currentMenu = this.gameover;
                    this.previusMenu = this.main;
                    break;

                case MenuResult.ToBeContinued:
                    this.currentMenu = this.tobecontinue;
                    this.previusMenu = this.main;
                    break;

                case MenuResult.Pub:
                    this.currentMenu = this.pub;
                    this.previusMenu = this.main;
                    break;

                case MenuResult.MainMenu:
                    this.currentMenu = this.main;
                    this.previusMenu = this.main;
                    break;
                default:
                    break;
            }
            this.menuState = result;
            return result;
        }


        public MenuResult UpdateState(string screen)
        {
            MenuResult result;
            switch (screen)
            {
                case "Pub":
                    result = MenuResult.Pub;
                    break;
                case "Title":
                    result = MenuResult.Title;
                    break;
                case "ToBeContinued":
                    result = MenuResult.ToBeContinued;
                    this.currentMenu = this.tobecontinue;
                    this.previusMenu = this.main;
                    break;
                case "Gameover":
                    result = MenuResult.Gameover;
                    this.currentMenu = this.gameover;
                    this.previusMenu = this.main;
                    break;
                case "MainMenu":
                    result = MenuResult.MainMenu;
                    this.currentMenu = this.main;
                    this.previusMenu = this.main;
                    break;
                case "Restart":
                    result = MenuResult.Restart;
                    this.currentMenu = this.pub;
                    this.previusMenu = this.main;
                    break;

                default:
                    result = MenuResult.None;
                    break;
            }
            this.menuState = result;
            return result;
        }
    }
}
