using System.Diagnostics;

namespace Reface.ThreadPoolSlim
{
	public class FixedThreadPoolSlim : IThreadPoolSlim
	{
		readonly object extThreadLock = new object();
		readonly object coreThreadLock = new object();
		readonly AutoResetEvent hasJobsEvent = new AutoResetEvent(false);

		readonly int coreSize;
		readonly int maxSize;
		readonly Func<Thread> threadFactory;
		readonly TimeSpan keepAliveTime;
		readonly Queue<(Action<object>, object)> jobs;
		readonly int maxJobSize;
		readonly Action fullHandler;

		int runningCoreThreadCount = 0;
		int runningExtThreadCount = 0;
		int maxId = 0;

		public FixedThreadPoolSlim(
			int coreSize,
			int maxSize,
			int maxJobSize,
			TimeSpan? keepAliveTime = null,
			Func<Thread>? threadFactory = null,
			Action? fullHandler = null)
		{
			this.coreSize = coreSize;
			this.maxSize = maxSize;
			this.maxJobSize = maxJobSize;
			this.jobs = new Queue<(Action<object>, object)>();
			this.threadFactory = threadFactory ?? new Func<Thread>(() =>
			 {
				 var id = Interlocked.Increment(ref maxId);
				 var t = new Thread(DoJobs);
				 t.Name = $"worker-{id}";
				 return t;
			 });
			this.fullHandler = fullHandler ?? new Action(() =>
			{
				throw new JobQueueIsFullException();
			});
			this.keepAliveTime = keepAliveTime ?? TimeSpan.FromSeconds(30);
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
						lock (coreThreadLock)
							runningCoreThreadCount--;
					};
					threadFactory().Start(callback);
					runningCoreThreadCount++;
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
							lock (extThreadLock)
								runningExtThreadCount--;
						};
						jobs.Enqueue((action, args));
						var t = threadFactory();
						t.Name = "ext-" + t.Name;
						t.Start(callback);
						runningExtThreadCount++;
						Debug.WriteLine("Ext Thread Created");
						return;
					}
				}
			Debug.WriteLine("Calling fullHandler()");
			fullHandler();
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
	}
}
