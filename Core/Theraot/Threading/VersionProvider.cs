using System;
using System.Threading;

namespace Theraot.Threading
{
    public sealed class VersionProvider
    {
        private Target _target;
        private Advancer _tryAdvance;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "By Design")]
        public VersionProvider(out Action nextVersion)
        {
            _target = new Target(out _tryAdvance);
            nextVersion = Advance;
        }

        public VersionProvider()
        {
            _target = new Target(out _tryAdvance);
        }

        internal delegate bool Advancer(out long number);

        public void Advance()
        {
            long number;
            if (!_tryAdvance.Invoke(out number))
            {
                _target = new Target(out _tryAdvance);
            }
        }

        public VersionToken AdvanceNewToken()
        {
            long number;
            if (!_tryAdvance.Invoke(out number))
            {
                _target = new Target(out _tryAdvance);
            }
            return new VersionToken(this, _target, number);
        }

        public VersionToken NewToken()
        {
            return new VersionToken(this);
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "By Design")]
        public sealed class VersionToken : IComparable<VersionToken>
        {
            private readonly VersionProvider _provider;
            private long _number;
            private Target _target;

            internal VersionToken(VersionProvider provider)
            {
                _provider = provider;
            }

            internal VersionToken(VersionProvider provider, Target target, long number)
            {
                _provider = provider;
                _target = target;
                _number = number;
            }

            public int CompareTo(VersionToken other)
            {
                if (ReferenceEquals(other, null))
                {
                    return 1;
                }
                else
                {
                    if (ReferenceEquals(_target, other._target))
                    {
                        return _number.CompareTo(other._number);
                    }
                    else
                    {
                        if (ReferenceEquals(_target, null))
                        {
                            return -1;
                        }
                        else
                        {
                            if (ReferenceEquals(other._target, null))
                            {
                                return 1;
                            }
                            else
                            {
                                var check = _target.CompareToExtracted(other._target);
                                if (check == 0)
                                {
                                    return _number.CompareTo(other._number);
                                }
                                else
                                {
                                    return check;
                                }
                            }
                        }
                    }
                }
            }

            public void Reset()
            {
                _target = null;
                _number = 0;
            }

            public void Update(Action callback)
            {
                if (callback == null)
                {
                    throw new ArgumentNullException("update");
                }
                else
                {
                    var nextTarget = _provider._target;
                    var needUpdate = false;
                    if (!ReferenceEquals(Interlocked.Exchange<Target>(ref _target, nextTarget), nextTarget))
                    {
                        needUpdate = true;
                    }
                    if (Interlocked.Exchange(ref _number, nextTarget.Number) != nextTarget.Number)
                    {
                        needUpdate = true;
                    }
                    if (needUpdate)
                    {
                        callback();
                    }
                }
            }

            public void Update()
            {
                var nextTarget = _provider._target;
                Interlocked.Exchange<Target>(ref _target, nextTarget);
                Interlocked.Exchange(ref _number, nextTarget.Number);
            }
        }

        internal sealed class Target : IComparable<Target>
        {
            private readonly long _time = ThreadingHelper.TicksNow();
            private long _number = long.MinValue;

            internal Target(out Advancer tryAdvance)
            {
                tryAdvance = TryAdvance;
            }

            public long Number
            {
                get
                {
                    return _number;
                }
            }

            public int CompareTo(Target other)
            {
                if (ReferenceEquals(other, null))
                {
                    return 1;
                }
                else
                {
                    return CompareToExtracted(other);
                }
            }

            internal int CompareToExtracted(Target other)
            {
                return _time.CompareTo(other._time);
            }

            private bool TryAdvance(out long number)
            {
                if (_number == long.MaxValue)
                {
                    number = 0;
                    return false;
                }
                else
                {
                    number = Interlocked.Increment(ref _number);
                    return true;
                }
            }
        }
    }
}