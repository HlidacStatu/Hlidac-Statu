using System;
using System.Collections.Generic;

namespace HlidacStatu.Util.Cache
{

    /// <summary>
    /// Manager jednotlivych instanci Cache
    /// </summary>
    /// <typeparam name="T">Typ ukladanych dat</typeparam>
    /// <typeparam name="Key">Typ unikatniho klice</typeparam>
    /// <typeparam name="TCache">Typ cache</typeparam>
    public abstract class Manager<T, Key, TCache>
        where T : class
        where TCache : Devmasters.Cache.BaseCache<T>
    {
        protected Func<Key, T> contentFunc = null;
        protected TimeSpan defaultExpiration = TimeSpan.Zero;
        protected string keyPrefix = null;
        protected object lockObj = new object();

        public Manager(string keyprefix, Func<Key, T> func, TimeSpan expiration)
        {
            contentFunc = func;
            this.defaultExpiration = expiration;
            keyPrefix = keyprefix;
        }

        public virtual T Get(Key key)
        {
            return Get(key, this.defaultExpiration);
        }
        public virtual T Get(Key key, TimeSpan itemSpecificExpiration)
        {
            return getTCacheInstance(key, itemSpecificExpiration, o => contentFunc.Invoke(key)).Get();
        }

        protected abstract TCache getTCacheInstance(Key key, TimeSpan itemSpecificExpiration, Func<Key, T> contentFunc);

        public bool Exists(Key key)
        {
            return getTCacheInstance(key, this.defaultExpiration, o => contentFunc.Invoke(key)).Exists();
        }

        public void Delete(Key key)
        {
            Set(key, null, defaultExpiration);
        }

        public void Set(Key key, T obj)
        {
            Set(key, obj, defaultExpiration);
        }

        public void Set(Key key, T obj, TimeSpan itemSpecificExpiration)
        {
            if (obj == null)
                getTCacheInstance(key, itemSpecificExpiration, o => contentFunc.Invoke(key)).Invalidate();
            else
            {
                getTCacheInstance(key, itemSpecificExpiration, o => contentFunc.Invoke(key)).ForceRefreshCache(obj);
            }
        }


        protected static object instancesLock = new object();
        protected static Dictionary<string, Manager<T, Key, TCache>> instances = new Dictionary<string, Manager<T, Key, TCache>>();



    }
}
