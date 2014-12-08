using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace RecallTracker_ProFlash 
{
    public class Recall 
    {
        public enum RecallState 
        {
            Recalling,
            Ported,
            Cancelled,
            Unknown
        }

        private int _lastActionTick;
        private RecallState _lastAction;       
        public RecallState LastAction
        {
            get { return _lastAction; } 
            set 
            {
                _lastAction = value;

                if (_lastAction == RecallState.Unknown) 
                {
                    if (TimeElapsedms > (recallTime-40)) LastAction = RecallState.Ported;
                    else LastAction = RecallState.Cancelled;
                } 
                else _lastActionTick = Environment.TickCount;
            } 
        }
        
        private Obj_AI_Hero _player;       
        public Obj_AI_Hero Player { get { return _player; } }
        public int TimeElapsedms { get { return (Environment.TickCount - _lastActionTick); } }

        public Recall(Obj_AI_Hero target) 
        {
            _player = target;
            recallTime = (_player.Masteries.Where(x => x.Page == MasteryPage.Utility && x.Id == 0x41 && x.Points == 1).Count() == 0) ? 8000 : 7000;
        }
        
        private int recallTime;
        public bool update() 
        {
            if (_lastAction == RecallState.Ported) return (TimeElapsedms>=25000)? true:false;
            else if (_lastAction == RecallState.Cancelled) return (TimeElapsedms >= 3000) ? true : false;
            else if (_lastAction == RecallState.Recalling) return (TimeElapsedms >= 8000) ? true : false;
            else return true;
        }

        public override string ToString() 
        {
            if (LastAction == RecallState.Recalling)
                return String.Format("{0} Recalling ({1:0.00})", Player.BaseSkinName, (((float)(TimeElapsedms - recallTime)) / (float)1000));
            else if (LastAction == RecallState.Ported)
                return String.Format("{0} Ported ({1:0.00})", Player.BaseSkinName, ((float)TimeElapsedms / (float)1000));
            else if (LastAction == RecallState.Cancelled)
                return String.Format("{0} Cancelled",Player.BaseSkinName);
            else return "";
        }
    }    
}
