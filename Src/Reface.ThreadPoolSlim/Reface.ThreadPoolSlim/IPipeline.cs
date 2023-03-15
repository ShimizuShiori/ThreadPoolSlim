namespace Reface.ThreadPoolSlim
{
	public interface IPipeline : IDisposable
	{
		void WaitIdle();
		void Submit(object data);
	}
}
