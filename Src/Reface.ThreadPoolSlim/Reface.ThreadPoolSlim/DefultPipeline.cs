using System.Diagnostics;

namespace Reface.ThreadPoolSlim
{
	public class DefultPipeline : IPipeline
	{
		readonly IDictionary<Type, HandlerInfo> typeToHandlerMap;

		public DefultPipeline(IDictionary<Type, HandlerInfo> typeToHandlerMap)
		{
			this.typeToHandlerMap = typeToHandlerMap;
		}

		public void Dispose()
		{
			foreach (var value in typeToHandlerMap.Values)
				value.ThreadPool.Dispose();
		}

		public void Submit(object data)
		{
			if (!typeToHandlerMap.TryGetValue(data.GetType(), out var handler))
				return;

			Debug.WriteLine($"Found handler for {data.GetType()}");

			handler.ThreadPool.Submit(state =>
			{
				IPipelineHandlerContext context = (IPipelineHandlerContext)state;
				handler.Action(context);
			}, new PipelineHandlerContext(
				data.GetType(),
				data,
				this,
				handler.ThreadPool));
		}

		public void WaitIdle()
		{
			foreach (var handler in typeToHandlerMap.Values)
			{
				handler.ThreadPool.WaitIdle();
			}
		}
	}
}
