using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class Pub : MenusParent
    {
        Device device;

        public Pub(Device device)
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
            tobecontinue.Texture = TextureLoader.FromFile(device, @"Resources/Pub.dds");

            MenuButton exitButton = new MenuButton(device, MenuResult.Pub, "Pokračovat", new System.Drawing.Point(device.Viewport.Width - 190, device.Viewport.Height - 90));

            this.elements.Add(tobecontinue);
            this.elements.Add(exitButton);
        }
    }
}
