using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TournamentAPI.Controllers;

namespace TournamentAPI
{
    public class TournamentGenerator
    {
        private const int TARGET_GROUP_SIZE = 4;
        private const int TOP_PLAYERS_TO_SELECT_FOR_KNOCKOUT = 2;

        public void Create(IDataStore dataStore, int tournamentId)
        {
            Player[] participantsList = dataStore.ListParticipantsForTournament(tournamentId);
            CreateGroupStageGames(dataStore, tournamentId, participantsList);

            dataStore.SetTournanamentStartedStatus(tournamentId, true);
        }


        public static void CreateKnockoutStageGames(IDataStore dataStore, Tournament tournament)
        {
            var knockoutPlayers = GetPlayersForKnockoutStage(dataStore, tournament);
            var roundType = CalculateRoundId(knockoutPlayers);
            var knockoutGames = GenerateFirstKnockoutStageRound(knockoutPlayers.ToArray(), tournament, roundType, dataStore);
            StoreGames(dataStore, knockoutGames, tournament.id);
        }

        public static void CreateKnockoutStageGames(IDataStore dataStore, Tournament tournament, List<PlayerScore> knockoutPlayers)
        {
            var roundType = CalculateRoundId(knockoutPlayers);
            var knockoutGames = GenerateRegularKnockoutStageRound(knockoutPlayers.ToArray(), tournament, roundType, dataStore);
            StoreGames(dataStore, knockoutGames, tournament.id);
        }

        private static RoundTypes CalculateRoundId(List<PlayerScore> knockoutPlayers)
        {
            if (knockoutPlayers.Count > 32)
            {
                return RoundTypes.KNOCKOUT_64;
            }
            else if (knockoutPlayers.Count > 16)
            {
                return RoundTypes.KNOCKOUT_32;
            }
            else if (knockoutPlayers.Count > 8)
            {
                return RoundTypes.KNOCKOUT_16;
            }
            else if (knockoutPlayers.Count > 4)
            {
                return RoundTypes.QUATERFINALS;
            }
            else if (knockoutPlayers.Count > 2)
            {
                return RoundTypes.SEMIFINALS;
            }

            return RoundTypes.FINALS;
        }

        private void CreateGroupStageGames(IDataStore dataStore, int tournamentId, Player[] participantsList)
        {
            List<Group> tournamentGroup = CreateGroups(dataStore, participantsList, RoundTypes.GROUP);

            foreach (Group group in tournamentGroup)
            {
                List<Game> games = CreateGamesFromGroup(group);
                StoreGames(dataStore, games, tournamentId);
            }
        }

        protected List<Group> CreateGroups(IDataStore datastore, Player[] tournamentPlayers, RoundTypes round)
        {
            List<Group> tournamentGroups = CreateTournamentGroupForPlayers(datastore, tournamentPlayers, round);
            AddPlayersToTournamentGroups(tournamentPlayers, tournamentGroups);
            return tournamentGroups;
        }


        protected List<Game> CreateGamesFromGroup(Group group)
        {
            var games = new List<Game>();

            var numberOfPlayers = group.playersInGroup.Count;
            for (int i = 0; i < numberOfPlayers - 1; i++)
            {
                for (int j = i + 1; j < numberOfPlayers; j++)
                {
                    var game = new Game() { Player1 = group.playersInGroup[i], Player2 = group.playersInGroup[j], Group = group };
                    games.Add(game);
                }
            }

            return games;
        }

        public static List<PlayerScore> GetPlayersForKnockoutStage(IDataStore dataStore, Tournament tournament)
        {
            var playerScores = dataStore.GetPlayerScores(tournament.id).ToList();

            List<PlayerScore> playersForKnockout = new List<PlayerScore>();

            GetTopPlayersFromEachGroup(playerScores, playersForKnockout, dataStore);

            var numberOfRounds = (int)Math.Ceiling(Math.Log(playersForKnockout.Count, 2));
            var requiredPlayers = (int) Math.Pow(2, numberOfRounds);

            AddExtraRequiredPlayers(playersForKnockout, requiredPlayers, playerScores, dataStore);

            return playersForKnockout;
        }

        private static List<Game> GenerateFirstKnockoutStageRound(PlayerScore[] playerScores, Tournament tournament, RoundTypes round, IDataStore dataStore)
        {
            List<PlayerScore> scores = playerScores.ToList();

            List<Game> games = new List<Game>();
            while (scores.Count > 0)
            {
                for (int j = 1; j < scores.Count; j++)
                {
                    if (PlayersAreInTheSameGroup(scores[0], scores[j])) continue;
                    int player1Id = scores[0].id;
                    int player2Id = scores[j].id;
                    var group = CreateKnockoutGroup(round, dataStore, player1Id, player2Id);
                    games.Add(new Game() { Group = group, Player1 = new Player() { id = player1Id }, Player2 = new Player() { id = player2Id }, Tournament = tournament });

                    scores.Remove(scores[j]);
                    scores.Remove(scores[0]);
                }
            }
            return games;
        }

        private static List<Game> GenerateRegularKnockoutStageRound(PlayerScore[] playerScores, Tournament tournament, RoundTypes round, IDataStore dataStore)
        {
            List<PlayerScore> scores = playerScores.ToList();

            List<Game> games = new List<Game>();

            for (int i = 0; i < scores.Count; i += 2)
            {
                int player1Id = scores[i].id;
                int player2Id = scores[i+1].id;
                var group = CreateKnockoutGroup(round, dataStore, player1Id, player2Id);
                games.Add(new Game() { Group = group, Player1 = new Player() { id = player1Id }, Player2 = new Player() { id = player2Id }, Tournament = tournament });
            }
            return games;
        }

        private static Group CreateKnockoutGroup(RoundTypes round, IDataStore dataStore, int player1Id, int player2Id)
        {
            var group = new Group();
            group.name = GetRoundName(round);
            group.roundId = (int)round;
            group.playersInGroup.Add(new Player() { id = player1Id });
            group.playersInGroup.Add(new Player() { id = player2Id });
            group.id = dataStore.CreateGroup(group);
            return group;
        }

        private static string GetRoundName(RoundTypes round)
        {
            switch (round)
            {
                case RoundTypes.GROUP:
                    break;
                case RoundTypes.KNOCKOUT_64:
                    return "Knockout 64";
                case RoundTypes.KNOCKOUT_32:
                    return "Knockout 32";
                case RoundTypes.KNOCKOUT_16:
                    return "Knockout 16";
                case RoundTypes.KNOCKOUT_8:
                    return "Knockout 8";
                case RoundTypes.QUATERFINALS:
                    return "Quarterfinals";
                case RoundTypes.SEMIFINALS:
                    return "Semifinals";
                case RoundTypes.FINALS:
                    return "Finals";
            }
            return "Unknown Ground";
        }

        private static bool PlayersAreInTheSameGroup(PlayerScore player1, PlayerScore player2)
        {
            return player1.groupId == player2.groupId;
        }

        private static void AddExtraRequiredPlayers(List<PlayerScore> playersForKnockout, int requiredPlayers, List<PlayerScore> playerScores, IDataStore dataStore)
        {
            while (playersForKnockout.Count < requiredPlayers)
            {
                var highestGroupScores = SelectTopPlayerFromGroups(playerScores);
                foreach (int groupId in highestGroupScores.Keys)
                {
                    UpdateGroupArrays(highestGroupScores, groupId, playerScores, playersForKnockout, dataStore);

                    if (HasGotEnoughPlayers(playersForKnockout, requiredPlayers))
                        break;
                }
            }
        }

        private static bool HasGotEnoughPlayers(List<PlayerScore> playersForKnockout, int requiredPlayers)
        {
            return playersForKnockout.Count >= requiredPlayers;
        }

        private static void UpdateGroupArrays(Dictionary<int, PlayerScore> highestGroupScores, int groupId, List<PlayerScore> playerScores, List<PlayerScore> playersForKnockout, IDataStore dataStore)
        {
            var score = highestGroupScores[groupId];
            playerScores.Remove(score);
            playersForKnockout.Add(score);
        }

        private static void GetTopPlayersFromEachGroup(List<PlayerScore> playerScores, List<PlayerScore> playersForKnockout, IDataStore dataStore)
        {
            for (int i = 0; i < TOP_PLAYERS_TO_SELECT_FOR_KNOCKOUT; i++)
            {
                var highestGroupScores = SelectTopPlayerFromGroups(playerScores);
                UpdateAllGroupArrays(highestGroupScores, playerScores, playersForKnockout, dataStore);
            }
        }

        private static Dictionary<int, PlayerScore> SelectTopPlayerFromGroups(List<PlayerScore> playerScores)
        {
            // groupId, score
            Dictionary<int, PlayerScore> highestGroupScores = new Dictionary<int, PlayerScore>();
            foreach (PlayerScore playerScore in playerScores)
            {
                UpdateHighestScore(highestGroupScores, playerScore);
            }
            return highestGroupScores;
        }

        private static void UpdateAllGroupArrays(Dictionary<int, PlayerScore> highestGroupScores, List<PlayerScore> playerScores, List<PlayerScore> playersForKnockout, IDataStore dataStore)
        {
            foreach (int groupId in highestGroupScores.Keys)
            {
                UpdateGroupArrays(highestGroupScores, groupId, playerScores, playersForKnockout, dataStore);
            }
        }

        private static void UpdateHighestScore(Dictionary<int, PlayerScore> highestGroupScores, PlayerScore playerScore)
        {
            if (highestGroupScores.ContainsKey(playerScore.groupId))
            {
                SetNewHighestScore(highestGroupScores, playerScore);
            }
            else
            {
                highestGroupScores.Add(playerScore.groupId, playerScore);
            }
        }

        private static void SetNewHighestScore(Dictionary<int, PlayerScore> highestGroupScores, PlayerScore playerScore)
        {
            if (ScoreIsHigherThanCurrentBest(playerScore, highestGroupScores))
            {
                highestGroupScores[playerScore.groupId] = playerScore;
            }
        }

        private static bool ScoreIsHigherThanCurrentBest(PlayerScore playerScore, Dictionary<int, PlayerScore> highestGroupScores)
        {
            return playerScore.score > highestGroupScores[playerScore.groupId].score;
        }

        private void AddPlayersToTournamentGroups(Player[] tournamentPlayers, List<Group> tournamentGroups)
        {
            int index = 0;
            foreach (Player player in tournamentPlayers)
            {
                tournamentGroups[index++].playersInGroup.Add(player);
                if (CanAddPlayersToTournamentGroup(tournamentGroups, index))
                {
                    index = 0;
                }
            }
        }

        private bool CanAddPlayersToTournamentGroup(List<Group> tournamentGroups, int index)
        {
            return index >= tournamentGroups.Count;
        }

        private List<Group> CreateTournamentGroupForPlayers(IDataStore datastore, Player[] tournamentPlayers, RoundTypes round)
        {
            List<Group> tournamentGroups = new List<Group>();
            int numberOfGroups = CalculateNumberOfGroups(tournamentPlayers);
            int name = 65;
            for (int i = 0; i < numberOfGroups; i++)
            {
                Group group = new Group();
                group.name = "Group " + (char)(name + i);
                group.id = datastore.CreateGroup(group);
                group.roundId = (int)round;
                tournamentGroups.Add(group);
            }

            return tournamentGroups;
        }


        private int CalculateNumberOfGroups(Player[] TournamentPlayers)
        {
            return Math.Max(1, (int)Math.Round((double)TournamentPlayers.Length / TARGET_GROUP_SIZE));
        }

        private static void StoreGames(IDataStore dataStore, List<Game> games, int tournamentId)
        {
            foreach (Game game in games)
            {
                dataStore.CreateGame(game, tournamentId);
            }
        }

        private int CalculateAmountOfPlayersThroughToNextRound(int numberOfGroups)
        {
            int amountOfPlayersThroughToNextRound = 0;
            amountOfPlayersThroughToNextRound = numberOfGroups * 2;
            if (CannotMakeBracketFromFirstAndSecondPlace(amountOfPlayersThroughToNextRound))
            {
                int logBaseTwo = (int)Math.Round(Math.Log(amountOfPlayersThroughToNextRound));
                amountOfPlayersThroughToNextRound = (int)Math.Pow(2, logBaseTwo + 1);
            }
            else
            {
                Math.Log(amountOfPlayersThroughToNextRound, 2);
            }
            return amountOfPlayersThroughToNextRound;
        }

        private static bool CannotMakeBracketFromFirstAndSecondPlace(int amountOfPlayersThroughToNextRound)
        {
            return Math.Log(amountOfPlayersThroughToNextRound, 2) % 1 != 0;
        }

        public Player[] PlayersThroughToNextRound(List<Group> groups, int numberOfGroups)
        {
            Player[] playersThroughToNextRound = new Player[CalculateAmountOfPlayersThroughToNextRound(numberOfGroups)];

            return playersThroughToNextRound;
        }

    }
}
