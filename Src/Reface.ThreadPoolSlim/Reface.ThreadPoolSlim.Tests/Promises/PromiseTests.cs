using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reface.ThreadPoolSlim.Promises;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reface.ThreadPoolSlim.Promises.Tests
{
	[TestClass()]
	public class PromiseTests
	{
		[TestInitialize]
		public void Init()
		{
			@event = new AutoResetEvent(false);
		}

		[TestCleanup]
		public void Dispose()
		{
			@event.Dispose();
		}

		[TestMethod()]
		public void TestThen_InSubThread()
		{
			string? str = null;
			var p = new Promise<string>((resolve, reject) =>
			{
				RunInSubThread(() => resolve("10"));
			});
			p.Then(result =>
			{
				str = result;
				@event.Set();
			});
			@event.WaitOne();
			Assert.AreEqual("10", str);
		}

		[TestMethod]
		public void TestCatch_WhenRejectAnException()
		{
			Exception? ex = null;
			var p = new Promise<string>((resolve, reject) =>
			{
				RunInSubThread(() => reject(new ApplicationException()));
			});
			p.Catch(e =>
			{
				ex = e;
				@event.Set();
			});
			@event.WaitOne();
			Assert.IsInstanceOfType(ex, typeof(ApplicationException));
		}

		[TestMethod]
		public void TestThen_InSameThread()
		{
			string str = "";
			new Promise<string>((resolve, reject) => resolve("A"))
				.Then(result => { str = result ?? ""; });
			Assert.AreEqual("A", str);
		}

		[TestMethod]
		public void TestResolve_Ignored_WhenHasBeenRejected()
		{
			string? str = null;
			Exception? ex = null;
			var p = new Promise<string>((resolve, reject) =>
			{
				reject(new ApplicationException());
				resolve("A");
			});
			p.Then(x =>
			{
				str = x;
			}).Catch(e =>
			{
				ex = e;
			});
			Assert.IsNull(str);
			Assert.IsInstanceOfType(ex, typeof(ApplicationException));
		}

		[TestMethod]
		public void TestReject_Ingored_WhenHasBeenResolved()
		{
			string? str = null;
			Exception? ex = null;
			var p = new Promise<string>((resolve, reject) =>
			{
				resolve("A");
				reject(new ApplicationException());
			});
			p.Then(x =>
			{
				str = x;
			}).Catch(e =>
			{
				ex = e;
			});
			Assert.IsNull(ex);
			Assert.AreEqual("A", str);
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void TestThen_MultiThens(bool async)
		{
			void ResolveData(Action<string> resolve)
			{
				resolve("A");
				@event.Set();
			}

			var process = new Action<Action<string>, Action<Exception>>((resolve, reject) =>
			{
				if (async)
					RunInSubThread(() => ResolveData(resolve));
				else
					ResolveData(resolve);
			});

			List<string> list = new List<string>();
			IPromise<string> promise = new Promise<string>(process);
			promise.Then(x => list.Add(x));
			promise.Then(x => list.Add(x));
			promise.Then(x => list.Add(x));
			promise.Then(x => list.Add(x));
			@event.WaitOne();
			Assert.AreEqual(4, list.Count);
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void TestThen_ChainedThens(bool async)
		{
			void ResolveData(Action<string> resolve)
			{
				resolve("A");
				@event.Set();
			}

			var process = new Action<Action<string>, Action<Exception>>((resolve, reject) =>
			{
				if (async)
					RunInSubThread(() => ResolveData(resolve));
				else
					ResolveData(resolve);
			});

			List<string> list = new List<string>();
			IPromise<string> promise = new Promise<string>(process);
			promise
				.Then(x => list.Add(x))
				.Then(x => list.Add(x))
				.Then(x => list.Add(x))
				.Then(x => list.Add(x));
			@event.WaitOne();
			Assert.AreEqual(4, list.Count);
		}

		AutoResetEvent @event;

		void RunInSubThread(Action action)
		{
			ThreadPool.QueueUserWorkItem(state => action(), null);
		}
	}
}