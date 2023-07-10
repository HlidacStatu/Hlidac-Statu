using System.Text;
using System;
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

        public static string PrettyPrintException(Exception ex)
        {
            StringBuilder sb = new StringBuilder(1024);
            Exception currentEx = ex;
            int exceptionDepth = 0;
            while (currentEx != null)
            {

                sb.Append("  Ex Type: " + currentEx.GetType().ToString() + "\r\n");
                if (currentEx.Message != null)
                    sb.Append("  LogMessage: " + currentEx.Message + "\r\n");

                if (currentEx.TargetSite != null)
                    sb.Append("  In Method: " + currentEx.TargetSite.ToString() + "\r\n");

                if (currentEx.StackTrace != null)
                    sb.Append("  Stack Trace: \r\n" + currentEx.StackTrace + "\r\n");

                currentEx = currentEx.InnerException;
                if (currentEx != null)
                {
                    sb.Append("\r\n\r\n");
                    exceptionDepth++;
                }
            }
            return sb.ToString();
        }
    }
}
