using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI
{
    /// <summary>
    /// struct representing one field of the grid
    /// </summary>
    struct Field
    {
        public Point position;
        public bool blocked;
        public bool ship;
        public Color color;
    }

    public class Grid : WiccanRede.AI.IWalkable
    {

        Size size;
        List<Point> blockedPosition;
        Field[,] fields;
        Bitmap bmp;
        Bitmap final;
        WiccanRede.AI.Map map;

        int fieldWidth;

        /// <summary>
        /// width of one field in the grid
        /// </summary>
        public int FieldWidth
        {
            get { return fieldWidth; }
        }
        int fieldHeight;

        float maxH;
        float[,] h;


        /// <summary>
        /// height of one field in the grid
        /// </summary>
        public int FieldHeight
        {
            get { return fieldHeight; }
        }

        /// <summary>
        /// #ctor
        /// </summary>
        /// <param name="bmp">Bitmap with map data</param>
        public Grid(Bitmap bmp)
        {
            this.size = new Size(bmp.Width, bmp.Height);
            this.fields = new Field[size.Width, size.Height];
            this.fieldWidth = 1;//gridPanel.Width / size.Width;
            this.fieldHeight = 1;//gridPanel.Height / size.Height;
            this.bmp = bmp;

            //load info from bitmap
            blockedPosition = new List<Point>();
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    this.fields[i, j] = new Field();
                    this.fields[i, j].position = new Point(i, j);
                    if (!(bmp.GetPixel(i, j).R > 250))
                    {
                        blockedPosition.Add(new Point(i, j));
                        fields[i, j].blocked = true;
                    }
                }
            }

            this.map = WiccanRede.AI.Map.GetInstance(this);
            h = new float[this.map.MapSize.X, this.map.MapSize.Y];
        }

        
        /// <summary>
        /// paint event, will draw the ship, the grid into the bitmap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns>Bitmap witch was used to draw on</returns>
        public Bitmap PaintGrid()
        {
            Graphics gBmp = Graphics.FromImage(this.final);
            gBmp.Clear(Color.White);

            Pen pen = Pens.Black;

            //draw grid
            for (int i = 0; i < this.size.Width; i++)
            {
                //gBmp.DrawLine(pen, new Point(i * this.fieldWidth, 0), new Point(i * this.fieldWidth, this.gridPanel.Bottom));
            }
            for (int i = 0; i < this.size.Height; i++)
            {
                //gBmp.DrawLine(pen, new Point(0, i * this.fieldHeight), new Point(this.gridPanel.Right, i * this.fieldHeight));
            }
            for (int i = 0; i < this.size.Width; i++)   //draw blocked positions
            {
                for (int j = 0; j < this.size.Height; j++)
                {
                    Rectangle rect = new Rectangle(i * this.fieldWidth, j * fieldHeight, this.fieldWidth, this.fieldHeight);
                    if (fields[i, j].blocked)
                    {
                        gBmp.FillRectangle(Brushes.Red, rect);
                    }
                    else if (maxH > 0 && h.GetLength(0) > 0)
                    {
                        int clr = (int)((h[i, j] * 255) / maxH);
                        fields[i, j].color = Color.FromArgb(0, clr, 0);
                        gBmp.DrawRectangle(new Pen(fields[i, j].color, 3), rect);
                    }

                }
            }

            //if (this.ship.Pause)
            //{
            //    PaintDebug(gBmp);
            //}
            //draw ship
            //this.ship.Draw(gBmp);
            return this.final;
        }

        //joining AI library
        #region IWalkable Members

        public bool IsPositionOnTereain(Microsoft.DirectX.Vector3 position)
        {
            PointF p = new PointF(position.X, position.Z);
            if (p.X < 0 || p.X > this.size.Width)
                return false;
            if (p.Y < 0 || p.Y > this.size.Height)
                return false;

            return true;
        }

        public Microsoft.DirectX.Vector3 GetPositionOnTerain(Microsoft.DirectX.Vector3 position)
        {
            return position;
        }

        public Microsoft.DirectX.Vector2 GetTerrainSize()
        {
            return new Microsoft.DirectX.Vector2(this.size.Width, this.size.Height);
        }

        public System.Drawing.Point Get2DMapPosition(Microsoft.DirectX.Vector3 position)
        {
            PointF p = new PointF(position.X, position.Z);
            int x = (int)(p.X / this.fieldWidth);
            int y = (int)(p.Y / this.fieldHeight);
            return new Point(x, y);
        }

        public Microsoft.DirectX.Vector3 Get3Dposition(System.Drawing.Point position2D)
        {
            Microsoft.DirectX.Vector3 v = new Microsoft.DirectX.Vector3();
            v.X = position2D.X * this.fieldWidth;
            v.Y = 1;
            v.Z = position2D.Y * this.fieldHeight;
            return v;
        }

        public System.Drawing.Point GetRelativePlayerPosition()
        {
            return new Point(0, 0);
        }

        public int[,] GetMap()
        {
            int[,] map = new int[this.size.Width, this.size.Height];
            for (int i = 0; i < this.size.Width; i++)
            {
                for (int j = 0; j < this.size.Height; j++)
                {
                    map[i, j] = 1;
                }
            }
            return map;
        }

        public System.Drawing.PointF GetSize()
        {
            PointF p = new PointF(this.size.Width, this.size.Height);
            return p;
        }

        public List<System.Drawing.Point> GetBlockedPositions()
        {
            return this.blockedPosition;
        }

        public bool IsPositionOnTerainBlocked(Microsoft.DirectX.Vector3 position)
        {
            if (this.blockedPosition.Contains(Get2DMapPosition(position)))
                return true;
            return false;
        }

        public Point GetPlayerPosition()
        {
            return new Point(0, 0);
        }

        #endregion
    }
}
