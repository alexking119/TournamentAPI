using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using TournamentAPI.Inputs;
using TournamentAPI.Responses.Tournament;

namespace TournamentAPI
{
    public class DataStore : IDataStore, IDisposable
    {
        private readonly IDbConnection connection;
        private IDbTransaction transaction;
        private bool _hasCommited = false;

        public DataStore(IDbConnection connection)
        {
            this.connection = connection;
            this.Open();
        }

        private void Open()
        {
            connection.Open();
            transaction = connection.BeginTransaction();
        }

        public void Commit()
        {
            _hasCommited = true;
            transaction.Commit();
        }

        public void Dispose()
        {
            if (transaction != null && !_hasCommited)
            {
                transaction.Rollback();
            }
            connection.Close();
        }

        public TournamentGames[] GetTournamentGames(int tournamentId, RoundTypes roundTypeFilter = RoundTypes.ANY_TYPE)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@tournamentId", tournamentId }
            };

            using (var cmd = CreateCommand("GetTournamentGames", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    List<TournamentGames> tournamentGamesList = new List<TournamentGames>();

                    ReadGamesList(roundTypeFilter, reader, tournamentGamesList);
                    return tournamentGamesList.ToArray();
                }
            }
        }

        public Group[] GetListOfGroupsInTournament(int tournamentId)
        {
            return GetListOfGroupsInTournament(tournamentId, RoundTypes.ANY_TYPE);
        }

        public Group[] GetListOfGroupsInTournament(int tournamentId, RoundTypes roundFilter)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"@tournamentId", tournamentId }
            };
            using (var cmd = CreateCommand("ListGroupsForTournament", parameters))
            {

                Dictionary<int, Group> groups = new Dictionary<int, Group>();

                using (var reader = cmd.ExecuteReader())
                {
                    List<Group> groupList = new List<Group>();
                    while (reader.Read())
                    {
                        Group group = GetGroupFromReader(reader);
                        if (!groups.ContainsKey(group.id) && IsGroupIncludedInFilter(group, roundFilter))
                        {

                            Player player = new Player
                            {
                                id = (int)reader["PlayerId"],
                                firstName = (string)reader["FirstName"],
                                surname = (string)reader["Surname"]
                            };

                            group.playersInGroup.Add(player);
                            groups[group.id] = group;

                            
                        }
                        else if (IsGroupIncludedInFilter(group, roundFilter))
                        {

                            var existingGroup = groups[group.id];

                            Player player = new Player
                            {
                                id = (int)reader["PlayerId"],
                                firstName = (string)reader["FirstName"],
                                surname = (string)reader["Surname"]
                            };

                            existingGroup.playersInGroup.Add(player);
                        }
                    }

                    foreach(var group in groups)
                    {
                        groupList.Add(group.Value);
                    }

                    return groupList.ToArray();
                }
            }
        }

        public int CreateTournament(Tournament tournament)
        {
            Dictionary<string, object> parameters = CreateParametersForCreateTournamentStoreProc(tournament);
            int output;
            using (var cmd = CreateCommand("CreateTournament", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    output = reader.GetInt32(0);
                    return output;
                }
            }
        }

        public int CreateGroup(Group group)
        {
            Dictionary<string, object> parameters = CreateParametersForCreateGroup(group);
            int output;

            using (IDbCommand cmd = CreateCommand("CreateGroup"))
            {
                cmd.Connection = connection;
                cmd.Transaction = transaction;

                AddParametersToCommand(parameters, cmd);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    output = reader.GetInt32(0);
                }
            }
            return output;
        }

        public void CreateGame(Game game, int tournamentId)
        {
            Dictionary<string, object> parameters = CreateParametersForCreateGame(game, tournamentId);
            using (var cmd = CreateCommand("CreateGame", parameters))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void SetTournanamentStartedStatus(int tournamentId, bool status)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);
            parameters.Add("@status", status);
            using (var cmd = CreateCommand("SetTournamentStartedStatus", parameters))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public Template[] ListTemplates()
        {
            List<Template> templatesToReturn = new List<Template>();
            using (var cmd = CreateCommand("ListTemplates"))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AppendTemplate(reader, templatesToReturn);
                    }

                    return templatesToReturn.ToArray();
                }
            }
        }

        public Tournament[] ListTournaments()
        {
            List<Tournament> tournamentsToReturn;
            using (var cmd = CreateCommand("ListTournaments"))
            {

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader == null)
                    {
                        return null;
                    }

                    tournamentsToReturn = ListTournamentsFromSQLReader(reader);
                    return tournamentsToReturn.ToArray();
                }
            }
        }

        public Player[] ListParticipantsForTournament(int tournamentId)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@TournamentId", tournamentId);

            List<Player> playersToReturn = new List<Player>();
            using (var cmd = CreateCommand("ListParticipantsForTournament", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    playersToReturn = ListPlayersFromSQLReader(reader);
                }

                return playersToReturn.ToArray();
            }
        }

        public Template GetTemplate(int index)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@id", index);

            using (var cmd = CreateCommand("GetTemplate", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    Template templateToReturn = CreateTemplateFromReader(reader);

                    return templateToReturn;
                }
            }
        }

        public Tournament GetTournament(int index)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@id", index);

            using (var cmd = CreateCommand("GetTournament", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    Tournament tournamentToReturn = GenerateTournamentFromReader(reader);

                    return tournamentToReturn;
                }
            }
        }

        public Player[] ListPlayers()
        {
            List<Player> playersToReturn = new List<Player>();
            using (var cmd = CreateCommand("ListPlayers"))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        playersToReturn.Add(CreatePlayerFromFields(reader));
                    }

                    return playersToReturn.ToArray();
                }
            }
        }

        public Player GetPlayer(int index)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@id", index);

            using (var cmd = CreateCommand("GetPlayer", parameters))
            {

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return CreatePlayerFromFields(reader);
                }
            }

        }

        public int CreatePlayer(Player player)
        {
            Dictionary<string, object> parameters = CreateParametersForNewPlayer(player);

            using (var cmd = CreateCommand("CreatePlayer", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }
        }

        public Player UpdatePlayer(Player player, int id)
        {
            Dictionary<string, object> parameters = CreateParametersForUpdatePlayer(player, id);

            using (var cmd = CreateCommand("UpdatePlayer", parameters))
            {

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return CreatePlayerFromFields(reader);
                }
            }
        }

        public void SetScores(int userId, int tournamentId, int groupId, int player1Id, int player2Id, int player1Score, int player2Score)
        {
            var game = GetGame(player1Id, player2Id, tournamentId, groupId);
            int scoreEditor = userId;

            char newGameState;
            if (userId != player1Id && userId != player2Id)
            {
                throw new Exception("You can only change your own scores!");
            }
            if (game.state.Equals((char) GameStateEnum.Confirmed))
            {
                throw new Exception("Tournament scores have already been agreed by both players!");
            }
            if (game.state.Equals((char)GameStateEnum.Undefined))
            {
                newGameState = (char)GameStateEnum.Pending;
            }
            else if (UserIsScoreEditor(userId, game))
            {
                newGameState = (char)GameStateEnum.Pending;
            }
            else if (InputScoreMatchesExistingScore(player1Score, player2Score, game))
            {
                newGameState = (char)GameStateEnum.Confirmed;
                scoreEditor = (int) game.scoreEditor;
            } else if (game.scoreEditor == null)
            {
                newGameState = (char) GameStateEnum.Pending;
            }
            else
            {
                throw new Exception("Input scores do not match other players input! Please consult the other player and input matching scores!");
            }

            var parameters = SetupScoreParameters(tournamentId, groupId, player1Id, player2Id, player1Score, player2Score, newGameState, scoreEditor);

            using (var cmd = CreateCommand("EnterScore", parameters))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static bool UserIsScoreEditor(int userId, TournamentGames game)
        {
            return userId == game.scoreEditor;
        }

        public TournamentGames GetGame(int player1Id, int player2Id, int tournamentId, int groupId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            parameters.Add("@tournamentId", tournamentId);
            parameters.Add("@groupId", groupId);
            parameters.Add("@player1Id", player1Id);
            parameters.Add("@player2Id", player2Id);

            using (var cmd = CreateCommand("GetGame", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    var game = new TournamentGames()
                    {
                        tournamentId = (int)reader["TournamentId"],
                        player1Score = (int)reader["Player1Score"],
                        player2Score = (int)reader["Player2Score"],
                        state = ((string)reader["State"])[0],
                        scoreEditor = reader["ScoreEditor"] as int?
                    };

                    return game;
                }
            }
        }

        public int GetPlayerIdFromUsername(string username)
        {
            username = username.Replace("MGSOPS\\", "");

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@Username", username);

            using (var cmd = CreateCommand("GetPlayerIdFromUsername", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        int output = reader.GetInt32(0);

                        return output;
                    }
                    throw new Exception("User (" + username + ") does not exist on system.");
                }
            }
        }

        public string GetPlayerNameFromId(int playerId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", playerId);

            using (var cmd = CreateCommand("GetPlayerNameFromId", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        string firstName = (string)reader["FirstName"];
                        string surname = (string)reader["Surname"];
                        string output = firstName +" "+ surname;
                        return output;
                    }
                    throw new Exception("User with ID (" + playerId + ") does not exist on system.");
                }
            }
        }

        public int[] GetPlayerTournaments(int playerId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", playerId);

            using (var cmd = CreateCommand("ListSignedUpTournaments", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    List<int> output = new List<int>();

                    while (reader.Read())
                    {
                        output.Add(reader.GetInt32(0));
                    }

                    return output.ToArray();
                }
            }
        }

        public PlayerScore[] GetPlayerScores(int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);

            using (var cmd = CreateCommand("GetScoresAndFrameDifferenceForGroup", parameters))
            {
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    List<PlayerScore> output = new List<PlayerScore>();

                    while (reader.Read())
                    {
                        var playerStats = CreatePlayerScoreFromReader(reader);
                        output.Add(playerStats);
                    }

                    return output.ToArray();
                }
            }
        }

        public bool IsRoundCompleted(int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            parameters.Add("@tournamentId", tournamentId);

            using (var cmd = CreateCommand("GetNumberOfIncompleteGames", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return ((int) reader["NonCompletedGames"]) == 0;
                }
            }
        }

        public int GetCurrentRound(int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            parameters.Add("@tournamentId", tournamentId);

            using (var cmd = CreateCommand("GetCurrentRound", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return (int) reader["RoundId"];
                }
            }
        }

        public void CreateTournamentPlayer(int playerId, int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@PlayerId", playerId);
            parameters.Add("@TournamentId", tournamentId);
            using (var cmd = CreateCommand("CreateTournamentPlayer", parameters))
            {
                cmd.ExecuteNonQuery();
            }
        }
        
        public int RemoveTournamentPlayer(int playerId, int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@PlayerId", playerId);
            parameters.Add("@TournamentId", tournamentId);

            using (var cmd = CreateCommand("RemoveTournamentPlayer", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return (int)reader["playerId"];
                }

            }
        }

        public bool IsUserKnown(string username)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@Username", username);

            using (var cmd = CreateCommand("IsUserInDatabase", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return ((int)reader["UsernameCount"]) != 0;
                }
            }
        }

        public bool HasTournamentStarted(int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);

            using (var cmd = CreateCommand("HasTournamentStarted", parameters))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return (bool)reader["HasStarted"];
                }
            }
        }

        public void UpdateTournamentState(int tournamentId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);

            using (var cmd = CreateCommand("UpdateTournamentState", parameters))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private bool IsGroupIncludedInFilter(Group group, RoundTypes filter)
        {
            if (group.roundId == (int)filter)
            {
                return true;
            }
            if (filter == RoundTypes.ANY_TYPE)
            {
                return true;
            }
            if (filter == RoundTypes.KNOCKOUT_ANY && (
                    filter == RoundTypes.KNOCKOUT_64 ||
                    filter == RoundTypes.KNOCKOUT_32 ||
                    filter == RoundTypes.KNOCKOUT_16 ||
                    filter == RoundTypes.KNOCKOUT_8 ||
                    filter == RoundTypes.QUATERFINALS ||
                    filter == RoundTypes.SEMIFINALS ||
                    filter == RoundTypes.FINALS
                ))
            {
                return true;
            }
            return false;
        }

        private static bool InputScoreMatchesExistingScore(int player1Score, int player2Score, TournamentGames game)
        {
            return game.scoreEditor != null && player1Score == game.player1Score && player2Score == game.player2Score;
        }

        private static Dictionary<string, object> SetupScoreParameters(int tournamentId, int groupId, int player1Id, int player2Id,
            int player1Score, int player2Score, char newGameState, int scoreEditor)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);
            parameters.Add("@groupId", groupId);
            parameters.Add("@player1Id", player1Id);
            parameters.Add("@player2Id", player2Id);
            parameters.Add("@player1Score", player1Score);
            parameters.Add("@player2Score", player2Score);
            parameters.Add("@state", newGameState);
            parameters.Add("@scoreEditor", scoreEditor);
            return parameters;
        }

        private static void ReadGamesList(RoundTypes roundTypeFilter, IDataReader reader, List<TournamentGames> tournamentGamesList)
        {
            while (reader.Read())
            {
                TournamentGames game = GetTournamentGameFromReader(reader);
                if (GameIsIncludedInFilter(roundTypeFilter, game))
                {
                    tournamentGamesList.Add(game);
                }
            }
        }

        private static bool GameIsIncludedInFilter(RoundTypes roundTypeFilter, TournamentGames game)
        {
            if ((RoundTypes)game.roundId == roundTypeFilter || roundTypeFilter == RoundTypes.ANY_TYPE)
            {
                return true;
            }
            if (roundTypeFilter == RoundTypes.KNOCKOUT_ANY &&
                ((RoundTypes)game.roundId == RoundTypes.KNOCKOUT_64 ||
                 (RoundTypes)game.roundId == RoundTypes.KNOCKOUT_32 ||
                 (RoundTypes)game.roundId == RoundTypes.KNOCKOUT_16 ||
                 (RoundTypes)game.roundId == RoundTypes.KNOCKOUT_8 ||
                 (RoundTypes)game.roundId == RoundTypes.QUATERFINALS ||
                 (RoundTypes)game.roundId == RoundTypes.SEMIFINALS ||
                 (RoundTypes)game.roundId == RoundTypes.FINALS))
            {
                return true;
            }
            return false;
        }

        private static Template CreateTemplateFromReader(IDataReader templateReader)
        {
            int id;
            string name;
            string className;
            Template templateToReturn;
            id = (int)templateReader["id"];
            name = (string)templateReader["name"];
            className = (string)templateReader["className"];
            templateToReturn = new Template() { id = id, name = name, className = className };
            return templateToReturn;
        }

        private static PlayerScore CreatePlayerScoreFromReader(IDataReader reader)
        {
            var playerStats = new PlayerScore();

            playerStats.id = (int)reader["playerId"];
            playerStats.wins = (int)reader["wins"];
            playerStats.lose = (int)reader["lose"];
            playerStats.draw = (int)reader["draw"];
            playerStats.win_frames = (int)reader["win_frames"];
            playerStats.lose_frames = (int)reader["lose_frames"];
            playerStats.groupId = (int)reader["GroupId"];
            return playerStats;
        }

        private static Dictionary<string, object> CreateParametersForNewPlayer(Player player)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@firstName", player.firstName);
            parameters.Add("@surname", player.surname);
            parameters.Add("@email", player.email);
            parameters.Add("@username", player.username);
            return parameters;
        }

        private static Dictionary<string, object> CreateParametersForCreateGroup(Group group)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@name", group.name);
            return parameters;
        }
        private static Dictionary<string, object> CreateParametersForUpdatePlayer(Player player, int id)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@id", id);
            parameters.Add("@firstName", player.firstName);
            parameters.Add("@surname", player.surname);
            parameters.Add("@email", player.email);
            return parameters;
        }

        private static Dictionary<string, object> CreateParametersForCreateGame(Game game, int tournamentId)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@tournamentId", tournamentId);
            parameters.Add("@groupId", game.Group.id);
            parameters.Add("@roundId", game.Group.roundId);
            parameters.Add("@player1Id", game.Player1.id);
            parameters.Add("@player2Id", game.Player2.id);
            return parameters;
        }

        private static Dictionary<string, object> CreateParametersForCreateTournamentStoreProc(Tournament tournament)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@templateId", tournament.templateId);
            parameters.Add("@name", tournament.name);
            parameters.Add("@description", tournament.description);
            parameters.Add("@startDate", tournament.startDate);
            parameters.Add("@endDate", tournament.endDate);
            parameters.Add("@organiser", tournament.organiser);
            return parameters;
        }

        private static Player CreatePlayerFromFields(IDataReader players)
        {
            var id = (int)players["id"];
            var firstName = (string)players["firstName"];
            var surname = (string)players["surname"];
            var email = (string)players["EmailAddress"];
            var username = (string)players["username"];

            return new Player() { id = id, firstName = firstName, surname = surname, email = email, username = username };
        }

        private static void AppendTemplate(IDataReader templates, List<Template> templatesToReturn)
        {
            var id = (int)templates["id"];
            var name = (string)templates["name"];
            var className = (string)templates["className"];

            templatesToReturn.Add(new Template() { id = id, name = name, className = className });
        }

        private List<Tournament> ListTournamentsFromSQLReader(IDataReader tournaments)
        {
            List<Tournament> tournamentsToReturn = new List<Tournament>();
            while (tournaments.Read())
            {
                AppendTournament(tournaments, tournamentsToReturn);
            }

            return tournamentsToReturn;
        }

        private List<Player> ListPlayersFromSQLReader(IDataReader players)
        {
            List<Player> playersToReturn = new List<Player>();
            while (players.Read())
            {
                AppendPlayer(players, playersToReturn);
            }

            return playersToReturn;
        }

        private static void AppendTournament(IDataReader tournaments, List<Tournament> tournamentsToReturn)
        {
            var id = (int)tournaments["id"];
            var templateId = (int)tournaments["templateId"];
            var name = (string)tournaments["name"];
            var description = (string)tournaments["description"];
            var startDate = (DateTime)tournaments["startDate"];
            var endDate = (DateTime)tournaments["endDate"];
            var organiser = (int)tournaments["OrganiserPlayerId"];
            var hasStarted = (bool)tournaments["HasStarted"];

            tournamentsToReturn.Add(new Tournament()
            {
                id = id,
                templateId = templateId,
                name = name,
                description = description,
                startDate = startDate,
                endDate = endDate,
                organiser = organiser,
                hasStarted = hasStarted
            });
        }

        private static void AppendPlayer(IDataReader players, List<Player> playersToReturn)
        {
            var id = (int)players["PlayerId"];
            var firstName = (String)players["FirstName"];
            var surname = (String)players["Surname"];
            var email = (String)players["Email"];
            var username = (String)players["Username"];

            playersToReturn.Add(new Player()
            {
                id = id,
                firstName = firstName,
                surname = surname,
                email = email,
                username = username
            });
        }

        private static Tournament GenerateTournamentFromReader(IDataReader reader)
        {
            Tournament tournamentToReturn;
            var id = (int)reader["id"];
            var templateId = (int)reader["templateId"];
            var name = (string)reader["name"];
            var description = (string)reader["description"];
            var startDate = (DateTime)reader["startDate"];
            var endDate = (DateTime)reader["endDate"];
            var organiser = (int)reader["OrganiserPlayerId"];
            var hasStarted = (bool)reader["HasStarted"];

            tournamentToReturn = new Tournament()
            {
                id = id,
                templateId = templateId,
                name = name,
                description = description,
                startDate = startDate,
                endDate = endDate,
                organiser = organiser,
                hasStarted = hasStarted
            };
            return tournamentToReturn;
        }

        private static void AddParametersToCommand(Dictionary<string, object> parameters, IDbCommand cmd)
        {
            if (parameters == null) return;

            foreach (string parameterName in parameters.Keys) // Add parameters to the command
            {
                var parameterValue = parameters[parameterName];
                SqlParameter parameter = new SqlParameter(parameterName, parameterValue);

                cmd.Parameters.Add(parameter);
            }
        }

        private IDbCommand CreateCommand(string procedure, Dictionary<string, object> parameters = null)
        {
            var cmd = connection.CreateCommand();

            cmd.Connection = connection;
            cmd.Transaction = transaction;

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedure;

            AddParametersToCommand(parameters, cmd);
            return cmd;
        }


        private static TournamentGames GetTournamentGameFromReader(IDataReader reader)
        {
            TournamentGames game = new TournamentGames()
            {
                tournamentId = (int)reader["TournamentId"],
                groupId = (int)reader["GroupID"],
                name = (string)reader["Name"],
                player1Id = (int)reader["Player1Id"],
                player1Name = (string)reader["player1Name"],
                player1Score = (reader["Player1Score"] is DBNull) ? 0 : (int)reader["Player1Score"],
                player2Id = (int)reader["Player2Id"],
                player2Name = (string)reader["player2Name"],
                player2Score = (reader["Player2Score"] is DBNull) ? 0 : (int)reader["Player2Score"],
                roundId = (int)reader["RoundId"],
                state = (char)GetGameStateFromReader(reader)
            };
            return game;
        }

        private static Group GetGroupFromReader(IDataReader reader)
        {
            Group group = new Group()
            {
                id = (int)reader["Id"],
                name = (string)reader["Name"],
                roundId = (int)reader["RoundId"]
            };
            return group;
        }
        
        private static GameStateEnum GetGameStateFromReader(IDataReader reader)
        {
            switch ((string)reader["State"])
            {
                case "U":
                    return GameStateEnum.Undefined;
                case "C":
                    return GameStateEnum.Confirmed;
                case "P":
                    return GameStateEnum.Pending;
            }
            return GameStateEnum.Undefined;
        }

    }
}
