using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.Game
{
    struct Follower
    {
        public string state;
        public string condition;

        public Follower(string state, string cond)
        {
            this.state = state;
            this.condition = cond;
        }
    }
    [System.Diagnostics.DebuggerDisplay("Substate: {conditionObject}->{substateName}")]
    struct Substate
    {
        public string substateName;
        public string conditionObject;
        public int orderIndex;

        public Substate(string substate, string obj, int orderIndex)
        {
            this.substateName = substate;
            this.conditionObject = obj;
            this.orderIndex = orderIndex;
        }


        public static bool operator ==(Substate state, string name)
        {
            if (name == state.conditionObject)
                return true;
            else
                return false;
        }
        public static bool operator !=(Substate state, string name)
        {
            if (name == state.conditionObject)
                return false;
            else
                return true;
        }
    }

    [System.Diagnostics.DebuggerDisplay("GameState: {name}")]
    internal class GameState
    {
        List<Follower> followers;

        internal List<Follower> Followers
        {
            get { return followers; }
        }
        List<Substate> substates;

        internal List<Substate> Substates
        {
            get { return substates; }
        }
        string name;

        public string Name
        {
            get { return name; }
        }

        bool order;

        public bool Order
        {
            get { return order; }
            set { order = value; }
        }

        private List<Substate> pastSubStates;

        public List<Substate> PastSubStates
        {
            get { return pastSubStates; }
        }

        public GameState(List<Follower> followers, List<Substate> substates, string name)
        {
            this.followers = followers;
            this.substates = substates;
            this.name = name;

            this.pastSubStates = new List<Substate>();
        }

        public static bool operator ==(GameState state, string name)
        {
            if (name == state.name)
                return true;
            else
                return false;
        }
        public static bool operator !=(GameState state, string name)
        {
            if (name == state.name)
                return false;
            else
                return true;
        }
    }
}
