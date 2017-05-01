using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAB.BatchServer.API.Models
{
    /// <summary>
    /// Represents an output entity
    /// </summary>
    public class Output
    {
        /// <summary>
        /// Id of the output
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OutputId { get; set; }

        /// <summary>
        /// Associated input for the output
        /// </summary>
        public Input Input { get; set; }

        /// <summary>
        /// Content of the output (currently the filename)
        /// </summary>
        [MaxLength(512)]        
        public string Result { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Total items on the output file
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Max score obtained
        /// </summary>
        public double MaxScore { get; set; }

        /// <summary>
        /// Average score obtained
        /// </summary>
        public double AvgScore { get; set; }

        /// <summary>
        /// Total score obtained
        /// </summary>
        public double TotalScore { get; set; }
    }
}
