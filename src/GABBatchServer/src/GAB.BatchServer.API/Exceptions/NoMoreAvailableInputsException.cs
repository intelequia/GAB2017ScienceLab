using System;
namespace GAB.BatchServer.API.Exceptions
{
    /// <summary>
    /// Exception for no more available inputs 
    /// </summary>
    public class NoMoreAvailableInputsException: Exception
    {
        /// <summary>
        /// Constructor for the NoMoreAvailableInputsException
        /// </summary>
        /// <param name="message"></param>
        public NoMoreAvailableInputsException(string message) : base(message)
        {            
        }
    }
}
