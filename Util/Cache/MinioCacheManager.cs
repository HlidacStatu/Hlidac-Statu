using System;

namespace HlidacStatu.Util.Cache
{

    public class MinioCacheManager<T, Key>
        : Manager<T, Key, Devmasters.Cache.AWS_S3.Cache<T>>
        where T : class
    {
        private string bucketName = "";
        private string accessKey = "";
        private string secretKey = "";
        private string[] serversUrl;
        Func<Key, string> keyValueSelector = null;
        public MinioCacheManager(string keyPrefix, Func<Key, T> func, TimeSpan expiration
            , string[] serversUrl, string bucket, string accessKey, string secretKey
            , Func<Key, string> keyValueSelector = null)
            : base(keyPrefix, func, expiration)
        {
            bucketName = bucket;
            this.serversUrl = serversUrl;
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.keyValueSelector = keyValueSelector ?? new Func<Key, string>(k => k.ToString());
        }
        protected override Devmasters.Cache.AWS_S3.Cache<T> getTCacheInstance(Key key, TimeSpan expiration, Func<Key, T> contentFunc)
        {
            return new Devmasters.Cache.AWS_S3.Cache<T>(
                this.serversUrl, this.bucketName, this.accessKey, this.secretKey,
                expiration, keyPrefix + keyValueSelector(key), (o) => contentFunc.Invoke(key)
            );
        }

        public static MinioCacheManager<T, Key> GetSafeInstance(string instanceName, Func<Key, T> func, TimeSpan expiration,
            string[] serversUrl, string bucketName, string accessKey, string secretKey,
            Func<Key, string> keyValueSelector = null)
        {
            lock (instancesLock)
            {
                string instanceFullName = instanceName + "|" + typeof(T).ToString() + "|" + typeof(Key).ToString();

                if (!instances.ContainsKey(instanceFullName))
                {
                    instances[instanceFullName] = new MinioCacheManager<T, Key>(instanceName, func, expiration,
                        serversUrl, bucketName, accessKey, secretKey,
                        keyValueSelector: keyValueSelector);
                }
                return (MinioCacheManager<T, Key>)instances[instanceFullName];
            }
        }



    }
}
