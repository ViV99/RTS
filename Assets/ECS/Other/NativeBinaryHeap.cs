using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECS.Other
{
    public struct NativeBinaryHeap<T>
        where T : struct, IComparable<T>
    {
        private NativeList<T> array;
        public int Count;

        public NativeBinaryHeap(Allocator allocator)
        {
            Count = 0;
            array = new NativeList<T>(allocator);
        }

        public void Dispose()
        {
            array.Dispose();
        }

        public T ExtractMin()
        {
            var min = array[0];
            array[0] = array[Count - 1];
            Count--;
            SiftDown(0);
            return min;
        }

        public void Insert(T key)
        {
            Count++;
            if (Count - 1 == array.Length)
                array.Add(key);
            else
                array[Count - 1] = key;
            SiftUp(Count - 1);
        }

        private void SiftDown(int i)
        {
            while (2 * i + 1 < Count)
            {
                var left = 2 * i + 1;
                var right = 2 * i + 2;
                var j = left;
                if (right < Count && array[right].CompareTo(array[left]) < 0)
                    j = right;
                if (array[i].CompareTo(array[j]) <= 0)
                    break;
                var swp = array[i];
                array[i] = array[j];
                array[j] = swp;
                i = j;
            }
        }
        
        private void SiftUp(int i)
        {
            while (array[i].CompareTo(array[(i - 1) / 2]) < 0)
            {
                var swp = array[i];
                array[i] = array[(i - 1) / 2];
                array[(i - 1) / 2] = swp;
                i = (i - 1) / 2;
            }
        }
    }
}