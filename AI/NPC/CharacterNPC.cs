using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace WiccanRede.AI
{
    /// <summary>
    /// priorities for npc
    /// </summary>
    public struct Priorities
    {
        public int killPrior;
        public int livePrior;
        public int hidePrior;
        public int overviewPrior;
        public int boostPrior;
        public int surprisePrior;
        public int helpPrior;
        public int coverPrior;
        public List<int> priorsList;

        public override string ToString()
        {
            string str = "Priorities: ";
            foreach (int i in priorsList)
            {
                str += i + "; ";
            }
            return str;
        }
    }

    /// <summary>
    /// priorities for npc
    /// </summary>
    public struct CopyOfPriorities
    {
        public int killPrior;
        public int livePrior;
        public int hidePrior;
        public int overviewPrior;
        public int boostPrior;
        public int surprisePrior;
        public int helpPrior;
        public int coverPrior;
        public List<int> priorsList;

        public override string ToString()
        {
            string str = "CopyOfPriorities: ";
            foreach (int i in priorsList)
            {
                str += i + "; ";
            }
            return str;
        }
    }

    /// <summary>
    /// class represnting npc character and stats
    /// </summary>
    [DebuggerDisplay("CharacterNPC {name} level {level}")]
    public class CharacterNPC
    {
        public int hp, level, mana, power, defense;
        public int visualRange;
        public Side side;
        public NPCType type;
        public string name;
        public Priorities priors;

        /// <summary>
        /// ctor, loads character from xml file
        /// </summary>
        /// <param name="xmlFile">path to xml, where is stored character configuration</param>
        public CharacterNPC(String xmlFile)
        {
            this.LoadFromFile(xmlFile);
        }
        /// <summary>
        /// default ctor, create default character
        /// </summary>
        public CharacterNPC()
        {
            this.hp = 100;
            this.level = 1;
            this.mana = 50;
            this.power = 20;
            this.defense = 20;
            this.side = Side.neutral;
            this.type = NPCType.beast;
            this.name = "Default";
            this.priors = new Priorities();
        }

        /// <summary>
        /// loads character from xml
        /// </summary>
        /// <param name="path">path to xml configuration</param>
        private void LoadFromFile(String path)
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.Load(path);

            foreach (XmlNode node in doc.GetElementsByTagName("NPC"))
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "Name":
                            this.name = child.InnerText;
                            break;
                        case "Side":
                            this.side = (Side) Convert.ToInt32(child.InnerText);
                            break;
                        case "Type":
                            this.type = (NPCType)Convert.ToInt32(child.InnerText);
                            break;
                        case "VisualRange":
                            this.visualRange = Convert.ToInt32(child.InnerText);
                            break;
                        case "Attributes":
                            this.hp = Convert.ToInt32(child.Attributes[hp].InnerText);
                            this.level = Convert.ToInt32(child.Attributes["level"].InnerText);
                            this.mana = Convert.ToInt32(child.Attributes["mana"].InnerText);
                            this.power = Convert.ToInt32(child.Attributes["power"].InnerText);
                            this.defense = Convert.ToInt32(child.Attributes["defense"].InnerText);
                            break;
                        case "Priors":
                            Priorities priors = new Priorities();
                            priors.priorsList = new List<int>();
                            priors.killPrior = Convert.ToInt32(child.Attributes["kill"].Value);
                            priors.priorsList.Add(priors.killPrior);
                            priors.livePrior = Convert.ToInt32(child.Attributes["live"].Value);
                            priors.priorsList.Add(priors.livePrior);
                            priors.hidePrior = Convert.ToInt32(child.Attributes["hide"].Value);
                            priors.priorsList.Add(priors.helpPrior);
                            priors.overviewPrior = Convert.ToInt32(child.Attributes["overview"].Value);
                            priors.priorsList.Add(priors.overviewPrior);
                            priors.surprisePrior = Convert.ToInt32(child.Attributes["surprise"].Value);
                            priors.priorsList.Add(priors.surprisePrior);
                            priors.boostPrior = Convert.ToInt32(child.Attributes["boost"].Value);
                            priors.priorsList.Add(priors.boostPrior);
                            priors.coverPrior = Convert.ToInt32(child.Attributes["cover"].Value);
                            priors.priorsList.Add(priors.coverPrior);
                            priors.helpPrior = Convert.ToInt32(child.Attributes["help"].Value);
                            priors.priorsList.Add(priors.helpPrior);
                            this.priors = priors;
                            break;
                    }
                }
            }
        }
    }
}
