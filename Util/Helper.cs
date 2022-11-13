using System.Threading.Tasks;

namespace HlidacStatu.Util
{
    public static class Helper
    {
        public static bool IsSet<T>(T flags, T flag) where T : struct
        {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;

            return (flagsValue & flagValue) != 0;
        }

        public static void Set<T>(ref T flags, T flag) where T : struct
        {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;

            flags = (T)(object)(flagsValue | flagValue);
        }

        public static void Unset<T>(ref T flags, T flag) where T : struct
        {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;

            flags = (T)(object)(flagsValue & (~flagValue));
        }
        
        /// <summary>
        /// Makes Task return its base class 
        /// </summary>
        public static async Task<TBase> GeneralizeTask<TBase, TDerived>(Task<TDerived> task) 
            where TDerived : TBase 
        {
            return (TBase) await task;
        }
    }
}
