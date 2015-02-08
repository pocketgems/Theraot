#if FAT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Theraot.Threading.Needles;

namespace Theraot.Collections.ThreadSafe
{
    /// <summary>
    /// Represent a thread-safe wait-free fixed size bucket with lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <typeparam name="TNeedle">THe type of the needles</typeparam>
    /// <remarks>
    /// Consider wrapping this class to implement <see cref="ICollection{T}" /> or any other desired interface.
    /// </remarks>
    [Serializable]
    public sealed class NeedleBucket<T, TNeedle> : IEnumerable<T>
        where TNeedle : INeedle<T>
    {
        private readonly Bucket<TNeedle> _entries;
        private readonly Converter<int, TNeedle> _needleFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeedleBucket{T, TNeedle}" /> class.
        /// </summary>
        /// <param name = "valueFactory">The delegate that is invoked to do the lazy initialization of the items given their index.</param>
        /// <param name="capacity">The capacity.</param>
        public NeedleBucket(Converter<int, T> valueFactory, int capacity)
        {
            if (ReferenceEquals(valueFactory, null))
            {
                throw new ArgumentNullException("valueFactory");
            }
            else
            {
                if (!NeedleHelper.CanCreateNeedle<T, TNeedle>())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unable to find a way to create {0}", typeof(TNeedle).Name));
                }
                else
                {
                    _needleFactory = index => NeedleReservoir<T, TNeedle>.GetNeedle(valueFactory(index));
                    _entries = new Bucket<TNeedle>(capacity);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NeedleBucket{T, TNeedle}" /> class.
        /// </summary>
        /// <param name = "valueFactory">The delegate that is invoked to do the lazy initialization of the items.</param>
        /// <param name="capacity">The capacity.</param>
        public NeedleBucket(Func<T> valueFactory, int capacity)
        {
            if (ReferenceEquals(valueFactory, null))
            {
                throw new ArgumentNullException("valueFactory");
            }
            else
            {
                if (!NeedleHelper.CanCreateNeedle<T, TNeedle>())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unable to find a way to create {0}", typeof(TNeedle).Name));
                }
                else
                {
                    _needleFactory = index => NeedleReservoir<T, TNeedle>.GetNeedle(valueFactory());
                    _entries = new Bucket<TNeedle>(capacity);
                }
            }
        }

        /// <summary>
        /// Gets the capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _entries.Capacity;
            }
        }

        /// <summary>
        /// Gets the number of items actually contained.
        /// </summary>
        public int Count
        {
            get
            {
                return _entries.Count;
            }
        }

        /// <summary>
        /// Copies the items to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        /// <exception cref="System.ArgumentNullException">array</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex;Non-negative number is required.</exception>
        /// <exception cref="System.ArgumentException">array;The array can not contain the number of elements.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _entries.ToArray().CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was new; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool Exchange(int index, T item, out T previous)
        {
            TNeedle _previous;
            if (_entries.Exchange(index, NeedleReservoir<T, TNeedle>.GetNeedle(item), out _previous))
            {
                previous = default(T);
                return true;
            }
            else
            {
                // TryGetValue is null resistant
                _previous.TryGetValue(out previous);
                // This is a needle that is no longer referenced, we donate it
                NeedleReservoir<T, TNeedle>.DonateNeedle(_previous);
                return false;
            }
        }

        /// <summary>
        /// Sets the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="needle">The needle.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was new; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool ExchangeNeedle(int index, TNeedle needle, out TNeedle previous)
        {
            // This may allow null to enter, this may also give null as previous if null was allowed to enter
            // We don't donate because we return the needle
            return _entries.Exchange(index, needle, out previous);
        }

        /// <summary>
        /// Retrieve or creates a new item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The value.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public T Get(int index)
        {
            if (index < 0 || index >= _entries.Capacity)
            {
                throw new ArgumentOutOfRangeException("index", "index must be greater or equal to 0 and less than capacity");
            }
            // Using TryGetValue first just avoid wasting a needle
            TNeedle _previous;
            if (_entries.TryGetInternal(index, out _previous))
            {
                // Null resistant
                T previous;
                _previous.TryGetValue(out previous);
                return previous;
                // We don't donate because we didn't remove
            }
            else
            {
                var newNeedle = _needleFactory(index);
                if (_entries.InsertInternal(index, newNeedle, out _previous))
                {
                    return newNeedle.Value;
                }
                else
                {
                    // we just failed to insert, meaning that we created a usless needle
                    // donate it
                    NeedleReservoir<T, TNeedle>.DonateNeedle(newNeedle);
                    return _previous.Value;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="System.Collections.Generic.IEnumerator{T}" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var needle in _entries)
            {
                T item;
                if (needle.TryGetValue(out item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Retrieve or creates a new needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The needle.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public TNeedle GetNeedle(int index)
        {
            TNeedle _previous;
            var newNeedle = _needleFactory(index);
            if (_entries.Insert(index, newNeedle, out _previous))
            {
                return newNeedle;
            }
            else
            {
                // we just failed to insert, meaning that we created a usless needle
                // donate it
                NeedleReservoir<T, TNeedle>.DonateNeedle(newNeedle);
                return _previous;
            }
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity.</exception>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the item was overwritten or removed.
        /// </remarks>
        public bool Insert(int index, T item)
        {
            // Only succeeds if there was nothing there before
            // meaning that if this succeeds it replaced nothing
            // If this fails whatever was there is still there
            return _entries.Insert(index, NeedleReservoir<T, TNeedle>.GetNeedle(item));
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the item was overwritten or removed.
        /// </remarks>
        public bool Insert(int index, T item, out T previous)
        {
            TNeedle _previous;
            if (_entries.Insert(index, NeedleReservoir<T, TNeedle>.GetNeedle(item), out _previous))
            {
                previous = default(T);
                return true;
            }
            else
            {
                // TryGetValue is null resistant
                _previous.TryGetValue(out previous);
                // Donate it
                NeedleReservoir<T, TNeedle>.DonateNeedle(_previous);
                return false;
            }
        }

        /// <summary>
        /// Inserts the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="needle">The needle.</param>
        /// <returns>
        ///   <c>true</c> if the needle was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity.</exception>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the needle was overwritten or removed.
        /// </remarks>
        public bool InsertNeedle(int index, TNeedle needle)
        {
            // This may allow null to enter
            return _entries.Insert(index, needle);
        }

        /// <summary>
        /// Inserts the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="needle">The needle.</param>
        /// <param name="previous">The previous needle in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the needle was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the needle was overwritten or removed.
        /// </remarks>
        public bool InsertNeedle(int index, TNeedle needle, out TNeedle previous)
        {
            // This may allow null to enter, this may also give null as previous if null was allowed to enter
            return _entries.Insert(index, needle, out previous);
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool RemoveAt(int index)
        {
            return _entries.RemoveAt(index);
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool RemoveAt(int index, out T previous)
        {
            TNeedle _previous;
            if (_entries.RemoveAt(index, out _previous))
            {
                // TryGetValue is null resistant
                _previous.TryGetValue(out previous);
                // Donate it
                NeedleReservoir<T, TNeedle>.DonateNeedle(_previous);
                return true;
            }
            else
            {
                previous = default(T);
                return false;
            }
        }

        /// <summary>
        /// Removes the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="previous">The previous needle in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the needle was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool RemoveNeedleAt(int index, out TNeedle previous)
        {
            return _entries.RemoveAt(index, out previous);
        }

        /// <summary>
        /// Sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="isNew">if set to <c>true</c> the index was not previously used.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public void Set(int index, T item, out bool isNew)
        {
            //This may have replaced something
            _entries.Set(index, NeedleReservoir<T, TNeedle>.GetNeedle(item), out isNew);
        }

        /// <summary>
        /// Sets the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="needle">The needle.</param>
        /// <param name="isNew">if set to <c>true</c> the index was not previously used.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public void SetNeedle(int index, TNeedle needle, out bool isNew)
        {
            // This may allow null to enter
            _entries.Set(index, needle, out isNew);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Tries to retrieve the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the item was retrieved; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool TryGet(int index, out T value)
        {
            TNeedle _previous;
            if (_entries.TryGet(index, out _previous))
            {
                // Null resistant
                _previous.TryGetValue(out value);
                // We don't donate because we didn't remove
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Tries to retrieve the needle at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the needle was retrieved; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool TryGetNeedle(int index, out TNeedle value)
        {
            return _entries.TryGet(index, out value);
        }
    }
}

#endif