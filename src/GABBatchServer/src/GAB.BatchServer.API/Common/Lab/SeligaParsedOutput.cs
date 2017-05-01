using System.Collections.Generic;

namespace GAB.BatchServer.API.Common.Lab
{
    /// <summary>
    /// Represents the parsed output of a Seliga file
    /// </summary>
    public class SeligaParsedOutput
    {
        /// <summary>
        /// Constructor of the SeligaParsedOutput class
        /// </summary>
        public SeligaParsedOutput()
        {
            Outputs = new List<SeligaOutput>();
        }
        /// <summary>
        /// List of outputs in the file
        /// </summary>
        public List<SeligaOutput> Outputs{ get; set; }        
        /// <summary>
        /// Max score obtained
        /// </summary>
        public double MaxScore { get; set; }
        /// <summary>
        /// Average score
        /// </summary>
        public double AvgScore { get; set; }
        /// <summary>
        ///  Total score
        /// </summary>
        public double TotalScore { get; set; }
    }
}
