using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TournamentAPI.Inputs;
using Microsoft.AspNetCore.Authorization;
using TournamentAPI.Responses.Tournament;

namespace TournamentAPI.Controllers
{
    [Route("[controller]")]
    public class ScoresController : Controller
    {
        private AppSettings _settings;
        public ScoresController(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        [HttpPut]
        [Authorize]
        public Response PutGameScores([FromBody] PutScoresInput scores)
        {
            var response = new Response();

            if (scores == null)
            {
                AddErrorToResponse(response, "Invalid score inputs! Please try again!");
                return response;
            }

            if (DoValidationOnInt(response, scores.tournamentId < 1, "Tournament ID is not valid.") ||
                DoValidationOnInt(response, scores.groupId < 1, "Group ID is not valid.") ||
                DoValidationOnInt(response, scores.player1Id < 1, "Player 1 ID is not valid.") ||
                DoValidationOnInt(response, scores.player2Id < 1, "Player 2 ID is not valid.") ||
                DoValidationOnInt(response, scores.player1Score < 0, "Player 1 score is not valid.") ||
                DoValidationOnInt(response, scores.player2Score < 0, "Player 2 score is not valid."))
            {
                return response;
            }

            try
            {
                var connectionString = _settings.TournamentDB;
                using (var dataStore = new DataStore(new SqlConnection(connectionString)))
                {
                    var userId = dataStore.GetPlayerIdFromUsername(Utils.GetUserName(User));
                    dataStore.SetScores(userId, scores.tournamentId, scores.groupId, scores.player1Id, scores.player2Id, scores.player1Score, scores.player2Score);
                    if (dataStore.IsRoundCompleted(scores.tournamentId))
                    {
                        StartNextRound(scores, dataStore);
                    }
                    dataStore.Commit();
                }

            }
            catch (Exception e)
            {
                AddErrorToResponse(response, e.Message);
            }

            return response;
        }

        private static void StartNextRound(PutScoresInput scores, DataStore dataStore)
        {
            var currentRound = dataStore.GetCurrentRound(scores.tournamentId);
            if (currentRound == (int) RoundTypes.GROUP)
            {
                TournamentGenerator.CreateKnockoutStageGames(dataStore, dataStore.GetTournament(scores.tournamentId));
            }
            else
            {
                var winners = GetWinnersOfCurrentRound(scores, dataStore, currentRound);
                if (dataStore.GetCurrentRound(scores.tournamentId) != (int) RoundTypes.FINALS)
                {
                    TournamentGenerator.CreateKnockoutStageGames(dataStore, dataStore.GetTournament(scores.tournamentId), winners);
                }
            }
        }

        private static List<PlayerScore> GetWinnersOfCurrentRound(PutScoresInput scores, DataStore dataStore, int currentRound)
        {
            var winners = new List<PlayerScore>();
            var games = dataStore.GetTournamentGames(scores.tournamentId, (RoundTypes) currentRound);
            foreach (TournamentGames game in games)
            {
                AddWinner(game, winners);
            }
            return winners;
        }

        private static void AddWinner(TournamentGames game, List<PlayerScore> winners)
        {
            // Knockout games are best of 3, so draws are not possible
            if (Player1Wins(game))
            {
                winners.Add(
                    new PlayerScore() {id = game.player1Id, groupId = game.groupId}
                );
            }
            else
            {
                winners.Add(
                    new PlayerScore() {id = game.player2Id, groupId = game.groupId}
                );
            }
        }

        private static bool Player1Wins(TournamentGames game)
        {
            return game.player1Score > game.player2Score;
        }

        private static bool DoValidationOnInt(Response response, bool expression, string errorText)
        {
            if (expression)
            {
                AddErrorToResponse(response, errorText);
                return true;
            }
            return false;
        }

        private static void AddErrorToResponse(Response response, string message)
        {
            response.HasErrors = true;
            response.Errors.Add(message);
        }
    }
}