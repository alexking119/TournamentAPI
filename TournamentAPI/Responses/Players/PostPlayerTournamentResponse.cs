using TournamentAPI.Controllers;

namespace TournamentAPI.Responses.Players
{
    public class PostPlayerTournamentResponse : Response
    {
        public int PlayerId;
        public int TournamentId;
    }
}