using TournamentAPI.Controllers;

namespace TournamentAPI.Responses.Tournament
{
    public class GetTournamentParticipantsResponse : Response
    {
        public Player[] Participants;
    }
}