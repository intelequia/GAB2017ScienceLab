using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GAB.BatchServer.API.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace GAB.BatchServer.API.Controllers
{
    /// <summary>
    /// API Controller for Resource Manager Templates
    /// </summary>
    [Route("api/[controller]")]
    public class TemplatesController : Controller
    {
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        /// <summary>
        /// Constructor for the Templates controller
        /// </summary>
        /// <param name="logger"></param>
        public TemplatesController(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("GAB.BatchServer.API.Controllers");
        }

        #region NewGuid
        /// <summary>
        /// Generates a new Resource Manager template with Guids
        /// </summary>
        /// <param name="numberOfGuids">Number of guids to generate</param>
        /// <returns></returns>
        [Route("NewGuid")]
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> NewGuid(int numberOfGuids = 1)
        {
            try
            {
                // Validations
                if (numberOfGuids == 0 || numberOfGuids > 1000)
                    return GetBadRequest($"Invalid number of guids {numberOfGuids}");

                Response.ContentType = "application/json";
                var template = @"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"", ""parameters"": {}, ""variables"": {}, ""resources"": [],
  ""outputs"": {[OUTPUTS]}
}
";
                
                var outputs = await Task.Run<List<string>>(() =>
                {
                    var o = new List<string>();
                    for (var i = 0; i < numberOfGuids; i++)
                    {
                        o.Add(@"""guid" + i + @""": { ""type"": ""string"", ""value"": """ + Guid.NewGuid() + @""" }");
                    }
                    return o;
                });

                var result = template.Replace("[OUTPUTS]", string.Join(",", outputs.ToArray()));
                return
                    Ok(result);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.NEW_GUID_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
        }



        #endregion

        #region Private
        private BadRequestObjectResult GetBadRequest(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return BadRequest(message);
        }
        #endregion  
    }
}
