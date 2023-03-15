namespace Reface.ThreadPoolSlim
{
	public interface IPipelineHandlerContext
	{
		Type DataType { get; }
		object Data { get; }
		IPipeline Pipeline { get; }
		IThreadPoolSlim ThreadPool { get; }
	}
	public class PipelineHandlerContext : IPipelineHandlerContext
	{
		public Type DataType { get; }

		public object Data { get; }

		public IPipeline Pipeline { get; }

		public IThreadPoolSlim ThreadPool { get; }

		public PipelineHandlerContext(Type dataType, object data, IPipeline pipeline, IThreadPoolSlim threadPool)
		{
			DataType = dataType;
			Data = data;
			Pipeline = pipeline;
			ThreadPool = threadPool;
		}
	}
	public class PipelineBuilder
	{
		readonly Dictionary<Type, HandlerInfo> typeToHandlerMap = new Dictionary<Type, HandlerInfo>();

		Func<Type, IThreadPoolSlim>? threadPoolFactory;

		public PipelineBuilder RegisterHandler(
			Type type,
			Action<IPipelineHandlerContext> action,
			IThreadPoolSlim? threadPool = null)
		{
			if (threadPool == null)
			{
				if (threadPoolFactory == null)
					throw new ArgumentNullException(nameof(threadPool));
				threadPool = threadPoolFactory(type);
			}

			typeToHandlerMap[type] = new HandlerInfo(threadPool, action);
			return this;
		}

		public PipelineBuilder RegisterHandler<T>(Action<IPipelineHandlerContext> action, IThreadPoolSlim? threadPool = null)
		{
			return this.RegisterHandler(typeof(T), action, threadPool);
		}

		public PipelineBuilder DefaultThreadPool(Func<Type, IThreadPoolSlim> factory)
		{
			this.threadPoolFactory = factory;
			return this;
		}
		public IPipeline Build()
		{
			return new DefultPipeline(typeToHandlerMap);
		}
	}
}
