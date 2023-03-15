namespace Reface.ThreadPoolSlim
{
	public interface IThreadPoolSlim : IDisposable
	{
		void Submit(Action<object> action, object arg);

		void WaitIdle();
	}
}
