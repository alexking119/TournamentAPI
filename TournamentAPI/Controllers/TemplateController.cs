using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TournamentAPI.Responses.Template;

namespace TournamentAPI.Controllers
{
    [Route("[controller]")]
    public class TemplateController : Controller
    {
        private AppSettings _settings;

        public TemplateController(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Gets all templates stored in the database
        /// </summary>
        /// <returns>Returns list of templates</returns>
        [HttpGet]
        public GetTemplatesResponse GetTemplates()
        {
            var connectionString = _settings.TournamentDB;
            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                GetTemplatesResponse response = new GetTemplatesResponse();
                try
                {
                    response.Templates = dataStore.ListTemplates();
                }
                catch (Exception e)
                {
                    response.HasErrors = true;
                    response.Errors.Add(e.Message);
                }
                return response;
            }
        }

        /// <summary>
        /// Gets a template with the specified ID stored in the database
        /// </summary>
        /// <param name="id">ID of the template to retrieve</param>
        /// <returns>Returns the requested template</returns>
        [HttpGet]
        [Route("{id}")]
        public GetTemplateResponse GetTemplateFromId(int id)
        {
            var connectionString = _settings.TournamentDB;

            using (var dataStore = new DataStore(new SqlConnection(connectionString)))
            {

                GetTemplateResponse response = new GetTemplateResponse();
                try
                {
                    response.Template = dataStore.GetTemplate(id);
                }
                catch (Exception e) { 
                    AddErrorToResponse(response, "ID is not valid.");
                    AddErrorToResponse(response, e.Message);
                }
                return response;
            }
        }
        private static void AddErrorToResponse(Response response, string message)
        {
            response.HasErrors = true;
            response.Errors.Add(message);
        }

    }
}