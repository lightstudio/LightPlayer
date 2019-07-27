using System;

namespace Light.Managed.Feedback
{
    /// <summary>
    /// Exception for throttled requsets.
    /// </summary>
    /// <remarks>Do not record this exception: it is used to represent a server status.</remarks>
    public class RequestThrottledException : Exception
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        public RequestThrottledException() : base("The client has sent too many requests in a specific time range.")
        {
            
        }
    }
}
