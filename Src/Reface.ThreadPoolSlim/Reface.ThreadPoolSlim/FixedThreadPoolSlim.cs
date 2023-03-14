namespace Reface.ThreadPoolSlim
{
	public class FixedThreadPoolSlim : IThreadPoolSlim
	{
		readonly int coreSize;
		readonly int maxSize;
		readonly Func<Thread> threadFactory;
		readonly TimeSpan keepAliveTime;
		readonly Queue<(Action<object>, object)> jobs;
		readonly int maxJobSize;
		readonly ICollection<Thread> coreThread = new List<Thread>();
		readonly ICollection<Thread> extThread = new List<Thread>();
		readonly AutoResetEvent hasJobsEvent = new AutoResetEvent(false);
		readonly Action fullHandler;

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
				t.Start();
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
			bool enqueued = false;
			lock (jobs)
			{
				if (jobs.Count < this.maxJobSize)
				{
					jobs.Enqueue((action, args));
					hasJobsEvent.Set();
					enqueued = true;
				}
			}
			lock (coreThread)
			{
				if (coreThread.Count < this.coreSize)
				{
					var t = threadFactory();
					coreThread.Add(t);
					return;
				}
			}
			lock (extThread)
			{
				if (extThread.Count < this.maxSize)
				{
					var t = threadFactory();
					extThread.Add(t);
					return;
				}
			}
			if (enqueued)
				return;

			fullHandler();
		}

		void DoJobs(object? obj)
		{
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



				job.Item1(job.Item2);
				Thread.Sleep(0);
			}
		}
	}
}
