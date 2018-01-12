using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TournamentAPI.Controllers;

namespace TournamentAPI.Responses.Tournament
{
    public class GetPlayerScoresResponse : Response
    {
        public PlayerScore[] playerScores;
    }
}
