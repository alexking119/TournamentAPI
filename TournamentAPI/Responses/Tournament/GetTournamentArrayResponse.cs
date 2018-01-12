using TournamentAPI.Controllers;

namespace TournamentAPI.Responses.Tournament
{
    public class GetTournamentArrayResponse : Response
    {
        public TournamentAPI.Tournament[] Tournaments;
        public int[] playerTournaments;
    }
}