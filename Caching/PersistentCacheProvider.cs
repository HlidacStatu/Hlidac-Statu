namespace HlidacStatu.Caching
{
    /// <summary>
    /// Provides a simple persistent cache abstraction for storing and retrieving values by key.
    /// Values are serialized to UTF-8 JSON before being persisted, and deserialized when read.
    /// </summary>
    public static class PersistentCacheProvider
    {
        /// <summary>
        /// Persists a value in the cache under the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value being cached.</typeparam>
        /// <param name="key">The cache key that uniquely identifies the entry.</param>
        /// <param name="value">The value to be cached. It will be serialized to JSON (UTF-8 bytes).</param>
        /// <returns>
        /// <c>true</c> if the value was successfully saved; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The current implementation only performs serialization and is expected to persist
        /// the serialized bytes to a backing store (e.g., database) in the TODO section.
        /// </remarks>
        /// <exception cref="NotImplementedException">
        /// Thrown until the persistence layer is implemented.
        /// </exception>
        public static bool Save<T>(string key, T value)
        {
            byte[] serialized = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(value);

            // TODO: Save 'serialized' to persistent storage by 'key'.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves a cached value by <paramref name="key"/>. If the value does not exist or cannot be read,
        /// returns <paramref name="valueIfNotExists"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value expected from the cache.</typeparam>
        /// <param name="key">The cache key that uniquely identifies the entry.</param>
        /// <param name="valueIfNotExists">
        /// The fallback value returned when the key is not found or deserialization fails.
        /// </param>
        /// <returns>
        /// The cached value if it exists and is successfully deserialized; otherwise <paramref name="valueIfNotExists"/>.
        /// </returns>
        /// <remarks>
        /// The current implementation uses placeholders for database existence checks and data retrieval.
        /// Replace the TODO sections with actual persistence logic (e.g., reading UTF-8 bytes by key).
        /// </remarks>
        public static T? Get<T>(string key, T valueIfNotExists = default!)
        {
            try
            {
                // TODO: Check whether the value exists in persistent storage.
                var exist = false; // Placeholder for existence check
                if (!exist)
                    return valueIfNotExists;

                // TODO: Read serialized bytes from persistent storage by 'key'.
                byte[] utf8Bytes = new byte[] { }; // Placeholder for read operation
                if (utf8Bytes == null || utf8Bytes.Length == 0)
                    return valueIfNotExists;

                T? value = System.Text.Json.JsonSerializer.Deserialize<T>(utf8Bytes);
                return value;
            }
            catch (Exception)
            {
                // TODO: Add logging of the exception context and key for diagnostics.
                return valueIfNotExists;
            }
        }
    }
}
