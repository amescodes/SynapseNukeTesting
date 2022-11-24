using System;

namespace Synapse
{
    public class SynapseException : Exception
    {
        public string Message { get; } = "";
        public Exception InnerException { get; }

        public SynapseException(string message)
        {
            Message = message;
        }

        public SynapseException(string message, Exception innerException)
        {
            Message = message;
            InnerException = innerException;
        }
    }
}
