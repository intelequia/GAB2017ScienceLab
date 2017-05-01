using System;

namespace GAB.BatchServer.API.Exceptions
{
    /// <summary>
    /// Exception for output parsing errors
    /// </summary>
    public class OutputParsingException: Exception
    {
        /// <summary>
        /// Constructor for the OutputParsingException
        /// </summary>
        /// <param name="message"></param>
        public OutputParsingException(string message) : base(message)
        {            
        }
}
}
