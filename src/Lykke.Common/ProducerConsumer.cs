﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common.Log;

namespace Common
{
    public abstract class ProducerConsumer<T> : IStartable, IStopable where T : class
    {
        private readonly object _startStopLockobject = new object();
        private readonly Queue<TaskCompletionSource<T>> _queue = new Queue<TaskCompletionSource<T>>();
        private readonly string _metricName; 

        protected readonly string _componentName;
        
        protected ILog Log { get; }

        protected abstract Task Consume(T item);

        protected ProducerConsumer(string componentName, ILog log)
        {
            _componentName = componentName;
            _metricName = $"ProducerConsumer<{typeof(T).Name}> for {_componentName}";   
            
            Log = log;
        }

        protected ProducerConsumer(ILog log)
        {
            _metricName = $"ProducerConsumer<{typeof(T).Name}>";
            
            Log = log;
        }

        private bool _started;
        private Task _threadTask;
        private TaskCompletionSource<T> _last;

        private TaskCompletionSource<T> Dequeue()
        {
            lock (_queue)
                return _queue.Dequeue();
        }

        private async Task StartTask()
        {
            while (true)
            {
                try
                {
                    try
                    {
                        var task = Dequeue();
                        var value = await task.Task;

                        if (value == null)
                            return;

                        await Consume(value);
                        
                        ApplicationInsightsTelemetry.TrackMetric(_metricName, _queue.Count);
                    }
                    catch (Exception exception)
                    {
                        await LogErrorAsync(exception);
                    }
                }
                // Saves the loop if nothing didn't help
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }
        }

        private async Task LogErrorAsync(Exception exception)
        {
            try
            {
                if (Log != null)
                {
                    if (string.IsNullOrWhiteSpace(_componentName))
                        await Log.WriteErrorAsync("Handle", "", exception);
                    else
                        await Log.WriteErrorAsync(_componentName, "Handle", "", exception);
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        protected void Produce(T item)
        {
            Start();

            lock (_queue)
            {
                var last = _last;
                _last = new TaskCompletionSource<T>();
                _queue.Enqueue(_last);
                ApplicationInsightsTelemetry.TrackMetric(_metricName, _queue.Count);
                last.SetResult(item);
            }

        }

        public virtual void Start()
        {
            if (_started)
                return;

            lock (_startStopLockobject)
            {
                if (_started)
                    return;

                _started = true;
            }


            _last = new TaskCompletionSource<T>();
            lock (_queue)
                _queue.Enqueue(_last);

            _threadTask = StartTask();
        }

        public virtual void Stop()
        {
            if (!_started)
                return;

            lock (_startStopLockobject)
            {
                if (!_started)
                    return;
                _started = false;
            }

            _last.SetResult(null);
            _threadTask.Wait();
        }

        public void Dispose()
        {
            Stop();
        }
    }


}
