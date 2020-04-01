using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class ToBeContinue : MenusParent
    {
        Device device;

        public ToBeContinue(Device device)
            : base(device)
        {
            this.device = device;
            device.DeviceReset += new EventHandler(device_DeviceReset);
            device_DeviceReset(null, null);
        }

        void device_DeviceReset(object sender, EventArgs e)
        {
            this.elements.Clear();

            MenuBackGround tobecontinue = new MenuBackGround(this.device);
            tobecontinue.Texture = TextureLoader.FromFile(device, @"Resources/ToBeContinue.dds");

            MenuButton exitButton = new MenuButton(device, MenuResult.Exit, "     Exit", new System.Drawing.Point(device.Viewport.Width - 190, device.Viewport.Height - 90));

            this.elements.Add(tobecontinue);
            this.elements.Add(exitButton);
        }
    }
}
