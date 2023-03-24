namespace Reface.ThreadPoolSlim.Promises
{
	public interface IPromise<T>
	{
		IPromise<T> Then(Action<T?> thenHandler);
		IPromise<TNext> Then<TNext>(Func<T?, TNext?> thenHandler);
		IPromise<T> Catch(Action<Exception?> errorHandler);
		IPromise<T> Finally(Action<T?, Exception?> handler);
	}
}
