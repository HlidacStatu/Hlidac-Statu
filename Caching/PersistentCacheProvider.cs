namespace HlidacStatu.Caching
{
    public static class PersistentCacheProvider
    {
        public static bool Save<T>(string key, T value)
        {
            byte[] serialized = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(value);

            //TODO save to db
            throw new NotImplementedException();
        }
        public static T? Get<T>(string key, T valueIfNotExists = default!)
        {
            try
            {
                //is in db ?
                var exist = false; //check in db
                if (!exist)
                    return valueIfNotExists;


                byte[] utf8Bytes = new byte[] { }; //read from db
                if (utf8Bytes == null || utf8Bytes.Length == 0)
                    return valueIfNotExists;
                T? value = System.Text.Json.JsonSerializer.Deserialize<T>(utf8Bytes);

                return value;
            }
            catch (Exception)
            {
                //TODO logging
                return valueIfNotExists;
            }
        }

    }
}
