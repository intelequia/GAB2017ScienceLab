namespace GAB.BatchServer.API.Common
{
    /// <summary>
    /// Constants for logging events
    /// </summary>
    public class LoggingEvents
    {
        /// <summary>
        /// Get new batch event
        /// </summary>
        public const int GET_NEW_BATCH = 100;
        /// <summary>
        /// Error obtaining a new batch event
        /// </summary>
        public const int GET_NEW_BATCH_SERVER_ERROR = 199;
        /// <summary>
        /// Upload output event
        /// </summary>
        public const int UPLOAD_OUTPUT = 200;
        /// <summary>
        /// Error uploading output event
        /// </summary>
        public const int UPLOAD_OUTPUT_SERVER_ERROR = 299;
        /// <summary>
        /// New Guid event
        /// </summary>
        public const int NEW_GUID = 300;
        /// <summary>
        /// Error creating new guid event
        /// </summary>
        public const int NEW_GUID_SERVER_ERROR = 399;
    }
}
