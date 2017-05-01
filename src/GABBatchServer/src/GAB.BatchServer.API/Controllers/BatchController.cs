using System;
using System.Collections.Generic;
using GAB.BatchServer.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GAB.BatchServer.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GAB.BatchServer.API.Exceptions;
using Microsoft.Extensions.Logging;
using GAB.BatchServer.API.Common;
using GAB.BatchServer.API.Common.Lab;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;

namespace GAB.BatchServer.API.Controllers
{
    /// <summary>
    /// API Controller for downloading and uploading GAB Science Lab tasks
    /// </summary>
    [Route("api/[controller]")]
    public class BatchController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly BatchServerContext _context;
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        /// <summary>
        /// Constructor for the BatchController class
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public BatchController(IConfiguration configuration, BatchServerContext context, ILoggerFactory logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger.CreateLogger("GAB.BatchServer.API.Controllers");
        }

        #region GetNewBatch
        /// <summary>
        /// Gets a new batch of tasks to process by a GAB Science Lab client
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        /// <param name="email">Email of the user deploying the lab</param>
        /// <param name="fullName">Full name of the user deploying the lab</param>
        /// <param name="teamName">Team name of the user deploying the lab</param>
        /// <param name="companyName">Company name of the user deploying the lab</param>
        /// <param name="location">Location of the user deploying the lab</param>
        /// <param name="countryCode">Country of the user deploying the lab</param>
        /// <returns>A batch of tasks to be processed</returns>
        /// <response code="200">Returns the assigned batch items to process</response>
        /// <response code="400">If any of the parameters is invalid</response>
        // GET api/GetNewBatch?batchSize=100&email=john@doe.com&fullName=johndoe&teamName=myTeam&location=91&countryCode=ES
        [Route("GetNewBatch")]
        [HttpGet]
        [ProducesResponseType(typeof(GetNewBatchResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> GetNewBatch([Required] int batchSize, [Required] string email, [Required] string fullName, [Required] string teamName,
            [Required] string companyName,
            [Required] string location, [Required] string countryCode)
        {
            try
            {
                _logger.LogInformation(LoggingEvents.GET_NEW_BATCH,
                    "Getting new batch with size {batchSize} and email {email}", batchSize, email);

                // Validations
                if (batchSize == 0 || batchSize > _configuration.GetValue<int>("BatchServer:MaxBatchSize"))
                    return GetBadRequest($"Invalid batch size {batchSize}");
                if (string.IsNullOrEmpty(email) || email.Length > 100)
                    return GetBadRequest("Parameter email is invalid");
                if (string.IsNullOrEmpty(fullName) || fullName.Length > 50)
                    return GetBadRequest("Parameter fullName is invalid");
                if (string.IsNullOrEmpty(companyName) || companyName.Length > 50)
                    return GetBadRequest("Parameter companyName is invalid");
                if (string.IsNullOrEmpty(location) || location.Length > 50)
                    return GetBadRequest("Parameter location is required");
                if (string.IsNullOrEmpty(countryCode) || countryCode.Length > 2 ||
                    !Countries.IsoCodes.ContainsKey(countryCode.ToUpperInvariant()))
                    return GetBadRequest("Parameter countryCode is not valid");

                // Trim parameters
                email = email.Trim().ToLowerInvariant();
                fullName = fullName.Trim();
                teamName = teamName.Trim();
                location = location.Trim();
                companyName = companyName.Trim();
                countryCode = countryCode.ToUpperInvariant();

                // Find the user
                var user = await GetOrCreateLabUserAsync(email, fullName, teamName, location, countryCode, companyName);

                // TODO Check if we allow more than X pending batches per user to avoid abuse?

                // Return the batch
                var batchId = Guid.NewGuid();
                var batchCollection = await AssignNewBatchAsync(batchId, batchSize, user);

                _telemetry.TrackEvent("BatchDelivered");

                return
                    Ok(new GetNewBatchResult
                    {
                        BatchId = batchId,
                        Inputs = batchCollection
                    });
            }
            catch (NoMoreAvailableInputsException ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogCritical(LoggingEvents.GET_NEW_BATCH, ex, "No more available inputs");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.GET_NEW_BATCH_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
        }

        private async Task<List<Input>> AssignNewBatchAsync(Guid batchId, int batchSize, LabUser user)
        {
            _logger.LogInformation("Assigning batch {batchId} (size {batchSize}) to user {email}", batchId, batchSize,
                user.EMail);
            var minInputId = _configuration.GetValue<int>("BatchServer:MinInputId");
            var rows = await _context.Database.ExecuteSqlCommandAsync(
                $"UPDATE dbo.Inputs SET BatchId='{batchId}', AssignedToLabUserId={user.LabUserId}, Status=1, ModifiedOn=GETUTCDATE() " + 
                "WHERE InputId IN " +
                    $"(SELECT TOP {batchSize} InputId FROM dbo.Inputs WITH (UPDLOCK, ROWLOCK, READPAST) " + 
                    $"WHERE Status=0 and InputId>{minInputId})");
            var batchCollection = await (from i in _context.Inputs
                                         where i.BatchId == batchId
                                         select i).ToListAsync();
            if (batchCollection.Count == 0)
            {
                throw new NoMoreAvailableInputsException("There are no more available inputs to process on this location");
            }

            // Generate the input Uris
            foreach (var input in batchCollection)
            {
                input.Parameters = Storage.InputsContainer.GetBlobReference(input.Parameters).Uri.ToString();
            }
            return batchCollection;
        }

        private async Task<LabUser> GetOrCreateLabUserAsync(string email, string fullName, string teamName, string location, string countryCode, string companyName)
        {
            var user = await _context.LabUsers.FirstOrDefaultAsync(u => u.EMail == email);
            if (user == null)
            {
                _logger.LogInformation("Creating user {email}", email);
                // Create the user if does not exist
                user = new LabUser
                {
                    EMail = email,
                    FullName = fullName,
                    TeamName = teamName,
                    Location = location,
                    CountryCode = countryCode,
                    CompanyName = companyName
                };
                _context.LabUsers.Add(user);
                await _context.SaveChangesAsync();
            }
            else // Check if the parameters are different from the stored ones
            {
                if (user.FullName != fullName || user.TeamName != teamName || user.CompanyName != companyName ||
                    user.Location != location || user.CountryCode != countryCode)
                {
                    _logger.LogInformation("Updating user {email}", email);
                    user.FullName = fullName;
                    user.TeamName = teamName;
                    user.CompanyName = companyName;
                    user.Location = location;
                    user.CountryCode = countryCode;
                    _context.LabUsers.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            return user;
        }

        #endregion

        #region UpdateBatchResult
        /// <summary>
        /// Uploads the result of a batch process
        /// </summary>
        /// <remarks>
        /// The inputId and email parameter must match the ones used during the GetNewBatch call
        /// </remarks>
        /// <param name="inputId">The id of the input to update</param>
        /// <param name="email">The email of the user that requested the input</param>
        /// <param name="result">The result of the batch process</param>
        /// <returns></returns>
        /// <response code="200">Returns the id of the processed ouput</response>
        /// <response code="400">If any of the parameters is invalid</response>
        [Route("UploadOutput")]
        [HttpPost]
        [ProducesResponseType(typeof(UploadOutputResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> UploadOutput([Required] int inputId, [Required] string email, [FromBody, Required] OutputContent result)
        {
            try
            {
                _logger.LogInformation(LoggingEvents.UPLOAD_OUTPUT,
                    "Uploading result for input {inputId} and email {email}", inputId, email);

                // Validations
                if (string.IsNullOrEmpty(email))
                    return GetBadRequest("Parameter email is required");
                if (string.IsNullOrEmpty(result?.Content))
                    return GetBadRequest("Parameter result is required");
                email = email.Trim();

                // Get the input
                var input = await _context.Inputs.FirstOrDefaultAsync(i => i.InputId == inputId);
                if (input == null)
                    return GetNotFound($"The input {inputId} was not found");

                // Get the user
                var user = await _context.LabUsers.FirstOrDefaultAsync(u => u.EMail == email);
                if (user == null)
                    return GetNotFound($"The user {email} was not found");

                // Security check
                if (input.AssignedTo?.LabUserId != user.LabUserId)
                {
                    return GetForbid($"The user {email} can't update the input {inputId} result value");
                }

                // Parse the output
                var parsedOutput = await OutputParser.ParseAsync(result.Content);

                // Upload the result to blob storage
                var blobName = await UploadOutputResultToStorageAsync(result.Content, input);

                // Update the output status
                var tuple = await UpdateOuputAsync(blobName, input, parsedOutput);
                var output = tuple.Item1;
                var firstUpdate = tuple.Item2;

                _telemetry.TrackEvent("OutputUploaded");

                // Notify event hub, but only if was the first update to avoid dashboard hacks
                if (firstUpdate)
                {
                    await SendEventHubNotificationAsync(input, user, output, parsedOutput);
                }                

                return Ok(new UploadOutputResult {OutputId = output.OutputId});
            }
            catch (OutputParsingException ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.UPLOAD_OUTPUT_SERVER_ERROR, ex, "Output parse error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.UPLOAD_OUTPUT_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
        }

        private async Task<string> UploadOutputResultToStorageAsync(string result, Input input)
        {
            var blobName = $"{input.Parameters}.out";
            var blob = Storage.OutputsContainer.GetBlockBlobReference(blobName);
            _logger.LogInformation($"Uploading the result for input {input.InputId} to {blob.Uri}");
            await Storage.UploadOutputAsync(_configuration, blob, result);
            return blobName;
        }

        private async Task<Tuple<Output, bool>> UpdateOuputAsync(string result, Input input, SeligaParsedOutput parsedOutput)
        {
            _logger.LogInformation("Updating input {inputId} to processed state", input.InputId);
            input.ModifiedOn = DateTime.UtcNow;
            input.Status = Input.InputStatusEnum.Processed;
            _context.Inputs.Update(input);

            var firstUpdate = false;
            var output = await _context.Outputs.FirstOrDefaultAsync(o => o.Input.InputId == input.InputId);
            if (output == null)
            {
                _logger.LogInformation("Creating output for input {inputId}", input.InputId);
                output = new Output
                {
                    Input = input,
                    Result = result,
                    TotalItems = parsedOutput.Outputs.Count,
                    MaxScore = parsedOutput.MaxScore,
                    AvgScore = parsedOutput.AvgScore,
                    TotalScore = parsedOutput.TotalScore,
                };
                 _context.Outputs.Add(output);
                firstUpdate = true;
            }
            else
            {
                _logger.LogWarning("Updating output {outputId} for input {inputId}", output.OutputId, input.InputId);
                output.Result = result;
                output.TotalItems = parsedOutput.Outputs.Count;
                output.MaxScore = parsedOutput.MaxScore;
                output.AvgScore = parsedOutput.AvgScore;
                output.TotalScore = parsedOutput.TotalScore;
                output.ModifiedOn = DateTime.UtcNow;
                _context.Outputs.Update(output);
            }
            await _context.SaveChangesAsync();
            return new Tuple<Output, bool>(output, firstUpdate);
        }

        private async Task SendEventHubNotificationAsync(Input input, LabUser user, Output output, SeligaParsedOutput seligaOutput)
        {
            var client = EventHubs.EventHubClient(_configuration);

            var messageData = new
            {
                inputId = input.InputId,
                batchId = input.BatchId,
                outputId = output.OutputId,
                deploymentId = _configuration["BatchServer:DeploymentId"],
                user = new
                {
                    email = user.EMail,
                    fullName = user.FullName,
                    location = user.Location,
                    teamName = user.TeamName,
                    companyName = user.CompanyName,
                    countryCode = user.CountryCode
                },
                score = new {
                    totalItems = seligaOutput.Outputs.Count,
                    maxScore = seligaOutput.MaxScore,
                    avgScore = seligaOutput.AvgScore,
                    totalScore = seligaOutput.TotalScore
                }
            };

            var message = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(messageData));

            if (client == null)
            {
                _logger.LogWarning("Event hubs disabled: {message}", message);                
            }
            else
            {
                await client.SendAsync(new Microsoft.Azure.EventHubs.EventData(Encoding.UTF8.GetBytes(message)));
            }
        }

        #endregion


        #region Logging

        private BadRequestObjectResult GetBadRequest(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return BadRequest(message);
        }
        private NotFoundObjectResult GetNotFound(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return NotFound(message);
        }

        private ForbidResult GetForbid(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return Forbid(message);
        }

        #endregion
    }
}
