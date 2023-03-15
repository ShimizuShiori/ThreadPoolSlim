# ThreadPoolSlim

## Background

*System.Threading.ThreadPool* is widely used in C# as a ThreadPool.

But *System.Threading.ThreadPool* is a static class.
If we want a ThreadPool with only 2 or 5 threads, *System.Threading.ThreadPool* couldn't help us.

So, I created this *ThreadPoolSlim*.
It allows you create a scoped ThreadPool, and set the properties as you need:
* CoreSize
* MaxSize
* MaxJobSize
* Keepalive
* FullHandler

**CoreSize**
If you have a ThreadPool with CoreSize=2,
And you have the jobs more than 2,
ThreadPool will start 2 threads to run your jobs.

**MaxJobSize**
If the Core-Threads are busy.
The new jobs will be enqueued into a queue to wait for the Core-Thread.
If the size of this queue greater than **MaxJobSize**, it will run some extension threads.

**MaxSize**
When the queue meets the **MaxJobSize**,
ThreadPool will start new extension threads to consume your jobs.
This **MaxSize** means the number of extension thread.
