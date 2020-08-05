using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace EventSystem
{
    static class Time
    {
        public static double UnixEpoch
        {
            get
            {
                return (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
            }
        }
    }
    public class EventSystem : IDisposable
    {
        public event Action OnTick;
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private ConcurrentQueue<Task<dynamic>> tasks = new ConcurrentQueue<Task<dynamic>>();
        public readonly Thread InnerThread;
        private double lastTick = Time.UnixEpoch;
        public double TimerMS;
        public EventSystem(double delay = 1000 / 60)
        {
            TimerMS = delay;
            InnerThread = new Thread(InnerLoop);
        }
        public void Start()
        {
            InnerThread.Start();
        }
        public void Wait()
        {
            InnerThread.Join();
        }
        public void Cancel()
        {
            cancellation.Cancel();
        }
        public async Task Enqueue(Action func)
        {
            Task<dynamic> task = new Task<dynamic>(() => { func(); return null; });
            tasks.Enqueue(task);
            await task;
        }
        public async Task Enqueue(Action func, CancellationTokenSource tokenSource)
        {
            Task<dynamic> task = new Task<dynamic>(() => { func(); return null; }, tokenSource.Token);
            tasks.Enqueue(task);
            await task;
        }
        public async Task<T> Enqueue<T>(Func<T> func)
        {
            Task<dynamic> task = new Task<dynamic>(() => { return func(); });
            tasks.Enqueue(task);
            return await task;
        }
        public async Task<T> Enqueue<T>(Func<T> func, CancellationTokenSource tokenSource)
        {
            Task<dynamic> task = new Task<dynamic>(() => { return func(); }, tokenSource.Token);
            tasks.Enqueue(task);
            return await task;
        }
        private void InnerLoop()
        {
            while (!cancellation.IsCancellationRequested)
            {
                double elapsed = Time.UnixEpoch - lastTick;
                if (elapsed >= TimerMS)
                {
                    if (tasks.Count > 0 && tasks.TryDequeue(out Task<dynamic> task))
                    {
                        if (!task.IsCanceled)
                            task.Start();
                    }
                    InvokeOnTick();
                    lastTick = Time.UnixEpoch;
                }
            }
            tasks.Clear();
        }
        public void Dispose()
        {
            Cancel();
        }
        private void InvokeOnTick()
        {
            if (OnTick != null) OnTick.Invoke();
        }
    }
}
