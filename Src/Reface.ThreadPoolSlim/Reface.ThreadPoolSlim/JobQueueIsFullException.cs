using System.Runtime.Serialization;

namespace Reface.ThreadPoolSlim
{
	public class JobQueueIsFullException : Exception
	{
		public JobQueueIsFullException()
		{
		}

		public JobQueueIsFullException(string? message) : base(message)
		{
		}

		public JobQueueIsFullException(string? message, Exception? innerException) : base(message, innerException)
		{
		}

		protected JobQueueIsFullException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
