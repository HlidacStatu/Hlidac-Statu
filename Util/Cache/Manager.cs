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
        protected TimeSpan expiration = TimeSpan.Zero;
        protected string keyPrefix = null;
        protected object lockObj = new object();

        public Manager(string keyprefix, Func<Key, T> func, TimeSpan expiration)
        {
            contentFunc = func;
            this.expiration = expiration;
            keyPrefix = keyprefix + "#";
        }

        public virtual T Get(Key key)
        {
            return Get(key, expiration);
        }
        public virtual T Get(Key key, TimeSpan expiration)
        {
            return getTCacheInstance(key, expiration, o => contentFunc.Invoke(key)).Get();                
        }

        protected abstract TCache getTCacheInstance(Key key, TimeSpan expiration, Func<Key, T> contentFunc);

        public void Delete(Key key)
        {
            Set(key, null, expiration);
        }

        public void Set(Key key, T obj)
        {
            Set(key, obj, expiration);
        }

        public void Set(Key key, T obj, TimeSpan expiration)
        {
            if (obj == null)
                getTCacheInstance(key, expiration, o => contentFunc.Invoke(key)).Invalidate();
            else
            {
                getTCacheInstance(key, expiration, o => contentFunc.Invoke(key)).ForceRefreshCache(obj);
            }
        }


        protected static object instancesLock = new object();
        protected static Dictionary<string, Manager<T, Key,TCache>> instances = new Dictionary<string, Manager<T, Key, TCache>>();



    }
}
