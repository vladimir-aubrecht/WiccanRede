using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using Logging;

namespace WiccanRede.Game
{
    class GameStateMachine
    {
        GameState currentState;

        internal GameState CurrentState
        {
            get { return currentState; }
        }

        List<GameState> gameStates;

        public GameStateMachine(string config)
        {
            this.gameStates = new List<GameState>();
            Logger.AddInfo("Nacitani game config xml: " + config);
            try
            {
                LoadConfiguration(config);
            }
            catch (Exception ex)
            {
                Logger.AddError("Chyba pri nacitani Game config xml, " + ex.ToString());
            }
        }

        private void LoadConfiguration(string confFile)
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.Load(confFile);

            string name ="";
            List<Follower> followers = new List<Follower>(8);
            List<Substate> substates = new List<Substate>(8);
            bool order = false;
            GameState state;

            foreach (XmlNode gameStateNode in doc.GetElementsByTagName("GameState"))
            {
                foreach (XmlNode child in gameStateNode.ChildNodes)
	            {
            		switch (child.Name)
                    {
                        case "Name":
                            name = child.InnerText;
                            break;
                        case "Followers":
                            foreach (XmlNode followerNode in child.ChildNodes)
	                        {
                                Follower f = new Follower();
                                f.condition = followerNode.Attributes["condition"].InnerText;
                                f.state = followerNode.InnerText;
                        		followers.Add(f);
	                        }
                            break;
                        case "Substates":
                            if (child.Attributes.Count > 0)
                            {
                                foreach (XmlAttribute att in child.Attributes)
                                {
                                    if (att.Name == "order")
                                    {
                                        order = Convert.ToBoolean(att.Value);
                                    }
                                }
                            }
                            int index = 0;
                            foreach (XmlNode substateNode in child.ChildNodes)
	                        {
                                Substate s = new Substate();
                                s.orderIndex = index;
                                s.conditionObject = substateNode.Attributes["object"].InnerText;
                                s.substateName = substateNode.InnerText;
                                substates.Add(s);
                                index++;
	                        }
                            break;
                        default:
                            Logger.AddWarning("Neznamy tag v game config xml");
                            break;
                    }
	            }
                state = new GameState(followers, substates, name);
                state.Order = order;
                this.gameStates.Add(state);
                followers = new List<Follower>(8);
                substates = new List<Substate>(8);
                if (state.Name == "Start")
                {
                    this.currentState = state;
                }
            }//outer foreach end
        }

        /// <summary>
        /// nastavi novy stav game states machine, pokud souhlasi objekt a podminka
        /// </summary>
        /// <param name="obj"> objekt, ktery posila udalost</param>
        /// <param name="ev">posilana udalost</param>
        /// <returns>vraci zda souhlasi objekt a udalost</returns>
        public bool Update(string obj, string ev)
        {
            //if (this.currentState.Substates.Contains(new Substate(ev, obj)) )
            bool valid = false;
            Substate subState = new Substate();

            foreach (Substate state in this.currentState.Substates)
            {
                //porovnat zda prichozi objekt souhlasi s prechodovym objektem substavu
                if (state == obj)
                {
                    valid = true;
                    subState = state;

                    if (!this.currentState.PastSubStates.Contains(state))
                    {
                        this.currentState.PastSubStates.Add(state); 
                    }
                    break;
                }
            }

            if (valid)
            {
                for (int i = 0; i < this.currentState.Followers.Count; i++)
                {
                    //prechod na naslednika je podminen udalosti a objektem
                    if (this.currentState.Followers[i].condition == subState.substateName) 
                        //|| ev == "click") //TODO zatim obecne na kliknuti - MUSI se predelat
                    {
                        //vybrat prislusny gameState
                        for (int j = 0; j < this.gameStates.Count; j++)
                        {
                            if (this.gameStates[j].Name == this.currentState.Followers[i].state)
                            {
                                if (!this.currentState.Order)
                                {
                                    this.currentState = this.gameStates[j];
                                    Logger.AddImportant("Prechod na novy game state: " + this.currentState.Name);
                                    return true; 
                                }
                                else
                                {
                                    if (this.currentState.PastSubStates.Count == this.currentState.Substates.Count)
                                    {
                                        this.currentState = this.gameStates[j];
                                        Logger.AddImportant("Prechod na novy game state: " + this.currentState.Name);
                                        return true; 
                                    }
                                }
                            }
                        }//konec cyklu

                    }
                } 
            }

            return false;
        }
    }
}
