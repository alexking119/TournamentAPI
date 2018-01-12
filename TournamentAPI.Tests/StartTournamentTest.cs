using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TournamentAPI.Tests.DataStoreTests;

namespace TournamentAPI.Tests
{
    [TestClass]
    public class StartTournamentTest
    {
        private IDataStore _dataStore;

        [TestInitialize]
        public void Setup()
        {
            var reader = new DataReaderMock();
            _dataStore = new DataStore(new SqlConnectionMock(0, reader));
        }
        [TestMethod]
        public void CreateGroupsWithTargetNumberOfPlayers()
        {
            var players = new Player[] {

                new Player(), new Player(), new Player(), new Player() 
            };

            var startTournament = new TournamentGeneratorMock();

            List<Group> actualGroups = startTournament.CreateGroups(_dataStore, players, 0);
            Assert.AreEqual(1, actualGroups.Count, "The number of groups created were not correct.");
            Assert.AreEqual(4, actualGroups[0].playersInGroup.Count, "The size of the group created was not correct.");
        }

        [TestMethod]
        public void CreateGroupsDoubleTargetPlayers()
        {
            var players = new Player[] {
                new Player(), new Player(), new Player(), new Player(), new Player(), new Player(), new Player(), new Player()
            };

            var startTournament = new TournamentGeneratorMock();

            List<Group> actualGroups = startTournament.CreateGroups(_dataStore, players, 0);
            Assert.AreEqual(2, actualGroups.Count, "The number of groups created were not correct.");
            Assert.AreEqual(4, actualGroups[0].playersInGroup.Count, "The size of the group created was not correct.");
        }

        [TestMethod]
        public void CreateGroupsPlayersDontDivideEvenly()
        {
            var players = new Player[] {
                new Player(), new Player(), new Player(), new Player(), new Player(), new Player(), new Player(), new Player(), new Player()
            };

            var startTournament = new TournamentGeneratorMock();

            List<Group> actualGroups = startTournament.CreateGroups(_dataStore, players, 0);
            Assert.AreEqual(2, actualGroups.Count, "The number of groups created were not correct.");
            Assert.AreEqual(5, actualGroups[0].playersInGroup.Count, "The size of the first group created was not correct.");
            Assert.AreEqual(4, actualGroups[1].playersInGroup.Count, "The size of the second group created was not correct.");
        }

        [TestMethod]
        public void CreateGamesFromGroupOfFour()
        {
            var players = new Group();
            var playersInGroup = new List<Player>()
            {
                new Player() { id = 1 },
                new Player() { id = 2 },
                new Player() { id = 3 },
                new Player() { id = 4 }
            };

            players.playersInGroup = playersInGroup;

            var startTournament = new TournamentGeneratorMock();

            var games = startTournament.CreateGamesFromGroup(players);

            int expectedNumberOfGames = CalculateExpectedNumberOfGames(players.playersInGroup);

            var playerIdsPlayed = CalculatePlayersPlayedAgainst(players.playersInGroup, games);

            Assert.AreEqual(expectedNumberOfGames, games.Count, "The correct number of games was not generated!");
            foreach (Player player in players.playersInGroup) {
                foreach (Player playerToCompare in players.playersInGroup)
                {
                    if (player.id == playerToCompare.id)
                    {
                        continue;
                    }
                    Assert.IsTrue(playerIdsPlayed.Contains(playerToCompare.id), "Player id " + player.id + " did not play id " + playerToCompare.id);
                }
            }
        }

        [TestMethod]
        public void CreateGamesFromGroupOfFive()
        {
            var players = new Group();
            var playersInGroup = new List<Player>()
            {
                new Player() { id = 1 },
                new Player() { id = 2 },
                new Player() { id = 3 },
                new Player() { id = 4 },
                new Player() { id = 5 }
            };

            players.playersInGroup = playersInGroup;

            var startTournament = new TournamentGeneratorMock();

            var games = startTournament.CreateGamesFromGroup(players);

            int expectedNumberOfGames = CalculateExpectedNumberOfGames(players.playersInGroup);

            var playerIdsPlayed = CalculatePlayersPlayedAgainst(players.playersInGroup, games);

            Assert.AreEqual(expectedNumberOfGames, games.Count, "The correct number of games was not generated!");
            foreach (Player player in players.playersInGroup)
            {
                foreach (Player playerToCompare in players.playersInGroup)
                {
                    if (player.id == playerToCompare.id)
                    {
                        continue;
                    }
                    Assert.IsTrue(playerIdsPlayed.Contains(playerToCompare.id), "Player id " + player.id + " did not play id " + playerToCompare.id);
                }
            }
        }

        [TestMethod]
        public void CreateGamesFromGroupOfThree()
        {
            var players = new Group();
            var playersInGroup = new List<Player>()
            {
                new Player() { id = 1 },
                new Player() { id = 2 },
                new Player() { id = 3 },
            };

            players.playersInGroup = playersInGroup;

            var tournament = new TournamentGeneratorMock();
            var games = tournament.CreateGamesFromGroup(players);

            int expectedNumberOfGames = CalculateExpectedNumberOfGames(players.playersInGroup);

            var playerIdsPlayed = CalculatePlayersPlayedAgainst(players.playersInGroup, games);

            Assert.AreEqual(expectedNumberOfGames, games.Count, "The correct number of games was not generated!");
            foreach (Player player in players.playersInGroup)
            {
                foreach (Player playerToCompare in players.playersInGroup)
                {
                    if (player.id == playerToCompare.id)
                    {
                        continue;
                    }
                    Assert.IsTrue(playerIdsPlayed.Contains(playerToCompare.id), "Player id " + player.id + " did not play id " + playerToCompare.id);
                }
            }
        }

        private static List<int> CalculatePlayersPlayedAgainst(List<Player> players, List<Game> games)
        {
            List<int> playerIdsPlayed = new List<int>();
            foreach (Player player in players)
            {
                foreach (Game game in games)
                {
                    if (ShouldAddPlayerId(player, game.Player1, playerIdsPlayed))
                    {
                        playerIdsPlayed.Add(game.Player1.id);
                    }
                    else if (ShouldAddPlayerId(player, game.Player2, playerIdsPlayed))
                    {
                        playerIdsPlayed.Add(game.Player2.id);
                    }
                }
            }
            return playerIdsPlayed;
        }

        private static int CalculateExpectedNumberOfGames(List<Player> players)
        {
            int expectedNumberOfGames = (int) (((double)players.Count / 2) * (players.Count - 1));
            return expectedNumberOfGames;
        }

        private static bool ShouldAddPlayerId(Player player, Player playerToCompare, List<int> playerIdsPlayed)
        {
            return playerToCompare.id != player.id && !playerIdsPlayed.Contains(playerToCompare.id);
        }
    }
}
