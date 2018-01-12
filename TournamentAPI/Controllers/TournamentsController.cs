using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using TournamentAPI.Responses.Tournament;

namespace TournamentAPI.Controllers
{
    [Route("[controller]")]
    public class TournamentsController : Controller
    {
        private AppSettings _settings;
        private DataStore _dataStore;

        public TournamentsController(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;

            //DataStore dataStore
            //_dataStore = dataStore;
        }
        /// <summary>
        /// List all tournaments
        /// </summary>
        /// <returns>Returns TournamentArrayResponse object containing list of tournaments and any errors</returns>
        [HttpGet]
        [Authorize]
        public GetTournamentArrayResponse Get()
        {
            var username = Utils.GetUserName(User);
            GetTournamentArrayResponse response = new GetTournamentArrayResponse();
            if (String.IsNullOrEmpty(username))
            {
                AddErrorToResponse(response, "username was not found");
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    response.Tournaments = dataStore.ListTournaments();

                    var playerId = dataStore.GetPlayerIdFromUsername(username);
                    response.playerTournaments = dataStore.GetPlayerTournaments(playerId);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }


        /// <summary>
        /// Gets a tournament with the specified id
        /// </summary>
        /// <param name="id"> unique identifier for each tournament, type int</param>
        /// <returns>Returns TournamentResponse object containing tournament and any errors</returns>
        [HttpGet]
        [Route("HasStarted/{id}")]
        public HasTournamentStartedResponse HasTournamentStarted(int id)
        {
            HasTournamentStartedResponse response = new HasTournamentStartedResponse();
            if (id < 1)
            {
                AddErrorToResponse(response, "ID is not valid.");
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    response.HasStarted = dataStore.HasTournamentStarted(id);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
                return response;
            }
        }

        /// <summary>
        /// Gets a tournament with the specified id
        /// </summary>
        /// <param name="id"> unique identifier for each tournament, type int</param>
        /// <returns>Returns TournamentResponse object containing tournament and any errors</returns>
        [HttpGet]
        [Route("{id}")]
        public GetTournamentResponse Get(int id)
        {
            GetTournamentResponse response = new GetTournamentResponse();
            if (id < 1)
            {
                AddErrorToResponse(response, "ID is not valid.");
                return response;
            }
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    response.Tournament = dataStore.GetTournament(id);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tournamentId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("AllGames/{tournamentId}")]
        public GetTournamentGamesResponse GetTournamentGames(int tournamentId)
        {
            GetTournamentGamesResponse response = new GetTournamentGamesResponse();
            if (tournamentId < 1)
            {
                AddErrorToResponse(response, "Tournament ID is not valid.");
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                try
                {
                    response.games = dataStore.GetTournamentGames(tournamentId);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }

        [HttpGet]
        [Route("GroupGames/{tournamentId}")]
        public GetTournamentGamesResponse GetTournamentGroupGames(int tournamentId)
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                GetTournamentGamesResponse response = new GetTournamentGamesResponse();

                try
                {
                    response.games = dataStore.GetTournamentGames(tournamentId, RoundTypes.GROUP);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }

        [HttpGet]
        [Route("PlayerScores/{tournamentId}")]
        public GetPlayerScoresResponse GetPlayerScores(int tournamentId)
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                GetPlayerScoresResponse response = new GetPlayerScoresResponse();

                try
                {
                    response.playerScores = dataStore.GetPlayerScores(tournamentId);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }

        [HttpGet]
        [Route("KnockoutGames/{tournamentId}")]
        public GetTournamentGamesResponse GetTournamentKnockoutGames(int tournamentId)
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                GetTournamentGamesResponse response = new GetTournamentGamesResponse();

                try
                {
                    response.games = dataStore.GetTournamentGames(tournamentId, RoundTypes.KNOCKOUT_ANY);
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }

                return response;
            }
        }

        /// <summary>
        /// Generates groups and matches for a tournament
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Generate/{id}")]
        public Response GenerateTournament(int id)
        {
            Response response = new Response();
            if (id < 1)
            {
                AddErrorToResponse(response, "ID is not valid.");
                return response;
            }

            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                if (dataStore.HasTournamentStarted(id))
                {
                    AddErrorToResponse(response, "Tournament has already been started!");
                    return response;
                }

                var tournament = dataStore.GetTournament(id);
                if (DateTime.Compare(DateTime.Now, tournament.startDate) < 0) // If before start date
                {
                    AddErrorToResponse(response, "Cannot start tournament before start date!");
                    return response;
                }

                dataStore.UpdateTournamentState(id);
                try
                {
                    TournamentGenerator startTournament = new TournamentGenerator();
                    startTournament.Create(dataStore, id);

                    dataStore.Commit();
                }
                catch (Exception e)
                {
                    AddErrorToResponse(response, e.Message);
                }
                return response;
            }
        }

        //[HttpGet]
        //public BracketMatchupResponse getBracketMatchups

        public class GenerateRequest
        {
            public int id;
        }

        /// <summary>
        /// Creates and adds new tournament
        /// </summary>
        /// <param name="value">Tournament object to add</param>
        [HttpPost]
        public PostTournamentResponse PostTournament([FromBody] Tournament value)
        {
            PostTournamentResponse response = new PostTournamentResponse();
            if (DoValidationOnExpression(response, value == null, "Tournament not valid - Ensure no fields are blank!") ||
                DoValidationOnExpression(response, String.IsNullOrEmpty(value.name), "The name is not valid.") ||
                DoValidationOnExpression(response, value.organiser < 1, "The organiser is not valid.") ||
                DoValidationOnExpression(response, value.startDate == null, "The start date is not valid.") ||
                DoValidationOnExpression(response, value.templateId < 1, "The template ID is not valid.") ||
                DoValidationOnExpression(response, String.IsNullOrEmpty(value.description), "The description is not valid.") ||
                DoValidationOnExpression(response, value.endDate == null, "The end date is not valid.") ||
                DoValidationOnExpression(response, value.startDate >= value.endDate, "Start date must be before than end date!"))
            {
                return response;
            }


            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                response.NewId = dataStore.CreateTournament(value);
                dataStore.Commit();
                return response;
            }
        }

        [HttpGet]
        [Route("Groups/{tournamentId}")]
        public ListGroupsResponse GetListOfGroupsInTournament(int tournamentId)
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                ListGroupsResponse response = new ListGroupsResponse();
                response.GroupsInTournament = dataStore.GetListOfGroupsInTournament(tournamentId);
                dataStore.Commit();
                return response;
            }
        }

        [HttpGet]
        [Route("Groups/GroupStages/{tournamentId}")]
        public ListGroupsResponse GetListOfGroupsInGroupStage(int tournamentId)
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {
                ListGroupsResponse response = new ListGroupsResponse();
                response.GroupsInTournament = dataStore.GetListOfGroupsInTournament(tournamentId, RoundTypes.GROUP);
                dataStore.Commit();
                return response;
            }
        }

        private static void AddErrorToResponse(Response response, String message)
        {
            response.HasErrors = true;
            response.Errors.Add(message);
        }

        private static bool DoValidationOnExpression(Response response, bool expression, string errorText)
        {
            if (expression)
            {
                AddErrorToResponse(response, errorText);
                return true;
            }
            return false;
        }
    }
}