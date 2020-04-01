using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class MainMenu : MenusParent
    {
        Device device;

        public MainMenu(Device device)
            : base(device)
        {
            this.device = device;
            device.DeviceReset += new EventHandler(device_DeviceReset);
            device_DeviceReset(null, null);
        }

        void device_DeviceReset(object sender, EventArgs e)
        {

            ControlElement temp = this.elements[0];
            this.elements.Clear();
            this.elements.Add(temp);

            MenuButton newButton = new MenuButton(device, MenuResult.New, "     Hrát", new System.Drawing.Point(0, 0));
            MenuButton exitButton = new MenuButton(device, MenuResult.Exit, "     Exit", new System.Drawing.Point(0, 0));

            newButton.Position = new System.Drawing.Point((device.Viewport.Width / 2) - newButton.Size.Width / 2, (device.Viewport.Height / 2) - 50);
            exitButton.Position = new System.Drawing.Point((device.Viewport.Width / 2) - newButton.Size.Width / 2, (device.Viewport.Height / 2) + 50);


            this.elements.Add(newButton);
            this.elements.Add(exitButton);
        }
    }
}
