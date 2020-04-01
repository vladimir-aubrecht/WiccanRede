using System;
using Logging;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI
{
    public class Node
    {
        public System.Drawing.Point position;
        public float g, h;
        public float f;

        public MapCellInfo cellInfo;

        public Node parent;
#if DEBUG
        public List<Node> opens;
        public List<Node> closeds;
#endif

        public int Index
        {
            get
            {
                return position.Y * Map.GetInstance().MapSize.X + position.X;
            }
        }

        public override string ToString()
        {
            string str;
            str = "Node: ";
            str += "g = " + g + "; h = " + h + "; f = " + f + "\n";
            str += position.ToString();
            return str;
        }
    }

    public class Astar : IDisposable
    {
        Map relatedMap;
        //TODO vytvorit prioritni frontu pro open list
        List<Node> open;
        List<Node> closed;
        Node[] closedList;

        Node goal, start;
        uint searchingSteps;
        float overEstimate;

        public float OverEstimate
        {
            get { return overEstimate; }
            set { overEstimate = value; }
        }

        List<System.Drawing.Point> path;
        System.Threading.Thread thread;

        //private System.Threading.ManualResetEvent allDone = new System.Threading.ManualResetEvent(false);
        public bool done = false;

        public List<System.Drawing.Point> Path
        {
            get
            {
                return path;
            }
        }

        private List<Node> nodePath;

        public List<Node> NodePath
        {
            get { return nodePath; }
        }

        public Astar()
        {
            this.relatedMap = Map.GetInstance();
            open = new List<Node>();
            closed = new List<Node>();
            path = new List<System.Drawing.Point>();
            nodePath = new List<Node>();
            this.overEstimate = 1;
            //Search(ref start_position, ref goal_position);

        }
        public Astar(System.Drawing.Point start_position, System.Drawing.Point goal_position)
        {
            this.relatedMap = Map.GetInstance();
            open = new List<Node>();
            closed = new List<Node>();
            path = new List<System.Drawing.Point>();
            this.overEstimate = 1;
            Search(true, ref start_position, ref goal_position);

        }

        public void Search(bool bThread, ref System.Drawing.Point start_position, ref System.Drawing.Point goal_position)
        {
            if (this.relatedMap == null)
            {
                this.relatedMap = Map.GetInstance();
            }
            closedList = new Node[this.relatedMap.MapSize.X * this.relatedMap.MapSize.X];
            this.open.Clear();
            this.closed.Clear();
            this.path.Clear();
            this.nodePath.Clear();
            this.searchingSteps = 0;

            //create start node
            this.start = new Node();
            this.start.position = start_position;
            this.start.g = 0;
            this.start.h = (float)Math.Sqrt(goal_position.X * goal_position.X + goal_position.Y * goal_position.Y);
            this.start.f = start.g + start.h;
            this.start.cellInfo = relatedMap.CellMap[start.position.X, start.position.Y];

            //create goal node and its evaluation
            this.goal = new Node();
            this.goal.position = goal_position;
            int xd = start.position.X - this.goal.position.X;
            int yd = start.position.Y - this.goal.position.Y;
            this.goal.g = Math.Max(Math.Abs(xd), Math.Abs(yd));
            this.goal.h = 0;
            this.goal.f = goal.g + goal.h;
            this.goal.cellInfo = relatedMap.CellMap[goal.position.X, goal.position.Y];

            //goal correction
            if (goal.cellInfo.Block && !goal.cellInfo.Danger)
            {
                Logger.AddInfo(this.goal.cellInfo.Position.ToString() + "goal je blocked");
                MapCellInfo[,] neighbourhood = this.relatedMap.getRelatedMap(this.goal.position, 5);
                for (int i = 0; i < neighbourhood.GetLength(0); i++)
                {
                    if (goal.cellInfo.Block && !goal.cellInfo.Danger)
                    {
                        for (int j = 0; j < neighbourhood.GetLength(1); j++)
                        {
                            if (!(neighbourhood[i, j].Block && !neighbourhood[i,j].Danger))
                            {
                                goal.position = neighbourhood[i, j].Position;
                                goal.cellInfo = neighbourhood[i, j];
                                break;
                            }
                        }
                    }
                }
            }

            if (goal.cellInfo.Block && !goal.cellInfo.Danger)
            {
                Logger.AddWarning(goal.position.ToString() + " - nelze najit cestu, cil je blokovan");
                return;
            }
            this.open.Add(start);
            Logger.AddImportant("Zacnu pocitat cestu");
            //allDone.Reset();
            done = false;

            //start path-finding!
            Logger.StartTimer("A*");
            if (bThread)
            {
                thread = new System.Threading.Thread(Start);
                thread.Priority = System.Threading.ThreadPriority.Normal;
                thread.Name = "A* thread";

                thread.IsBackground = true;
                Logger.AddThreadStart(thread);
                thread.Start();
            }
            else
            {
                Start();
            }
        }

        private void Start()
        {
            while (open.Count > 0)
            {
                float score = int.MaxValue;
                Node current = new Node();
                foreach (Node node in open)
                {
                    if (node.f < score)
                    {
                        score = node.f;
                        current = node;
                    }
                }
                //Logger.AddInfo("Zkoumam " + current.position.ToString() + " h=" + current.h);
                this.open.Remove(current);

#if DEBUG
                current.opens = new List<Node>();
                current.closeds = new List<Node>();
                current.opens.AddRange(this.open);
                current.closeds.AddRange(this.closed);
#endif

                if (current.position == goal.position)
                {
                    int closedCount = 0;
#if DEBUG
                    for (int i = 0; i < this.closedList.Length; i++)
                    {
                        if (this.closedList[i] != null)
                        {
                            closedCount++;
                        }
                    } 
#else
                    closedCount = -1;
#endif
                    Logging.Logger.AddInfo("mam vysledek, pocet kroku: " + searchingSteps.ToString() +
                        ", open list: " + this.open.Count + ", closed list: " + closedCount);
                    this.goal = current;
                    break;
                }

                List<Node> expanders = Expand(current);

                foreach (Node successor in expanders)
                {
                    successor.g = current.g + 1;// TransitCost(current, successor);
                    this.Evaluate(successor);

                    //crossways - more cost
                    if (successor.position.X != current.position.X && successor.position.Y != current.position.Y)
                    {
                        successor.h += 2;
                        successor.f += 2;
                    }
                    Node inOpen = null, inClosed = null;
                    foreach (Node node in this.open)
                    {
                        if (node.position == successor.position)
                        {
                            inOpen = node;
                            break;
                        }
                    }
                    //foreach (Node node in this.closed)
                    //{
                    //    if (node.position == successor.position)
                    //    {
                    //        inClosed = node;
                    //        break;
                    //    }
                    //}
                    if (closedList[successor.Index] != null)
                    {
                        inClosed = closedList[successor.Index];
                    }
                    if ((inOpen != null && inOpen.g <= successor.g) || (inClosed != null && inClosed.g <= successor.g))
                    {
                        continue;
                    }

                    if (inOpen != null)
                    {
                        this.open.Remove(inOpen);
                    }
                    if (inClosed != null)
                    {
                        //this.closed.Remove(inClosed);
                        this.closedList[inClosed.Index] = null;
                    }

                    successor.parent = current;
                    this.open.Add(successor);
                    searchingSteps++;
                }
                //System.Threading.Thread.Sleep(0);

                //this.closed.Add(current);
                this.closedList[current.Index] = current;
            }
            Node goalNode = goal;
            List<Node> temp = new List<Node>();
            while (goalNode.parent != null)
            {
                temp.Add(goalNode);
                goalNode = goalNode.parent;
            }
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                Map.GetInstance().CellMap[temp[i].position.X, temp[i].position.Y].bPartOfPath = true;
                path.Add(temp[i].position);
                nodePath.Add(temp[i]);
            }
            //allDone.Set();

#if DEBUG
            //Logger.AddImportant(this.relatedMap.ToString());
            //this.relatedMap.GetBitmap().Save("Paths\\" + DateTime.Now.Ticks.ToString() + "path" + path.Count.ToString() + ".png");
#endif
            done = true;
            Logger.StopTimer("A*");
        }

        private List<Node> Expand(Node node)
        {
            List<Node> successors = new List<Node>();
            Node successor = new Node();

            //East
            if (node.position.X + 1 < this.relatedMap.MapSize.X)
            {
                successor.position = new System.Drawing.Point(node.position.X + 1, node.position.Y);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //West
            if (node.position.X - 1 >= 0)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X - 1, node.position.Y);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //South
            if (node.position.Y - 1 >= 0)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X, node.position.Y - 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //North
            if (node.position.Y + 1 < this.relatedMap.MapSize.Y)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X, node.position.Y + 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }

            //NE
            if (node.position.Y + 1 < this.relatedMap.MapSize.Y &&
                node.position.X + 1 < this.relatedMap.MapSize.X)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X + 1, node.position.Y + 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //SE
            if (node.position.Y - 1 >= 0 &&
                node.position.X + 1 < this.relatedMap.MapSize.X)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X + 1, node.position.Y - 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //SW
            if (node.position.Y - 1 >= 0 &&
                node.position.X - 1 >= 0)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X - 1, node.position.Y - 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }
            //NW
            if (node.position.Y + 1 < this.relatedMap.MapSize.Y &&
                node.position.X - 1 >= 0)
            {
                successor = new Node();
                successor.position = new System.Drawing.Point(node.position.X - 1, node.position.Y + 1);
                successor.cellInfo = relatedMap.CellMap[successor.position.X, successor.position.Y];

                if (!successor.cellInfo.Block)
                {
                    successors.Add(successor);
                }
            }

            return successors;
        }

        private void Evaluate(Node node)
        {
            //node.g = (float)Math.Sqrt(node.position.X * node.position.X + node.position.Y * node.position.Y) -
            //    (float)Math.Sqrt(this.start.position.X * this.start.position.X + this.start.position.Y * this.start.position.Y);
            //node.h = (float)Math.Sqrt(this.goal.position.X * this.goal.position.X + this.goal.position.Y * this.goal.position.Y) -
            //    (float)Math.Sqrt(node.position.X * node.position.X + node.position.Y * node.position.Y);
            //node.g = Math.Abs(node.position.X - start.position.X) + Math.Abs(node.position.Y - start.position.Y);
            //node.h = Math.Abs(goal.position.X - node.position.X) + Math.Abs(goal.position.Y - node.position.Y);
            //node.g = Math.Abs(node.g);
            //node.h = Math.Abs(node.h);
            //this.relatedMap.CellMap[node.position.X, node.position.Y]

            float xd = node.position.X - this.goal.position.X;
            float yd = node.position.Y - this.goal.position.Y;

            node.h = Math.Max(Math.Abs(xd), Math.Abs(yd));
            node.h *= this.overEstimate;
            node.f = node.g + node.h;
        }

        private float TransitCost(Node fromNode, Node toNode)
        {
            return Map.GetInstance().GetRating(RatingType.Hiding, fromNode.position, toNode.position);
        }

        public float[,] GetH(Point goal)
        {
            float[,] h = new float[Map.GetInstance().MapSize.X, Map.GetInstance().MapSize.X];

            for (int i = 0; i < h.GetLength(0); i++)
            {
                for (int j = 0; j < h.GetLength(1); j++)
                {
                    float xd = i - goal.X;
                    float yd = j - goal.Y;

                    h[i, j] = Math.Max(Math.Abs(xd), Math.Abs(yd));
                }
            }
            return h;
        }
        public void Dispose()
        {
            this.relatedMap = null;
            this.open.Clear();
            this.closed.Clear();
        }
    }
}
