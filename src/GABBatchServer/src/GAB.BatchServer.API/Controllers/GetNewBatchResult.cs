using System;
using System.Collections.Generic;
using GAB.BatchServer.API.Models;

namespace GAB.BatchServer.API.Controllers
{
    /// <summary>
    /// Represents the ouput of the GetNewBatch method
    /// </summary>
    public class GetNewBatchResult
    {
        /// <summary>
        /// The batch Id
        /// </summary>
        public Guid BatchId { get; set; }
        /// <summary>
        /// The list of inputs of this batch
        /// </summary>
        public List<Input> Inputs { get; set; }
    }
}
