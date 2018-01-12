using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TournamentAPI
{
    public class PlayerScore
    {
        public int id;
        public int wins;
        public int lose;
        public int draw;
        public int win_frames;
        public int lose_frames;
        public int groupId;
        public int score { get
            {
                return (wins * 3) + (draw * 1) + (lose * 0);
            }
        }
        public int frames {
            get {
                return win_frames;
            }
        }
    }
}
