using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TournamentAPI.Inputs
{
    public class PutScoresInput
    {
        public int tournamentId;
        public int groupId;
        public int player1Id;
        public int player2Id;
        public int player1Score;
        public int player2Score;
    }
}
