namespace TournamentAPI
{
    public interface IDataStore
    {
        void Commit();

        Template[] ListTemplates();

        Template GetTemplate(int index);

        int CreateTournament(Tournament tournament);

        Tournament[] ListTournaments();

        Player[] ListParticipantsForTournament(int id);

        Tournament GetTournament(int index);

        Player[] ListPlayers();

        Player GetPlayer(int index);

        int CreatePlayer(Player player);

        int CreateGroup(Group group);

        Player UpdatePlayer(Player player, int id);

        void CreateGame(Game game, int tournamentId);

        void SetTournanamentStartedStatus(int tournamentId, bool status);

        void SetScores(int userId, int tournamentId, int groupId, int player1Id, int player2Id, int player1Score, int player2Score);

        PlayerScore[] GetPlayerScores(int tournamentId);

        bool IsRoundCompleted(int tournamentId);
    }
}
