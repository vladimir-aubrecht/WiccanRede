using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;


using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede.Graphics
{
    /// <summary>
    /// Trida slouzici k inicializaci Direct3D zarizeni
    /// </summary>
    public class Initializator : IDisposable
    {
        int width = 800;
        int height = 600;
        PresentParameters presentParameters;
        Form form;
        Device device;

        int failureRestoreCount = 0;
        bool deviceLost = false;

        /// <summary>
        /// Uklidi objekty initializatoru
        /// </summary>
        public void Dispose()
        {
            if (device != null && !device.Disposed)
                device.Dispose();
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="form">Okno, do ktereho se provede inicializace Direct3D zarizeni</param>
        public Initializator(Form form)
        {
            this.form = form;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MaximizeBox = false;

            presentParameters = new PresentParameters();
            presentParameters.DeviceWindowHandle = form.Handle;
            presentParameters.ForceNoMultiThreadedFlag = false;
            presentParameters.FullScreenRefreshRateInHz = 0;

            SetBackBuffer(Manager.Adapters[0].CurrentDisplayMode.Format, 1);
            SetResolution(false, 800, 600);
            SetDepthStencil(true);
            SetVSync(false);
            SetMultiSampleQuality(0, MultiSampleType.None);

            Init();
        }

        /// <summary>
        /// Provede inicializaci
        /// </summary>
        private void Init()
        {
           int defaultAdapter = 0;
           DeviceType type = DeviceType.Hardware;

           for (int t = 0; t < Manager.Adapters.Count; t++)
           {
               AdapterDetails adapterInformation = Manager.Adapters[t].Information;

               if (adapterInformation.Description == "NVIDIA PerfHUD")
               {
                   type = DeviceType.Reference;
                   defaultAdapter = t;
                   break;
               }
           }
           device = new Device(defaultAdapter, type, form, CreateFlags.HardwareVertexProcessing, presentParameters);
        }

        /// <summary>
        /// Provede reinicializaci zarizeni s novym nastavenim
        /// </summary>
        public void ReInit()
        {
            ResetDevice();

            if (presentParameters.Windowed)
                form.Size = new System.Drawing.Size(presentParameters.BackBufferWidth, presentParameters.BackBufferHeight);

        }

        #region Present Parameters settings

        /// <summary>
        /// Provede nastaveni back bufferu
        /// </summary>
        /// <param name="format">Format back bufferu</param>
        /// <param name="count">Pocet back bufferu</param>
        public void SetBackBuffer(Format format, int count)
        {
            presentParameters.BackBufferFormat = format;
            presentParameters.BackBufferCount = count;
        }

        /// <summary>
        /// Provede nastaveni rozliseni obrazovky
        /// </summary>
        /// <param name="fullscreen">Ma se aplikace spustit do fullscreenu?</param>
        /// <param name="width">Sirka</param>
        /// <param name="height">Vyska</param>
        public void SetResolution(bool fullscreen, int width, int height)
        {
            presentParameters.Windowed = !fullscreen;
            this.width = width;
            this.height = height;
            presentParameters.BackBufferWidth = this.width;
            presentParameters.BackBufferHeight = this.height;
        }

        /// <summary>
        /// Metoda vrati aktualne nastavene rozliseni
        /// </summary>
        /// <returns>Metoda vrati aktualne nastavene rozliseni</returns>
        public Vector2 GetResolution()
        {
            return new Vector2(this.width, this.height);
        }

        /// <summary>
        /// Povoleni/zakazani zbufferu a stencil bufferu
        /// </summary>
        /// <param name="enable">True povoli zbuffer, false ho zakaze</param>
        public void SetDepthStencil(bool enable)
        {
            presentParameters.EnableAutoDepthStencil = enable;

            if (Manager.CheckDepthStencilMatch(0, DeviceType.Hardware, presentParameters.BackBufferFormat, Format.X8R8G8B8, DepthFormat.D32))
                presentParameters.AutoDepthStencilFormat = DepthFormat.D32;
            else if (Manager.CheckDepthStencilMatch(0, DeviceType.Hardware, presentParameters.BackBufferFormat, Format.X8R8G8B8, DepthFormat.D24S8))
                presentParameters.AutoDepthStencilFormat = DepthFormat.D24S8;
            else if (Manager.CheckDepthStencilMatch(0, DeviceType.Hardware, presentParameters.BackBufferFormat, Format.X8R8G8B8, DepthFormat.D16))
                presentParameters.AutoDepthStencilFormat = DepthFormat.D16;
            else throw new Exception("Graficka karta nepodporuje ani 16b ZBuffer, neni mozne pokracovat.");
        }

        /// <summary>
        /// Povoli/zakaze vertikalni synchronizaci
        /// </summary>
        /// <param name="enable">True povoli vertikalni synchronizaci, false ji zakaze</param>
        public void SetVSync(bool enable)
        {
            if (enable)
            {
                presentParameters.PresentationInterval = PresentInterval.Default;
                presentParameters.SwapEffect = SwapEffect.Copy;
            }
            else
            {
                presentParameters.PresentationInterval = PresentInterval.Immediate;
                presentParameters.SwapEffect = SwapEffect.Discard;
            }
        }

        /// <summary>
        /// Nastavi uroven kvality multisamplingu
        /// </summary>
        /// <param name="qualityLevel">Uroven kvality multisamplingu</param>
        /// <param name="type">Typ multisamplingu</param>
        public void SetMultiSampleQuality(int qualityLevel, MultiSampleType type)
        {
            presentParameters.MultiSampleQuality = qualityLevel;
            presentParameters.MultiSample = type;
        }

        #endregion

        /// <summary>
        /// Vrati objekt zarizeni
        /// </summary>
        /// <returns>Vrati objekt zarizeni</returns>
        public Device GetDevice()
        {
            return device;
        }

        /// <summary>
        /// Metoda zjisti, jestli je zapnut fullscreen a pokud ano, prepne do okna, pokud je aplikace v okenim rezimu, tak naopak prepne do fullscreenu
        /// </summary>
        /// <param name="width">Sirka</param>
        /// <param name="height">Vyska</param>
        public void ToggleFullscreen(int width, int height)
        {
            SetResolution(presentParameters.Windowed, width, height);
            ReInit();
        }

        /// <summary>
        /// Vrati, zda je device prepnuty do fullscreen modu
        /// </summary>
        /// <returns>Vrati true, pokud je device prepnuty do fullscreen modu, jinak false</returns>
        public bool IsFullscreen()
        {
            return !presentParameters.Windowed;
        }

        /// <summary>
        /// Zjisti, zda nastala ztrata zarizeni
        /// </summary>
        /// <returns>Vrati true, pokud nastala ztrata zarizeni, jinak false</returns>
        public bool IsDeviceLost()
        {
            return deviceLost;
        }

        /// <summary>
        /// Provede nezbytne testy, zda se muze zacit renderovat, popr. resetne zarizeni (napr. pri device lostu)
        /// </summary>
        /// <returns>Vrati true, pokud je mozne zahajit rendering, jinak false</returns>
        public bool BeginRender()
        {
            if (deviceLost)
            {
                try
                {
                    device.TestCooperativeLevel();
                    return true;
                }
                catch (DeviceNotResetException)
                {
                    return ResetDevice();
                }
                catch
                {
                    return false;
                }

            }

            return true;
        }

        /// <summary>
        /// Metoda resetne zarizeni
        /// </summary>
        /// <returns>Vraci true, pokud se reset zdaril, jinak false</returns>
        private bool ResetDevice()
        {

            try
            {
                if (!presentParameters.Windowed && System.Environment.OSVersion.Version.Major >= 6)
                {
                    form.FormBorderStyle = FormBorderStyle.None;
                    form.ClientSize = new System.Drawing.Size(width, height);
                }

                SetResolution(!presentParameters.Windowed, this.width, this.height);
                device.Reset(presentParameters);

                if (presentParameters.Windowed)
                {
                    form.FormBorderStyle = FormBorderStyle.FixedSingle;
                    form.ClientSize = new System.Drawing.Size(width, height);
                }

                deviceLost = false;
                failureRestoreCount = 0;
                return true;
            }
            catch (DeviceLostException)
            {
                failureRestoreCount++;

                if (failureRestoreCount > 3)
                    throw new Exception("Nezdarilo se obnovit zarizeni");

                deviceLost = true;
                return false;
            }

        }

        /// <summary>
        /// Metoda zkontroluje ztratu zarizeni a v pripade, ze je to mozne, tak provede prohozeni back bufferu a front bufferu
        /// </summary>
        public void EndRender()
        {
            try
            {
                device.Present();
            }
            catch
            {
                deviceLost = true;
            }
        }
    }
}
