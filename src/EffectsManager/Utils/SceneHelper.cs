using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede.Graphics.Utils
{
    /// <summary>
    /// Trida pro praci s rastrem
    /// </summary>
    public static class BitmapOperation
    {
        /// <summary>
        /// Metoda nacte pole barev z bitmapy
        /// </summary>
        /// <param name="bmp">Bitmapa, ze ktere se budou cist data</param>
        /// <returns>Pole barev</returns>
        public unsafe static Color[,] BitmapToColorArray(Bitmap bmp)
        {
            Color[,] img = new Color[bmp.Width, bmp.Height];
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < data.Height; y++)
            {
                // vypocte ukazatel na zacatek y-teho radku
                int* retPos = (int*)((int)data.Scan0 + (y * data.Stride));

                int x = 0;
                while (x < data.Width)
                {
                    // vyplni pixel nahodnou barvou
                    img[x, y] = Color.FromArgb(*retPos);

                    // posun na dalsi pixel
                    retPos++; x++;
                }
            }
            bmp.UnlockBits(data);

            return img;
        }

        /// <summary>
        /// Metoda vytvori Bitmapu z pole barev
        /// </summary>
        /// <param name="pixels">Zadane pole barev</param>
        /// <returns>Vytvorena bitmapa</returns>
        public unsafe static Bitmap ColorArrayToBitmap(Color[,] pixels)
        {
            Bitmap bmp = new Bitmap(pixels.GetLength(0), pixels.GetLength(1));
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < data.Height; y++)
            {
                // vypocte ukazatel na zacatek y-teho radku
                int* retPos = (int*)((int)data.Scan0 + (y * data.Stride));

                int x = 0;
                while (x < data.Width)
                {
                    // vyplni pixel nahodnou barvou
                    *retPos = pixels[x, y].ToArgb();

                    // posun na dalsi pixel
                    retPos++; x++;
                }
            }
            bmp.UnlockBits(data);

            return bmp;
        }


        public static Color[,] Blur(Color[,] img)
        {
            Color[,] nMatrix = new Color[img.GetLength(0), img.GetLength(1)];

            for (int i = 0; i < nMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < nMatrix.GetLength(1); j++)
                {
                    Color i1j1 = (j + 1 < img.GetLength(1) && (i + 1) < img.GetLength(0)) ? img[i + 1, j + 1] : Color.FromArgb(0, 0, 0);
                    Color ij1 = (j + 1 < img.GetLength(1)) ? img[i, j + 1] : Color.FromArgb(0, 0, 0);                    
                    Color i_1j1 = (j + 1 < img.GetLength(1) && (i - 1) > 0) ? img[i - 1, j + 1] : Color.FromArgb(0, 0, 0);

                    Color i1j = (i + 1 < img.GetLength(0)) ? img[i + 1, j] : Color.FromArgb(0, 0, 0);
                    Color ij = img[i, j];
                    Color i_1j = ((i - 1) > 0) ? img[i - 1, j] : Color.FromArgb(0, 0, 0);

                    Color i1j_1 = ((j - 1) > 0 && (i + 1) < img.GetLength(0)) ? img[i + 1, j - 1] : Color.FromArgb(0, 0, 0);
                    Color ij_1 = (j - 1 > 0) ? img[i, j - 1] : Color.FromArgb(0, 0, 0);
                    Color i_1j_1 = (j - 1 > 0 && (i - 1) > 0) ? img[i - 1, j - 1] : Color.FromArgb(0, 0, 0);



                    int blurR = i1j1.R + ij1.R + i_1j1.R + i1j.R + ij.R + i_1j.R + i1j_1.R + ij_1.R + i_1j_1.R;
                    int blurG = i1j1.G + ij1.G + i_1j1.G + i1j.G + ij.G + i_1j.G + i1j_1.G + ij_1.G + i_1j_1.G;
                    int blurB = i1j1.B + ij1.B + i_1j1.B + i1j.B + ij.B + i_1j.B + i1j_1.B + ij_1.B + i_1j_1.B;

                    blurR /= 9;
                    blurG /= 9;
                    blurB /= 9;

                    nMatrix[i, j] = Color.FromArgb(blurR, blurG, blurB);
                }
            }


            return nMatrix;
        }

        /// <summary>
        /// Metoda vrati barevne pole pixelu v texture
        /// </summary>
        /// <param name="texture">Textura, ze ktere se ctou data</param>
        /// <returns>Pole Coloru</returns>
        public static Color[,] TextureToColorArray(Texture texture)
        {
            return BitmapToColorArray(TextureToBitmap(texture));
        }

        /// <summary>
        /// Metoda vrati bitmapu vytvorenou z predane textury
        /// </summary>
        /// <param name="texture">Textura, ze ktery se bude vytvaret bitmapa</param>
        /// <returns>Vracena bitmapa</returns>
        public static Bitmap TextureToBitmap(Texture texture)
        {
            return (Bitmap)Bitmap.FromStream(TextureLoader.SaveToStream(ImageFileFormat.Bmp, texture));
        }
    }

    /// <summary>
    /// Trida zaobalujici rendering do textury
    /// </summary>
    public class RenderToTexture : IDisposable
    {

        #region Fields
        Device device;
        int width;
        int height;
        Format format;
        bool depthStencil;
        DepthFormat depthFormat;

        RenderToSurface renderToSurface;
        Texture texture;
        Surface surface;
        #endregion

        /// <summary>
        /// Uvolni objekty
        /// </summary>
        public void Dispose()
        {
            if (renderToSurface != null && !renderToSurface.Disposed)
                renderToSurface.Dispose();

            if (surface != null && !surface.Disposed)
                surface.Dispose();

            if (texture != null && !texture.Disposed)
                texture.Dispose();

        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="device">Zarizeni, pres ktere se bude renderovat</param>
        /// <param name="width">Sirka textury</param>
        /// <param name="height">Vyska textury</param>
        /// <param name="format">Format textury</param>
        /// <param name="depthStencil">Ma se pouzivat depth/stencil buffer?</param>
        /// <param name="depthFormat">Format depth/stencil bufferu</param>
        public RenderToTexture(Device device, int width, int height, Format format, bool depthStencil, DepthFormat depthFormat)
        {
            this.device = device;
            this.width = width;
            this.height = height;
            this.format = format;
            this.depthStencil = depthStencil;
            this.depthFormat = depthFormat;

            ReInit();
        }

        /// <summary>
        /// Provede reinicializaci vsech vnitrnich objektu
        /// </summary>
        public void ReInit()
        {
            Dispose();

            renderToSurface = new RenderToSurface(device, width, height, format, depthStencil, depthFormat);
            texture = new Texture(device, width, height, 1, Usage.RenderTarget, format, Pool.Default);
            surface = texture.GetSurfaceLevel(0);
        }

        /// <summary>
        /// Zahajeni renderingu do textury
        /// </summary>
        public void BeginScene()
        {
            renderToSurface.BeginScene(surface);
        }

        /// <summary>
        /// Ukonceni renderingu do textury
        /// </summary>
        /// <param name="filter">Nastavi filtrovaci mod, ktery se pouzije pro vyrenderovanou texturu</param>
        public void EndScene(Filter filter)
        {
            renderToSurface.EndScene(filter);
        }

        /// <summary>
        /// Vrati vyrenderovanou texturu
        /// </summary>
        /// <returns></returns>
        public Texture GetTexture()
        {
            return texture;
        }
    }
}
