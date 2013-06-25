#if FAT
﻿using System;
using System.Threading;
using Theraot.Core;

namespace Theraot.Threading
{
    public sealed class Work : ICloneable
    {
        [ThreadStatic]
        private static Work _current;

        private static int _lastId;
        private readonly Action _action;
        private readonly Context _context;
        private readonly bool _exclusive;
        private int _done;
        private int _id;
        private Exception _resultException;

        private Work(Action action, bool exclusive, Context context)
        {
            if (ReferenceEquals(context, null))
            {
                throw new ArgumentNullException("context");
            }
            else
            {
                _id = Interlocked.Increment(ref _lastId) - 1;
                _context = context;
                _action = action ?? ActionHelper.GetNoopAction();
                _exclusive = exclusive;
            }
        }

        public static Work Current
        {
            get
            {
                return _current;
            }
        }

        public bool Done
        {
            get
            {
                return Thread.VolatileRead(ref _done) == 1;
            }
        }

        public int Id
        {
            get
            {
                return _id;
            }
        }

        public Work Clone()
        {
            return _context.AddWork(_action, _exclusive);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void Start()
        {
            _context.ScheduleWork(this);
        }

        public void Wait()
        {
            while (Thread.VolatileRead(ref _done) == 0)
            {
                _context.DoOneWork();
            }
        }

        private void Execute()
        {
            var oldCurrent = Interlocked.Exchange(ref _current, this);
            try
            {
                _action.Invoke();
            }
            catch (Exception exc)
            {
                _resultException = exc;
            }
            finally
            {
                Thread.VolatileWrite(ref _done, 1);
                Interlocked.Exchange(ref _current, oldCurrent);
            }
        }

        public sealed class Context : IDisposable
        {
            private const int INT_InitialWorkCapacityHint = 128;
            private static Context _defaultContext = new Context(INT_InitialWorkCapacityHint, Environment.ProcessorCount, false);

            private static int _lastId;
            private int _dedidatedThreadCount;
            private bool _disposable;
            private AutoResetEvent _event;
            private int _id;
            private LazyBucket<Thread> _threads;
            private volatile bool _waitRequest;
            private volatile bool _work;
            private int _workingDedicatedThreadCount;
            private int _workingTotalThreadCount;
            private QueueBucket<Work> _works;

            public Context()
                : this(INT_InitialWorkCapacityHint, Environment.ProcessorCount, true)
            {
                //Empty
            }

            public Context(int initialWorkCapacity)
                : this(initialWorkCapacity, Environment.ProcessorCount, true)
            {
                //Empty
            }

            public Context(int initialWorkCapacity, int dedicatedThreads)
                : this(initialWorkCapacity, dedicatedThreads, true)
            {
                //Empty
            }

            private Context(int initialWorkCapacity, int dedicatedThreads, bool disposable)
            {
                if (dedicatedThreads < 0)
                {
                    throw new ArgumentOutOfRangeException("dedicatedThreads", "dedicatedThreads < 0");
                }
                else
                {
                    _works = new QueueBucket<Work>(initialWorkCapacity);
                    _threads = new LazyBucket<Thread>
                        (
                            input => new Thread(DoWorks)
                            {
                                Name = string.Format("Dedicated Thread {0} on Work.Context {1}", input, _id),
                                IsBackground = true
                            },
                            dedicatedThreads
                        );
                    _id = Interlocked.Increment(ref _lastId) - 1;
                    _event = new AutoResetEvent(false);
                    _work = true;
                    _disposable = disposable;
                }
            }

            [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralexceptionTypes", Justification = "Pokemon")]
            ~Context()
            {
                try
                {
                    //Empty
                }
                finally
                {
                    try
                    {
                        DisposeExtracted();
                    }
                    catch
                    {
                        //Pokemon
                    }
                }
            }

            public static Context DefaultContext
            {
                get
                {
                    return _defaultContext;
                }
            }

            public int Id
            {
                get
                {
                    return _id;
                }
            }

            public Work AddWork(Action action)
            {
                return new Work(action, false, this);
            }

            public Work AddWork(Action action, bool exclusive)
            {
                return new Work(action, exclusive, this);
            }

            public void Dispose()
            {
                if (_disposable)
                {
                    DisposeExtracted();
                }
            }

            public void DoOneWork()
            {
                if (_work)
                {
                    try
                    {
                        if (_waitRequest)
                        {
                            ThreadingHelper.SpinWait(() => !_waitRequest);
                        }
                        Interlocked.Increment(ref _workingTotalThreadCount);
                        Work item;
                        if (_works.TryDequeue(out item))
                        {
                            Execute(item);
                        }
                    }
                    catch
                    {
                        // Pokemon
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _workingTotalThreadCount);
                    }
                }
                else
                {
                    throw new ObjectDisposedException(GetType().ToString());
                }
            }

            internal void ScheduleWork(Work work)
            {
                if (_work)
                {
                    _works.Enqueue(work);
                    if (_threads.Capacity == 0)
                    {
                        ThreadPool.QueueUserWorkItem
                        (
                            _ =>
                            {
                                DoOneWork();
                            }
                        );
                    }
                    else
                    {
                        var threadIndex = Interlocked.Increment(ref _dedidatedThreadCount) - 1;
                        if (threadIndex < _threads.Capacity)
                        {
                            Thread thread;
                            _threads.TryGet(threadIndex, out thread);
                            thread.Start();
                        }
                        else
                        {
                            Thread.VolatileWrite(ref _dedidatedThreadCount, _threads.Capacity);
                            if (Thread.VolatileRead(ref _workingDedicatedThreadCount) < _threads.Count)
                            {
                                _event.Set();
                            }
                        }
                    }
                }
            }

            private void DisposeExtracted()
            {
                try
                {
                    _work = false;
                    while (Thread.VolatileRead(ref _dedidatedThreadCount) > 0)
                    {
                        _event.Set();
                    }
                    _event.Close();
                }
                finally
                {
                    try
                    {
                        //Empty
                    }
                    finally
                    {
                        _work = false;
                        _threads = null;
                        GC.SuppressFinalize(this);
                    }
                }
            }

            private void DoWorks()
            {
                try
                {
                    Interlocked.Increment(ref _workingTotalThreadCount);
                    Interlocked.Increment(ref _workingDedicatedThreadCount);
                    while (_work)
                    {
                        Work item;
                        if (_works.TryDequeue(out item))
                        {
                            Execute(item);
                        }
                        else if (_works.Count == 0)
                        {
                            try
                            {
                                Interlocked.Decrement(ref _workingDedicatedThreadCount);
                                Interlocked.Decrement(ref _workingTotalThreadCount);
                                _event.WaitOne();
                            }
                            finally
                            {
                                Interlocked.Increment(ref _workingTotalThreadCount);
                                Interlocked.Increment(ref _workingDedicatedThreadCount);
                            }
                        }
                        if (_waitRequest)
                        {
                            Interlocked.Decrement(ref _workingTotalThreadCount);
                            ThreadingHelper.SpinWait(() => !_waitRequest);
                            Interlocked.Increment(ref _workingTotalThreadCount);
                        }
                    }
                }
                catch
                {
                    // Pokemon
                }
                finally
                {
                    Interlocked.Decrement(ref _workingDedicatedThreadCount);
                    Interlocked.Decrement(ref _workingTotalThreadCount);
                }
            }

            private void Execute(Work item)
            {
                if (item._exclusive)
                {
                    _waitRequest = true;
                    ThreadingHelper.SpinWait(() => Thread.VolatileRead(ref _workingTotalThreadCount) == 1);
                    item.Execute();
                    _waitRequest = false;
                }
                else
                {
                    item.Execute();
                }
            }
        }
    }
}
#endif