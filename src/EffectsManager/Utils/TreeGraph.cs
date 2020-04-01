using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.Utils
{
    /// <summary>
    /// Trida reprezentujici OctTree
    /// </summary>
    public class TreeGraph<T> : IDisposable
    {
        private T keys;
        private TreeGraph<T>[] childs;

        private TreeGraph<T> rightNeighboure;
        private TreeGraph<T> leftNeighboure;

        private int lastChild = 0;

        /// <summary>
        /// Konstruktor vytvarejici koren OctTree
        /// </summary>
        /// <param name="keys">Klice pro tento koren</param>
        public TreeGraph(int N, T keys)
        {
            childs = new TreeGraph<T>[N];
            this.keys = keys;
        }

        /// <summary>
        /// Funkce vrati klice aktualniho korenu
        /// </summary>
        /// <returns>Vraceny klice</returns>
        public T GetKeys()
        {
            return keys;
        }
        
        /// <summary>
        /// Funkce nastavi klice aktualniho korenu
        /// </summary>
        /// <param name="keys">Nove klice</param>
        public void SetKeys(T keys)
        {
            this.keys = keys;
        }

        /// <summary>
        /// Funkce vraci praveho souseda
        /// </summary>
        /// <returns>Pokud pravy soused existuje, vrati ho, jinak vrati null</returns>
        public TreeGraph<T> GetLeftNeighboure()
        {
            return leftNeighboure;
        }

        /// <summary>
        /// Funkce vraci leveho souseda
        /// </summary>
        /// <returns>Pokud levy soused existuje, vrati ho, jinak vrati null</returns>
        public TreeGraph<T> GetRightNeighboure()
        {
            return rightNeighboure;
        }

        /// <summary>
        /// Funkce vraci koren/list aktualniho korenu
        /// </summary>
        /// <param name="i">Poradove cislo korene</param>
        /// <returns>Vraci dcerinej koren/list</returns>
        public TreeGraph<T> GetChild(int i)
        {
            if ((i > childs.Length) && (i < 0))
                return null;

            return childs[i];
        }

        /// <summary>
        /// Funkce vraci strom, obsahujici pouze jednu uroven a to uroven listu
        /// </summary>
        /// <returns>Strom s jednotlivymi listy</returns>
        public TreeGraph<T> GetTreeLeaves()
        {
            TreeGraph<T> tree = this;

            while (tree.GetChild(0).GetChild(0) != null)
                tree = tree.GetChild(0);

            tree = tree.GetChild(0);

            return tree;
        }

        /// <summary>
        /// Funkce prida dalsi dcerinny prvek
        /// </summary>
        /// <param name="keys">Klice pro dcerinny prvek</param>
        public void AddChild(T keys)
        {
            childs[lastChild] = new TreeGraph<T>(childs.Length, keys);

            if (lastChild > 0)
            {
                childs[lastChild].leftNeighboure = childs[lastChild - 1];
                childs[lastChild - 1].rightNeighboure = childs[lastChild];
            }

            if (leftNeighboure != null && lastChild == 0)
            {
                TreeGraph<T> previousNode = leftNeighboure.GetChild(leftNeighboure.childs.Length - 1);

                previousNode.rightNeighboure = childs[lastChild];
                childs[lastChild].leftNeighboure = previousNode;
            }

            lastChild++;
        }

        /// <summary>
        /// Funkce pro uklizeni pameti
        /// </summary>
        public void Dispose()
        {
            if (childs != null)
            {
                for (int i = 0; i < childs.Length; i++)
                {
                    if (childs[i] != null)
                    {
                        childs[i].Dispose();
                        childs[i] = null;
                    }
                }
            }

        }
    }

}