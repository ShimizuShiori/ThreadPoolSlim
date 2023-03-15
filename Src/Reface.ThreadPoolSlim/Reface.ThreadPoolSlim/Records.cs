namespace Reface.ThreadPoolSlim
{
	public record HandlerInfo(IThreadPoolSlim ThreadPool, Action<IPipelineHandlerContext> Action);
}