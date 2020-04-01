using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.AI
{
    public interface IFsmState
    {
        string GetName();
        List<string> GetNextPossibleStates();
        bool IsEqual(string stateName);
    }

    /// <summary>
    /// abstract class providing environment for finite state machine plug-ins
    /// </summary>
    public abstract class FSM : IControlMechanism
    {
        /// <summary>
        /// actual state of state machine
        /// </summary>
        protected IFsmState actualState;
        /// <summary>
        /// actual state of state machine
        /// </summary>
        public IFsmState ActualState
        {
            get
            {
                return this.actualState;
            }
            set
            {
                this.actualState = value;
            }
        }

        /// <summary>
        /// Updates the state, calls React function
        /// </summary>
        public abstract void Update();

        ///// <summary>
        ///// React at action, by change of the state 
        ///// </summary>
        ///// <param name="action"></param>
        //internal abstract void React(WiccanRede.AI.AiEvents action);

        ///// <summary>
        ///// in an accordance to actual state do some action
        ///// </summary>
        ///// <param name="actionSelection">object to do action select</param>
        //internal abstract void CheckState(WiccanRede.AI.IActionSelection actionSelection);
    }
}
