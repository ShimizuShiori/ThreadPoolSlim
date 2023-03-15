using System.Diagnostics;

namespace Reface.ThreadPoolSlim
{
	public class FixedThreadPoolSlim : IThreadPoolSlim
	{
		readonly object extThreadLock = new object();
		readonly object coreThreadLock = new object();
		readonly AutoResetEvent hasJobsEvent = new AutoResetEvent(false);
		readonly AutoResetEvent idleEvent = new AutoResetEvent(false);

		readonly int coreSize;
		readonly int maxSize;
		readonly Func<Thread> threadFactory;
		readonly TimeSpan keepAliveTime;
		readonly Queue<(Action<object>, object)> jobs;
		readonly int maxJobSize;
		readonly Action fullHandler;
		readonly string name;

		int runningCoreThreadCount = 0;
		int runningExtThreadCount = 0;
		int maxCoreId = 0;
		int maxExtId = 0;

		public FixedThreadPoolSlim(
			int coreSize,
			int maxSize,
			int maxJobSize,
			string? name = null,
			TimeSpan? keepAliveTime = null,
			Func<Thread>? threadFactory = null,
			Action? fullHandler = null)
		{
			this.coreSize = coreSize;
			this.maxSize = maxSize;
			this.maxJobSize = maxJobSize;
			this.jobs = new Queue<(Action<object>, object)>();
			this.name = name ?? "TheadPool";
			this.threadFactory = threadFactory ?? new Func<Thread>(() =>
			 {
				 var t = new Thread(DoJobs);
				 t.IsBackground = true;
				 t.Priority = ThreadPriority.Normal;
				 return t;
			 });
			this.fullHandler = fullHandler ?? new Action(() =>
			{
				throw new JobQueueIsFullException();
			});
			this.keepAliveTime = keepAliveTime ?? TimeSpan.FromSeconds(30);
		}

		Thread CreateThread(WorkerType workerType)
		{
			var thread = this.threadFactory();
			int id = workerType == WorkerType.Core
				? Interlocked.Increment(ref this.maxCoreId)
				: Interlocked.Increment(ref this.maxExtId);
			thread.Name = $"{name}-{workerType}-{id}";
			return thread;
		}

		public void Submit(Action<object> action, object args)
		{
			Debug.WriteLine($"Args={args}");
			bool enqueued = false;
			lock (jobs)
			{
				Debug.WriteLine($"jobs.Count={jobs.Count}, maxJobSize={maxJobSize}");
				if (jobs.Count < this.maxJobSize)
				{
					jobs.Enqueue((action, args));
					hasJobsEvent.Set();
					enqueued = true;
					Debug.WriteLine("Queued");
				}
			}
			lock (coreThreadLock)
			{
				Debug.WriteLine($"runningCoreThreadCount={runningCoreThreadCount}, coreSize={coreSize}");
				if (runningCoreThreadCount < this.coreSize)
				{
					Action callback = () =>
					{
						ReduceRunningCount(WorkerType.Core);

					};
					CreateThread(WorkerType.Core).Start(callback);
					Interlocked.Increment(ref runningCoreThreadCount);
					Debug.WriteLine("Core Thread Created");
					return;
				}
			}
			if (enqueued)
				return;

			lock (jobs)
				lock (extThreadLock)
				{
					Debug.WriteLine($"runningExtThreadCount={runningExtThreadCount}, maxSize={maxSize}");
					if (runningExtThreadCount < this.maxSize)
					{
						Action callback = () =>
						{
							ReduceRunningCount(WorkerType.Ext);
						};
						jobs.Enqueue((action, args));
						CreateThread(WorkerType.Ext).Start(callback);
						Interlocked.Increment(ref runningExtThreadCount);
						Debug.WriteLine("Ext Thread Created");
						return;
					}
				}
			Debug.WriteLine("Calling fullHandler()");
			fullHandler();
		}

		public void WaitIdle()
		{
			this.idleEvent.WaitOne();
		}

		public void Dispose()
		{
			hasJobsEvent.Dispose();
			idleEvent.Dispose();
		}
		void DoJobs(object? obj)
		{
			Debug.WriteLine($"[{Thread.CurrentThread.Name}] Started");
			bool waitForJobs = false;
			while (true)
			{
				if (waitForJobs)
				{
					if (!hasJobsEvent.WaitOne(this.keepAliveTime))
						break;
					waitForJobs = false;
				}

				(Action<object>, object) job;
				lock (jobs)
				{
					if (jobs.Count == 0)
					{
						waitForJobs = true;
						continue;
					}
					else
						job = jobs.Dequeue();
				}


				Debug.WriteLine($"[{Thread.CurrentThread.Name}] Doing Job");
				job.Item1(job.Item2);
				Debug.WriteLine($"[{Thread.CurrentThread.Name}] Finished Job");
				Thread.Sleep(0);
			}
			Debug.WriteLine($"[{Thread.CurrentThread.Name}] Dead");
			if (obj is Action callback)
				callback();
		}

		void ReduceRunningCount(WorkerType type)
		{
			int runningCount = 0;
			switch (type)
			{
				case WorkerType.Core:
					runningCount = Interlocked.Decrement(ref runningCoreThreadCount) + runningExtThreadCount;
					break;
				case WorkerType.Ext:
					runningCount = Interlocked.Decrement(ref runningExtThreadCount) + runningCoreThreadCount;
					break;
				default:
					break;
			}
			if (runningCount == 0)
				idleEvent.Set();
		}
	}
}
