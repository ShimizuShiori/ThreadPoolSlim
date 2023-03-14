namespace Reface.ThreadPoolSlim
{
	public interface IThreadPoolSlim
	{
		void Submit(Action<object> action, object arg);
	}
}
