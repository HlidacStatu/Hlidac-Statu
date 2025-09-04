using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku
{
    public static class UredniciStaticCache
    {
        private static IFusionCache _cache;

        public static void Init(IFusionCache fusionCache)
        {
            _cache = fusionCache;
        }

        
    }


}
