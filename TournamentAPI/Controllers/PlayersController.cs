using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TournamentAPI.Responses.Players;
using TournamentAPI.Responses.Tournament;

namespace TournamentAPI.Controllers
{
    [Route("[controller]")]
    public class PlayersController : Controller
    {
        private AppSettings _settings;
        public PlayersController(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Adds player from the User credentials of the browser session.
        /// </summary>
        [HttpPost]
        [Route("Player")]
        [Authorize]
        public PostPlayerResponse AddPlayer([FromBody] Player player)
        {
            var response = new PostPlayerResponse();
            var username = Utils.GetUserName(User);

            if (DoValidationOnString(response, player.firstName, "You must enter a first name.") ||
               DoValidationOnString(response, player.surname, "You must enter a surname.") ||
               DoValidationOnString(response, player.email, "You must enter an email.") ||
               DoValidationOnString(response, username, "Username was not loaded from browser")
               )
            {
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    player.username = username;
                    response.NewId = dataStore.CreatePlayer(player);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
                dataStore.Commit();
            }

            return response;
        }


        [HttpPut]
        [Route("Player")]
        [Authorize]
        public PutPlayerResponse UpdatePlayerDetails([FromBody] Player player)
        {
            var username = Utils.GetUserName(User);
            var response = new PutPlayerResponse();

            if (DoValidationOnString(response, player.firstName, "You must enter a first name.") ||
               DoValidationOnString(response, player.surname, "You must enter a surname.") ||
               DoValidationOnString(response, player.email, "You must enter an email.") ||
               DoValidationOnString(response, username, "Username was not loaded from browser")
               )
            {
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                try
                {
                    int playerId = dataStore.GetPlayerIdFromUsername(username);

                    response.UpdatedPlayer = dataStore.UpdatePlayer(player, playerId);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
            }

            return response;
        }

        [HttpPost]
        [Route("TournamentPlayers/{tournamentId}")]
        [Authorize]
        public PostPlayerTournamentResponse AddPlayerToTournament(int tournamentId)
        {
            var username = Utils.GetUserName(User);
            var response = new PostPlayerTournamentResponse();
            
            if (tournamentId < 1)
            {
                AddErrorToResponse(response, "Tournament ID is not valid.");
                return response;
            }
            if (DoValidationOnString(response, username, "Username was not loaded from browser"))
            {
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                try
                {
                    var playerId = dataStore.GetPlayerIdFromUsername(username);

                    var playerTournaments = dataStore.GetPlayerTournaments(playerId);
                    if (playerTournaments.Contains(tournamentId))
                    {
                        AddErrorToResponse(response, "Player already entered in tournament!");
                        return response;
                    }

                    dataStore.CreateTournamentPlayer(playerId, tournamentId);
                    response.PlayerId = playerId;
                    response.TournamentId = tournamentId;

                    dataStore.Commit();
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
            }

            return response;
        }

        [HttpDelete]
        [Route("TournamentPlayers/{tournamentId}")]
        [Authorize]
        public PostPlayerTournamentResponse RemovePlayerFromTournament(int tournamentId)
        {
            var username = Utils.GetUserName(User);
            var response = new PostPlayerTournamentResponse();

            if (tournamentId < 1)
            {
                AddErrorToResponse(response, "Tournament ID is not valid.");
                return response;
            }
            if (DoValidationOnString(response, username, "Username was not loaded from browser"))
            {
                return response;
            }

            try
            {
                var connectionString = _settings.TournamentDB;
                using (var dataStore = new DataStore(new SqlConnection(connectionString)))
                {

                    var playerId = dataStore.GetPlayerIdFromUsername(username);

                    var playerTournaments = dataStore.GetPlayerTournaments(playerId);
                    if (!playerTournaments.Contains(tournamentId))
                    {
                        AddErrorToResponse(response, "Player is not in this tournament yet!");
                        return response;
                    }

                    response.PlayerId = dataStore.RemoveTournamentPlayer(playerId, tournamentId);
                    response.TournamentId = tournamentId;

                    dataStore.Commit();
                }
            }
            catch (Exception e)
            {
                AddErrorToResponse(response, e.Message);
            }

            return response;
        }

        [HttpGet]
        [Route("PlayerId")]
        [Authorize]
        public GetPlayerIdResponse GetPlayerId()
        {
            var username = Utils.GetUserName(User);

            var response = new GetPlayerIdResponse();

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    if (username != null)
                    {
                        response.PlayerId = dataStore.GetPlayerIdFromUsername(username);
                    }
                    else
                    {
                        response.HasErrors = true;
                        response.Errors.Add("Could not get username from API Get call");
                    }
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
            }
            return response;
        }

        [HttpGet]
        [Route("PlayerName/{playerId}")]
        [Authorize]
        public GetPlayerNameResponse GetPlayerName(int playerId)
        {
            var response = new GetPlayerNameResponse();

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    if (playerId != null)
                    {
                        response.PlayerName = dataStore.GetPlayerNameFromId(playerId);
                    }
                    else
                    {
                        response.HasErrors = true;
                        response.Errors.Add("Could not get ID from API Get call");
                    }
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
            }
            return response;
        }

        [HttpGet]
        [Route("IsPlayerInTournament")]
        [Authorize]
        public GetPlayerIsInTournament IsPlayerInTournament(int tournamentId)
        {
            var username = Utils.GetUserName(User);


            var response = new GetPlayerIsInTournament();

            if (tournamentId < 1)
            {
                AddErrorToResponse(response, "Tournament ID is not valid.");
                return response;
            }
            if (DoValidationOnString(response, username, "Username was not loaded from browser"))
            {
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                try
                {
                    var playerId = dataStore.GetPlayerIdFromUsername(username);

                    var playerTournaments = dataStore.GetPlayerTournaments(playerId);
                    response.IsInTournament = playerTournaments.Contains(tournamentId);
                }
                catch (Exception e)
                {
                    response.HasErrors = true;
                    response.Errors.Add(e.Message);
                }
            }
            return response;
        }

        [HttpGet]
        [Route("TournamentParticipants/{tournamentId}")]
        public GetTournamentParticipantsResponse GetTournamentParticipants(int tournamentId)
        {
            var response = new GetTournamentParticipantsResponse();

            if (tournamentId < 1)
            {
                AddErrorToResponse(response, "Tournament ID is not valid.");
                return response;
            }

            try
            {
                var connectionString = _settings.TournamentDB;
                using (var dataStore = new DataStore(new SqlConnection(connectionString)))
                {

                    var participants = dataStore.ListParticipantsForTournament(tournamentId);
                    response.Participants = participants;

                }
            }
            catch (Exception e)
            {
                AddErrorToResponse(response, e.Message);
            }

            return response;
        }

        [HttpGet]
        [Route("IsUserKnown")]
        [Authorize]
        public IsUserKnownResponse IsUserInDatabase()
        {
            var username = Utils.GetUserName(User);
            var response = new IsUserKnownResponse();

            var connectionString = _settings.TournamentDB;

            try
            {
                using (var ds = new DataStore(new SqlConnection(connectionString)))
                {
                    response.IsUserKnown = ds.IsUserKnown(username);
                }
            }
            catch (Exception e)
            {
                AddErrorToResponse(response, e.Message);
            }
            return response;
        }

        private static void AddErrorToResponse(Response response, string message)
        {
            response.HasErrors = true;
            response.Errors.Add(message);
        }

        private bool DoValidationOnString(Response response, string field, string errorText)
        {
            if (String.IsNullOrEmpty(field))
            {
                AddErrorToResponse(response, errorText);
                return true;
            }
            return false;
        }
    }
}