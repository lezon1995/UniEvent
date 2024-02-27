using System;
using System.Runtime.CompilerServices;

namespace UniEvent.Internal
{
    // fixed size queue.
    internal class FastQueue<T>
    {
        T[] array;
        int head;
        int tail;
        int size;

        public FastQueue(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }

            array = new T[capacity];
            head = tail = size = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (size == array.Length)
            {
                ThrowForFullQueue();
            }

            array[tail] = item;
            MoveNext(ref tail);
            size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (size == 0)
            {
                ThrowForEmptyQueue();
            }

            var _head = head;
            var _array = array;
            var removed = _array[_head];
            _array[_head] = default;
            MoveNext(ref head);
            size--;
            return removed;
        }

        public void EnsureNewCapacity(int capacity)
        {
            var newArray = new T[capacity];
            if (size > 0)
            {
                if (head < tail)
                {
                    Array.Copy(array, head, newArray, 0, size);
                }
                else
                {
                    Array.Copy(array, head, newArray, 0, array.Length - head);
                    Array.Copy(array, 0, newArray, array.Length - head, tail);
                }
            }

            array = newArray;
            head = 0;
            tail = size == capacity ? 0 : size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void MoveNext(ref int index)
        {
            int tmp = index + 1;
            if (tmp == array.Length)
            {
                tmp = 0;
            }

            index = tmp;
        }

        void ThrowForEmptyQueue()
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        void ThrowForFullQueue()
        {
            throw new InvalidOperationException("Queue is full.");
        }
    }
}