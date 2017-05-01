using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAB.BatchServer.API.Models
{
    /// <summary>
    /// Represents a lab user entity
    /// </summary>
    public class LabUser
    {
        /// <summary>
        /// ID of the user
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LabUserId { get; set; }

        /// <summary>
        /// Email of the lab user
        /// </summary>
        [MaxLength(100)]
        public string EMail { get; set; }

        /// <summary>
        /// Full name of the lab user
        /// </summary>
        [MaxLength(50)]
        public string FullName { get; set; }

        /// <summary>
        /// Location of the lab user
        /// </summary>
        [MaxLength(50)]
        public string Location { get; set; }

        /// <summary>
        /// Company name of the lab user
        /// </summary>
        [MaxLength(50)]
        public string CompanyName { get; set; }

        /// <summary>
        /// Country code of the lab user
        /// </summary>
        [MaxLength(2)]
        public string CountryCode { get; set; }

        /// <summary>
        /// Team name of the lab user
        /// </summary>
        [MaxLength(100)]
        public string TeamName { get; set; }

        /// <summary>
        /// Creation date of the lab user
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTime ModifiedOn { get; set; }

    }
}
