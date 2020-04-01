using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class GameOver : MenusParent
    {
        Device device;

        public GameOver(Device device)
            : base(device)
        {
            this.device = device;
            device.DeviceReset += new EventHandler(device_DeviceReset);
            device_DeviceReset(null, null);
        }

        void device_DeviceReset(object sender, EventArgs e)
        {
            this.elements.Clear();

            MenuBackGround gameover = new MenuBackGround(this.device);
            gameover.Texture = TextureLoader.FromFile(device, @"Resources/gameover.dds");

            MenuButton restartButton = new MenuButton(device, MenuResult.Restart, "   Restart", new System.Drawing.Point(device.Viewport.Width - 350, device.Viewport.Height - 160));
            MenuButton exitButton = new MenuButton(device, MenuResult.Exit, "     Exit", new System.Drawing.Point(device.Viewport.Width - 350, device.Viewport.Height - 90));

            this.elements.Add(gameover);
            this.elements.Add(restartButton);
            this.elements.Add(exitButton);
        }
    }
}
