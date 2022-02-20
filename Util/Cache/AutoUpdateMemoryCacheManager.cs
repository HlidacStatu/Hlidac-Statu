using Devmasters.Cache.LocalMemory;

using System;

namespace HlidacStatu.Util.Cache
{

    public class AutoUpdateMemoryCacheManager<T, Key>
        : Manager<T, Key, AutoUpdatedLocalMemoryCache<T>>
        where T : class
    {

        Func<Key, string> keyValueSelector = null;

        public AutoUpdateMemoryCacheManager(string keyPrefix, Func<Key, T> func, TimeSpan expiration, Func<Key, string> keyValueSelector = null)
            : base(keyPrefix, func, expiration)
        {
            this.keyValueSelector = keyValueSelector ?? new Func<Key, string>(k => k.ToString());
        }
        protected override AutoUpdatedLocalMemoryCache<T> getTCacheInstance(Key key, TimeSpan expiration, Func<Key, T> contentFunc)
        {
            return new AutoUpdatedLocalMemoryCache<T>(expiration, keyPrefix + keyValueSelector(key), (o) => contentFunc.Invoke(key));
        }

        private static AutoUpdateMemoryCacheManager<T, Key> GetSafeInstance(Type instanceType)
        {
            lock (instancesLock)
            {
                string instanceFullName = instanceType.AssemblyQualifiedName + "|" + typeof(T).ToString() + "|" + typeof(Key).ToString();
                if (!instances.ContainsKey(instanceFullName))
                {
                    instances[instanceFullName] = (AutoUpdateMemoryCacheManager<T, Key>)Activator.CreateInstance(instanceType);
                }
                return (AutoUpdateMemoryCacheManager<T, Key>)instances[instanceFullName];
            }
        }
        public static AutoUpdateMemoryCacheManager<T, Key> GetSafeInstance(string instanceName, Func<Key, T> func, TimeSpan expiration,
            Func<Key, string> keyValueSelector = null)
        {
            lock (instancesLock)
            {
                string instanceFullName = instanceName + "|" + typeof(T).ToString() + "|" + typeof(Key).ToString();

                if (!instances.ContainsKey(instanceFullName))
                {
                    instances[instanceFullName] = new AutoUpdateMemoryCacheManager<T, Key>(instanceName, func, expiration,
                        keyValueSelector: keyValueSelector);
                }
                return (AutoUpdateMemoryCacheManager<T, Key>)instances[instanceFullName];
            }
        }



    }
}
