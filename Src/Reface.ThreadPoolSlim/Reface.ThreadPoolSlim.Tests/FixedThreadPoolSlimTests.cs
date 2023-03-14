using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Reface.ThreadPoolSlim.Tests
{
	[TestClass]
	public class FixedThreadPoolSlimTests
	{
		[TestMethod]
		public void Submit_ShouldExecutedInDifferentThread_WhenJobsLessThanCoreSize()
		{
			FixedThreadPoolSlim threadPool = new FixedThreadPoolSlim(2, 5, 10);
			string threadName1 = string.Empty;
			string threadName2 = string.Empty;

			CountdownEvent e = new CountdownEvent(2);
			threadPool.Submit(s =>
			{
				threadName1 =
Thread.CurrentThread.Name ?? "";
				e.Signal();
			}, 1);
			threadPool.Submit(s =>
			{
				threadName2 =
Thread.CurrentThread.Name ?? "";
				e.Signal();
			}, 2);

			e.Wait();
			Assert.AreNotEqual(threadName1, threadName2);
		}

		[TestMethod]
		[DataRow(3)]
		[DataRow(4)]
		[DataRow(5)]
		[DataRow(6)]
		[DataRow(7)]
		[DataRow(8)]
		[DataRow(9)]
		public void Submit_ShouldExecutedInTwoThread_WhenJobsGreaterThanCoreSize(int workerCount)
		{
			FixedThreadPoolSlim threadPool = new FixedThreadPoolSlim(2, 5, 10);
			HashSet<string> threadNames = new HashSet<string>();

			CountdownEvent e = new CountdownEvent(workerCount);
			for (int i = 0; i < workerCount; i++)
				threadPool.Submit(s =>
				{
					Thread.Sleep(10);
					lock (threadNames)
						threadNames.Add(Thread.CurrentThread.Name ?? "");
					e.Signal();
				}, 1);

			e.Wait();
			Assert.AreEqual(2, threadNames.Count);
		}


		[TestMethod]
		public void Submit_ShouldExecutedInThreeThread_WhenJobsGreaterThanMaxSize()
		{
			FixedThreadPoolSlim threadPool = new FixedThreadPoolSlim(1, 2, 5);
			HashSet<string> threadNames = new HashSet<string>();

			int workerCount = 6;
			CountdownEvent e = new CountdownEvent(workerCount);
			for (int i = 0; i < workerCount; i++)
				threadPool.Submit(s =>
				{
					Thread.Sleep(10);
					lock (threadNames)
						threadNames.Add(Thread.CurrentThread.Name ?? "");
					e.Signal();
				}, 1);

			e.Wait();
			Assert.AreEqual(2, threadNames.Count);
		}

		[TestMethod]
		public void Submit_ShouldThrowException_WhenJobsIsTooMuch()
		{
			FixedThreadPoolSlim threadPool = new FixedThreadPoolSlim(1, 2, 5);
			HashSet<string> threadNames = new HashSet<string>();

			int safeWorker = 8;
			int unsafeWorker = 2;
			CountdownEvent e = new CountdownEvent(safeWorker + unsafeWorker);
			for (int i = 0; i < safeWorker; i++)
				threadPool.Submit(s =>
				{
					Thread.Sleep(1000);
					lock (threadNames)
						threadNames.Add(Thread.CurrentThread.Name ?? "");
					e.Signal();
				}, i);
			for (int i = 0; i < unsafeWorker; i++)
			{
				Assert.ThrowsException<JobQueueIsFullException>(() =>
				{
					threadPool.Submit(s =>
					{
						Thread.Sleep(1000);
						lock (threadNames)
							threadNames.Add(Thread.CurrentThread.Name ?? "");

					}, i * 100);
				});
				e.Signal();
			}

			e.Wait();
		}

		[TestMethod]
		public void Submit_ShouldFinishAllJobs()
		{
			FixedThreadPoolSlim pool = new FixedThreadPoolSlim(1, 10, 1000);
			int workerCount = 1000;
			int result = 0;
			CountdownEvent e = new CountdownEvent(workerCount);
			for (int i = 0; i < workerCount; i++)
				pool.Submit(s =>
				{
					Interlocked.Increment(ref result);
					e.Signal();
				}, null);
			e.Wait();
			Assert.AreEqual(1000, result);

		}
	}
}