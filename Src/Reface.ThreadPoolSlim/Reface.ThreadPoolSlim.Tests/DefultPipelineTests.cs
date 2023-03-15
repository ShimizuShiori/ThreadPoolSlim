using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Reface.ThreadPoolSlim.Tests
{
	[TestClass()]
	public class DefultPipelineTests
	{
		[TestMethod()]
		public void SubmitPlus()
		{
			int i = 0;
			using (IPipeline pipeline = new PipelineBuilder()
					.RegisterHandler(
					typeof(int),
					d =>
					{
						Interlocked.Add(ref i, (int)d.Data);
					},
					new FixedThreadPoolSlim(2, 100, 1000, keepAliveTime: TimeSpan.FromSeconds(3)))
					.Build())
			{

				pipeline.Submit(1);
				pipeline.Submit(2);
				pipeline.Submit(3);
				pipeline.Submit(4);

				pipeline.WaitIdle();

				Assert.AreEqual(10, i);
			}
		}

		[TestMethod()]
		public void Submit_MoreThatTwoTypes()
		{
			List<string> list = new List<string>();
			using (IPipeline pipeline = new PipelineBuilder()
					.DefaultThreadPool(type => new FixedThreadPoolSlim(
						2,
						100,
						1000,
						keepAliveTime: TimeSpan.FromSeconds(3),
						name: $"{type.FullName}"))
					.RegisterHandler<int>(c => c.Pipeline.Submit(c.Data.ToString() ?? ""))
					.RegisterHandler<string>(c =>
					{
						lock (list)
							list.Add((string)c.Data);
					})
					.Build())
			{

				pipeline.Submit(1);
				pipeline.Submit(2);
				pipeline.Submit(3);
				pipeline.Submit(4);

				pipeline.WaitIdle();

				Assert.AreEqual(4, list.Count);
			}
		}
	}
}