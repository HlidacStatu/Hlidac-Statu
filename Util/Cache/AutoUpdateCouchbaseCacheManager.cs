using System;

namespace HlidacStatu.Util.Cache
{

    public class AutoUpdateCouchbaseCacheManager<T, Key>
        : Manager<T, Key, Devmasters.Cache.Couchbase.AutoUpdateCouchbaseCache<T>>
        where T : class
    {

        private string bucketName = "";
        private string username = "";
        private string password = "";
        private string[] serversUrl; 
        Func<Key, string> keyValueSelector = null;
        public AutoUpdateCouchbaseCacheManager(string keyPrefix, Func<Key, T> func, TimeSpan expiration,
            string[] serversUrl, string couchbaseBucketName, string username, string password, Func<Key, string> keyValueSelector = null)
            : base(keyPrefix, func, expiration)
        {
            bucketName = couchbaseBucketName;
            this.serversUrl = serversUrl;
            this.username = username;
            this.password = password;
            this.keyValueSelector = keyValueSelector ?? new Func<Key, string>(k => k.ToString());
        }
        protected override Devmasters.Cache.Couchbase.AutoUpdateCouchbaseCache<T> getTCacheInstance(Key key, TimeSpan expiration, Func<Key, T> contentFunc)
        {

            return new Devmasters.Cache.Couchbase.AutoUpdateCouchbaseCache<T>(expiration, 
                keyPrefix + keyValueSelector(key), 
                (o) => contentFunc.Invoke(key),
                serversUrl, bucketName, username, password
                );
        }


        public static AutoUpdateCouchbaseCacheManager<T, Key> GetSafeInstance(string instanceName, Func<Key, T> func, TimeSpan expiration,
            string[] serversUrl, string couchbaseBucketName, string username, string password, Func
            <Key, string> keyValueSelector = null)
        {
            lock (instancesLock)
            {
                string instanceFullName = instanceName + "|" + typeof(T).ToString() + "|" + typeof(Key).ToString();

                if (!instances.ContainsKey(instanceFullName))
                {
                    instances[instanceFullName] = new AutoUpdateCouchbaseCacheManager<T, Key>(instanceName, func, expiration,
                        serversUrl, couchbaseBucketName, username, password,
                        keyValueSelector: keyValueSelector
                        );
                }
                return (AutoUpdateCouchbaseCacheManager<T, Key>)instances[instanceFullName];
            }
        }



    }
}
