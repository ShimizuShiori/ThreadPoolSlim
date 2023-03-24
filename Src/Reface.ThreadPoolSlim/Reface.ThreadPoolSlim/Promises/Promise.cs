namespace Reface.ThreadPoolSlim.Promises
{
	public class Promise<T> : IPromise<T>
	{
		const byte STATE_PENDING = 1;
		const byte STATE_FULFILLED = 2;
		const byte STATE_REJECTED = 3;

		byte state;
		//readonly object StateLocker = new object();
		readonly ICollection<Action<Exception?>> errorHandlers;
		readonly ICollection<Action<T?>> thenHandlers;
		readonly ICollection<Action<T?, Exception?>> finallyHandlers;
		Exception? error;
		T? result;

		public Promise()
		{
			errorHandlers = new List<Action<Exception?>>();
			thenHandlers = new List<Action<T?>>();
			finallyHandlers = new List<Action<T?, Exception?>>();
			state = STATE_PENDING;
		}

		public Promise(Action<Action<T>, Action<Exception>> process) : this()
		{
			try
			{
				process(SetResult, SetException);
			}
			catch (Exception ex)
			{
				SetException(ex);
			}
		}

		public IPromise<T> Catch(Action<Exception?> errorHandler)
		{
			if (state != STATE_REJECTED)
			{
				lock (errorHandlers)
					errorHandlers.Add(errorHandler);
				return this;
			}

			errorHandler(error);
			return this;
		}

		public IPromise<T> Finally(Action<T?, Exception?> handler)
		{
			if (state == STATE_PENDING)
			{
				lock (finallyHandlers)
					finallyHandlers.Add(handler);
				return this;
			}

			handler(result, error);
			return this;
		}

		public IPromise<T> Then(Action<T?> thenHandler)
		{
			if (state != STATE_FULFILLED)
			{
				lock (thenHandlers)
					thenHandlers.Add(thenHandler);
				return this;
			}

			thenHandler(result);
			return this;
		}

		public IPromise<TNext> Then<TNext>(Func<T?, TNext?> thenHandler)
		{
			var promise = new Promise<TNext>();
			if (state != STATE_FULFILLED)
			{
				lock (thenHandlers)
					thenHandlers.Add(rlt =>
					{
						try
						{
							var nextRlt = thenHandler(rlt);
							promise.SetResult(nextRlt);
						}
						catch (Exception ex)
						{
							promise.SetException(ex);
						}
					});
				return promise;
			}

			try
			{
				var nextRlt = thenHandler(result);
				promise.SetResult(nextRlt);
			}
			catch (Exception ex)
			{
				promise.SetException(ex);
			}
			return promise;
		}

		void SetResult(T? value)
		{
			if (state != STATE_PENDING)
				return;

			result = value;
			state = STATE_FULFILLED;
			lock (thenHandlers)
				foreach (var handler in thenHandlers)
					handler(result);
		}

		void SetException(Exception? ex)
		{
			if (state != STATE_PENDING)
				return;

			error = ex;
			state = STATE_REJECTED;
			lock (errorHandlers)
				foreach (var handler in errorHandlers)
					handler(ex);
		}
	}
}
