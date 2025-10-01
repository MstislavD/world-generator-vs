using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomExtension
{
    public class RandomExt : Random
    {
        Random _random;
        public RandomExt()
        {
            Seed = new Random().Next(int.MinValue, int.MaxValue);
            _random = new Random(Seed);
        }

        public RandomExt(int seed)
        {
            Seed = seed;
            _random = new Random(Seed);
        }

        public int Seed { get; }

        public int[] NextVector(int size, int maxValue)
        {
            int[] vector = new int[size];
            for (int i = 0; i < size; i++)
            {
                vector[i] = Next(maxValue);
            }
            return vector;
        }

        public T NextItem<T>(IList<T> list)
        {
            int index = _random.Next(list.Count);
            return list[index];
        }

        public T NextItemExtract<T>(IList<T> list)
        {
            int index = _random.Next(list.Count);
            int lastIndex = list.Count - 1;
            T item = list[index];
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        public IEnumerable<T> Permutation<T>(IList<T> list)
        {
            while (list.Count > 0)
                yield return NextItemExtract(list);
        }

 

        public override int Next() => _random.Next();
        public override int Next(int maxValue) => _random.Next(maxValue);
        public override int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
        public override double NextDouble() => _random.NextDouble();
        public override void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
    }
}
