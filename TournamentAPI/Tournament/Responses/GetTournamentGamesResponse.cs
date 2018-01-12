using TournamentAPI.Controllers;
using TournamentAPI.Inputs;

namespace TournamentAPI.Responses.Tournament
{

    public class GetTournamentGamesResponse : Response
    {
        public TournamentGames[] games;

    }
    public class TournamentGames
    {
        public int tournamentId;
        public int groupId;
        public string name;
        public int player1Id;
        public string player1Name;
        public int player1Score;
        public int player2Id;
        public string player2Name;
        public int player2Score;
        public int roundId;
        public char state;
        public int? scoreEditor;
    }
}
