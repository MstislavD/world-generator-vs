using RandomExtension;
using System;
using System.Collections.Generic;

namespace WorldSimulation.HistorySimulation
{
    class SelectItemArgs<T>
    {
        List<T> items = new List<T>();
        public void AddName(T item) => items.Add(item);
        public int Count => items.Count;
        public T GetItem(RandomExt random) => random.NextItem(items);
    }
}
