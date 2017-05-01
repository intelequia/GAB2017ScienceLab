using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GAB.BatchServer.API.Common
{
    internal class EventHubs
    {
        private static List<EventHubClient> _eventHubPool;
        private static List<EventHubClient> EventHubPool(IConfiguration configuration)
        {
            if (_eventHubPool == null || _eventHubPool.Count == 0)
            {                
                var maxPoolSize = int.Parse(configuration["BatchServer:EventHubPoolSize"]);
                var connectionStringBuilder =
                    new EventHubsConnectionStringBuilder(configuration.GetConnectionString("EventHub"))
                    {
                        EntityPath = configuration["BatchServer:EventHubName"]
                    };
                _eventHubPool = new List<EventHubClient>();
                for (var index = 0; index < maxPoolSize; index++)
                {
                    var eventHubClient =
                        Microsoft.Azure.EventHubs.EventHubClient.CreateFromConnectionString(
                            connectionStringBuilder.ToString());
                    _eventHubPool.Add(eventHubClient);
                }
            }
            return _eventHubPool;
        }

        internal static EventHubClient EventHubClient(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.GetConnectionString("EventHub"))
                || string.IsNullOrEmpty(configuration["BatchServer:EventHubName"])) return null;
            var index = new Random().Next(int.Parse(configuration["BatchServer:EventHubPoolSize"]) - 1);
            return EventHubPool(configuration)[index];
        }

        internal static void Initialize(IConfiguration configuration, ILogger logger)
        {
            logger.LogInformation("Initializing event hub pool");
            var client = EventHubClient(configuration);
            if (client == null)
            {
                logger.LogWarning("EventHub is disabled. Check application settings");
            }
            else
            {
                logger.LogInformation($"Event hub pool successfully initialzed (Pool size: {_eventHubPool.Count})");
            }
        }
        
    }
}
