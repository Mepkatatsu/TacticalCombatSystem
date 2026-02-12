#nullable enable
using System.Collections.Generic;

namespace Script.CommonLib.Tables
{
    public abstract class TableBase<TKey, TData> : ITable<TKey, TData> where TKey : notnull
    {
        protected readonly Dictionary<TKey, TData> datas = new();

        public abstract void Initialize();

        public TData Get(TKey key)
        {
            return datas.GetValueOrDefault(key);
        }

        public ICollection<TKey> GetAllKeys()
        {
            return datas.Keys;
        }

        public ICollection<TData> GetAllValues()
        {
            return datas.Values;
        }
    }
}
