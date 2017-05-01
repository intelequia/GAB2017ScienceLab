using System.Collections.Generic;
using System.Linq;
using GAB.BatchServer.API.Models;
using Microsoft.Extensions.Logging;

namespace GAB.BatchServer.API.Data
{
    /// <summary>
    /// Database initializer
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Initalizes the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="seedData"></param>
        public static void Initialize(BatchServerContext context, ILogger logger, bool seedData)
        {
            logger.LogInformation("Initializing database");
            context.Database.EnsureCreated();

            if (!seedData) return;

            // Look for any input
            if (context.Inputs.Any())
            {
                logger.LogInformation("Database already initialized. Skipping...");
                return; // The database has been seeded
            }

            logger.LogInformation("Seeding database with default data");
            // Test inputs
            var inputs = new List<Input>();
            for (var i = 1; i < 1000; i++)
            {
                inputs.Add(new Input {Status = Input.InputStatusEnum.Ready, Parameters = $"input{i}.txt"});
            }
            context.Inputs.AddRange(inputs);
            context.SaveChanges();
        }
    }
}
