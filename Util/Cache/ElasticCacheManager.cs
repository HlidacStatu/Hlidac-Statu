using System;

namespace HlidacStatu.Util.Cache
{

    public class ElasticCacheManager<T, Key>
        : Manager<T, Key, Devmasters.Cache.Elastic.Cache<T>>
        where T : class
    {
        private string indicieName = "";
        private string accessKey = "";
        private string secretKey = "";
        private string[] serversUrl;
        Func<Key, string> keyValueSelector = null;
        public ElasticCacheManager(string keyPrefix, Func<Key, T> func, TimeSpan expiration
            , string[] serversUrl, string indicie, string accessKey=null, string secretKey=null
            , Func<Key, string> keyValueSelector = null)
            : base(keyPrefix, func, expiration)
        {
            indicieName = indicie;
            this.serversUrl = serversUrl;
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.keyValueSelector = keyValueSelector ?? new Func<Key, string>(k => k.ToString());
        }
        protected override Devmasters.Cache.Elastic.Cache<T> getTCacheInstance(Key key, TimeSpan expiration, Func<Key, T> contentFunc)
        {
            return new Devmasters.Cache.Elastic.Cache<T>(
                this.serversUrl, this.indicieName,
                expiration, keyPrefix + keyValueSelector(key), (o) => contentFunc.Invoke(key)
            );
        }

        public static ElasticCacheManager<T, Key> GetSafeInstance(string instanceName, Func<Key, T> func, TimeSpan expiration,
            string[] serversUrl, string bucketName, string accessKey, string secretKey,
            Func<Key, string> keyValueSelector = null)
        {
            lock (instancesLock)
            {
                string instanceFullName = instanceName + "|" + typeof(T).ToString() + "|" + typeof(Key).ToString();

                if (!instances.ContainsKey(instanceFullName))
                {
                    instances[instanceFullName] = new ElasticCacheManager<T, Key>(instanceName, func, expiration,
                        serversUrl, bucketName, accessKey, secretKey,
                        keyValueSelector: keyValueSelector);
                }
                return (ElasticCacheManager<T, Key>)instances[instanceFullName];
            }
        }



    }
}
