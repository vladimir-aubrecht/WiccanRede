using System;
using Microsoft.DirectX;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI
{
    public enum RatingType
    {
        Hiding, Explore, Direct, Carefull
    }

    /// <summary>
    /// struct to represent one map cell
    /// </summary>
    public struct MapCellInfo
    {
        int height;

        public int Height
        {
            get { return height; }
        }
        bool bPlayer;

        public bool Player
        {
            get { return bPlayer; }
            set { bPlayer = value; }
        }
        bool bFriendlyNPC;
        bool bDanger;

        public bool Danger
        {
            get { return bDanger; }
            set { bDanger = value; }
        }
        private bool bBlock; //kvuli vypisu

        public bool Block
        {
            get { return (bBlock || bDanger 
                || (Map.GetInstance().GetTerrain().GetPlayerPosition() == this.position)); }
        }
        System.Drawing.Point position;

        public System.Drawing.Point Position
        {
            get { return position; }
        }

        public MapCellInfo(int iHeight, bool bPlayer, bool bFriendlyNPC, bool bDanger, bool bBlock, System.Drawing.Point position)
        {
            this.height = iHeight;
            this.bPlayer = bPlayer;
            this.bFriendlyNPC = bFriendlyNPC;
            this.bDanger = bDanger;
            this.bBlock = bBlock || bDanger;
            this.position = position;
            this.bPartOfPath = false;
        }
        public override string ToString()
        {
            return ("cell info: vyska=" + this.height + ", hrac=" + this.bPlayer.ToString() + 
                ", NPC=" + this.bFriendlyNPC.ToString() + ", blocked=" + this.bBlock);
        }

        public bool bPartOfPath;    //pro vypis
    }

    /// <summary>
    /// class to represent 2D map, its singleton
    /// </summary>
    public class Map
    {
        private static Map instance;
        int[,] heightMap;
        Point size;
        System.Drawing.Bitmap bmp;
        int maxHeight;
        List<Point> npcsPosition;
        Point playerPosition;

        MapCellInfo[,] cellMap;

        /// <summary>
        /// 2D array of cells in map
        /// </summary>
        public MapCellInfo[,] CellMap
        {
            get { return cellMap; }
        }

        IWalkable terrain;

        /// <summary>
        /// gets the instance of Map, do not create new instance
        /// </summary>
        /// <seealso cref="GetInstance(IWalkable terrain)"/>
        /// <returns>instance of this map</returns>
        public static Map GetInstance()
        {
            return instance;
        }
        /// <summary>
        /// Get instance of map, if this is not initializes, calls ctor()
        /// </summary>
        /// <param name="terrain">terrain, its, needed to create new instance</param>
        /// <returns></returns>
        public static Map GetInstance(IWalkable terrain)
        {
            if (instance == null)
                instance = new Map(terrain);
            return instance;
        }

        /// <summary>
        /// private ctor - singleton
        /// </summary>
        /// <param name="terrain"></param>
        private Map(IWalkable terrain)
        {
            if (terrain == null)
                return;

            this.terrain = terrain;
            this.heightMap = terrain.GetMap();

            cellMap = new MapCellInfo[heightMap.GetLength(0), heightMap.GetLength(1)];
            size = new System.Drawing.Point(this.heightMap.GetLength(0), this.heightMap.GetLength(1));
            this.playerPosition = terrain.GetPlayerPosition();
            npcsPosition = new List<Point>();
            List<Point> blocked = terrain.GetBlockedPositions();
            Logging.Logger.AddInfo("Teren ma " + blocked.Count.ToString() + " blokovanych pozic");

            int x, y;
            x = cellMap.GetLength(0);
            y = cellMap.GetLength(1);
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    bool bPlayer = (terrain.GetPlayerPosition() == new System.Drawing.Point(i, j));
                    bool bFriendlyNPC = false;
                    bool bBlocked = false; //(terrain.GetBlockedPositions()[i] == new System.Drawing.Point(i, j));
                    foreach (System.Drawing.Point p in blocked)
                    {
                        if (p == new System.Drawing.Point(i, j))
                        {
                            bBlocked = true;
                            blocked.Remove(p);
                            break;
                        }
                    }
                    if (heightMap[i, j] > this.maxHeight)
                    {
                        this.maxHeight = heightMap[i, j];
                    }
                    cellMap[i, j] = new MapCellInfo(heightMap[i, j], bPlayer, bFriendlyNPC, false, bBlocked, new System.Drawing.Point(i, j));

                }
            }
            bmp = new Bitmap(this.MapSize.X, this.MapSize.Y);
            Logging.Logger.AddInfo("AI: Nactena mapa");
        }

        /// <summary>
        /// sumarize map around the position in range
        /// </summary>
        /// <param name="position">center position</param>
        /// <param name="range">range of cells around position</param>
        /// <returns>2D array of MapCellInfo around given position</returns>
        public MapCellInfo[,] getRelatedMap(System.Drawing.Point position, int range)
        {
            position.X -= range / 2;
            position.Y -= range / 2;

            if (position.X <= 0)
                position.X = 0;
            if (position.Y <= 0)
                position.Y = 0;

            MapCellInfo[,] newMap = new MapCellInfo[range, range];

            if (range + position.X >= heightMap.GetLength(0))
            {
                range = heightMap.GetLength(0) - (int)position.X;
            }
            if (range + position.Y >= heightMap.GetLength(1))
            {
                range = heightMap.GetLength(0) - (int)position.Y;
            }

            for (int i = (int)position.X; i < position.X + range; i++)
            {
                for (int j = (int)position.Y; j < position.Y + range; j++)
                {
                    newMap[i - (int)position.X, j - (int)position.Y] = this.cellMap[i, j];
                }
            }

            return newMap;
        }
        /// <summary>
        /// updates positions of all npcs and player
        /// </summary>
        /// <param name="npcs"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Update(List<NPC> npcs)
        {
            //clear cellmap
            foreach (Point pos in this.npcsPosition)
            {
                this.cellMap[pos.X, pos.Y].Danger = false;
            }
            npcsPosition.Clear();
            
            //set
            if (terrain.GetPlayerPosition() != this.playerPosition)
            {
                this.cellMap[playerPosition.X, playerPosition.Y].Player = false;
                this.playerPosition = terrain.GetPlayerPosition();
                this.cellMap[playerPosition.X, playerPosition.Y].Player = true;
            }

            foreach (NPC npc in npcs)
            {
                this.npcsPosition.Add(npc.GetPosition2D());
                this.cellMap[npc.GetMapX(), npc.GetMapY()].Danger = true;
            }

            #region bitmap
//            bmp = new System.Drawing.Bitmap(this.MapSize.X, this.MapSize.Y);
//#if DEBUG
//            for (int i = 0; i < this.MapSize.X; i++)
//            {
//                for (int j = 0; j < this.MapSize.Y; j++)
//                {
//                    if (this.cellMap[i, j].bBlock)
//                    {
//                        bmp.SetPixel(i, j, System.Drawing.Color.Red);
//                    }
//                    else if (this.cellMap[i, j].bPartOfPath)
//                    {
//                        bmp.SetPixel(i, j, System.Drawing.Color.Green);
//                    }
//                    else if (this.cellMap[i, j].Player)
//                    {
//                        bmp.SetPixel(i, j, System.Drawing.Color.Black);
//                    }
//                    else
//                    {
//                        int koef = this.cellMap[i, j].Height / this.maxHeight;
//                        koef *= -100;
//                        koef = 255 + koef;
//                        System.Drawing.Color heightColor = Color.FromArgb(koef, koef, koef);
//                        bmp.SetPixel(i, j, heightColor);
//                    }
//                    //this.cellMap[i, j].bPartOfPath = false;
//                }
//            }
//#endif
            #endregion
        }

        /// <summary>
        /// gets the map size
        /// </summary>
        public System.Drawing.Point MapSize
        {
            get
            {
                return this.size;
            }
        }
        /// <summary>
        /// generate the bitmap with blocked positions, npcs position and paths, for debug purposes
        /// </summary>
        /// <returns>generated bitmap</returns>
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public System.Drawing.Bitmap GetBitmap()
        {
            for (int i = 0; i < this.MapSize.X; i++)
            {
                for (int j = 0; j < this.MapSize.Y; j++)
                {
                    this.cellMap[i, j].bPartOfPath = false;
                    this.cellMap[i, j].Player = false;
                    if (terrain.GetPlayerPosition() == new Point(i, j))
                    {
                        this.cellMap[i, j].Player = true;
                    }
                }
            }
            return this.bmp;
        }

        /// <summary>
        /// gets rating of transfer between two points
        /// </summary>
        /// <param name="type">rating type</param>
        /// <param name="from">first point</param>
        /// <param name="to">second point</param>
        /// <returns>cost of transfer</returns>
        public float GetRating(RatingType type, Point from, Point to)
        {
            int rating = 1;
            switch (type)
            {
                case RatingType.Hiding:
                    //rating /= (height / 10);
                    //if (bDanger)
                    //    rating /= 10;
                    //if (bPlayer)
                    //    rating /= 10;
                    //if (bBlock)
                    //    rating = 999999;
                    break;
                case RatingType.Explore:
                    //rating += height / 10;
                    break;
                case RatingType.Direct:
                    break;
                case RatingType.Carefull:
                    break;
                default:
                    break;
            }
            //rating += 2;
            return rating;
        }

        /// <summary>
        /// gets the terrain
        /// </summary>
        /// <returns></returns>
        public IWalkable GetTerrain()
        {
            return this.terrain;
        }

        public override string ToString()
        {
            string path = "Naznaceni spocitane cesty: \n";

            for (int i = 0; i < this.MapSize.X; i++)
            {
                for (int j = 0; j < this.MapSize.Y; j++)
                {
                    path += (this.CellMap[i, j].bPartOfPath) ? "X" : "_";
                    this.CellMap[i, j].bPartOfPath = false;
                    //    if (this.cellMap[i, j].bBlock)
                    //    {
                    //        path += "B";
                    //        this.CellMap[i, j].bBlock = false;
                    //    }
                    //    else if (this.cellMap[i, j].bPartOfPath)
                    //    {
                    //        path += "X";
                    //        this.CellMap[i, j].bPartOfPath = false;
                    //    }
                    //    else
                    //    {
                    //        path += "-";
                    //    }
                }
                path += "\n";
            }
            return path;
        }
    }
}
