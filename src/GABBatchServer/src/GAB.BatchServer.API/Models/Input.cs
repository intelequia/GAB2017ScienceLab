using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace GAB.BatchServer.API.Models
{
    /// <summary>
    /// Represents an Input entity
    /// </summary>
    public class Input
    {
        /// <summary>
        /// Possible status of an input
        /// </summary>
        public enum InputStatusEnum
        {
            /// <summary>
            /// Ready to be processed
            /// </summary>
            Ready = 0,
            /// <summary>
            /// Being processed
            /// </summary>
            Processing = 1,
            /// <summary>
            /// Already processed
            /// </summary>
            Processed = 2,
            /// <summary>
            /// Error while processing
            /// </summary>
            Error = 3
        }

        /// <summary>
        /// Id of the input
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InputId { get; set; }

        /// <summary>
        /// Parameters for the input (currently the filename)
        /// </summary>
        [MaxLength(800)]
        public string Parameters { get; set; }
        /// <summary>
        /// Status of the input
        /// </summary>
        [JsonIgnore]
        public InputStatusEnum Status { get; set; }
        /// <summary>
        /// Id of the batch that includes this input
        /// </summary>
        [JsonIgnore]
        public Guid? BatchId { get; set; }

        /// <summary>
        /// Assigned to Lab user
        /// </summary>
        [JsonIgnore]
        public LabUser AssignedTo { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        [JsonIgnore]
        public DateTime CreatedOn { get; set;  }

        /// <summary>
        /// Last modification date
        /// </summary>
        [JsonIgnore]
        public DateTime ModifiedOn { get; set; }
    }
}
