using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HlidacStatu.Util
{
    public class StackReport
    {

        /// <summary>
        /// Returns name of method, from which is this method called.
        /// </summary>
        /// <returns></returns>
        public static string GetCallingMethod(bool showFullStack, Func<StackFrame, string> formatter = null, int skipFrames = 0)
        {
             formatter = formatter ?? ShortFormatStackFrame;

            List<StackFrame> steps = new List<StackFrame>();
            int startFrame = 1 + skipFrames;
            StackFrame stackframe = null;
            StackFrame prevStackframe = null;
            StringBuilder sb = new StringBuilder(1024);
            do
            {
                if (stackframe != null)
                    prevStackframe = stackframe;
                stackframe = new StackFrame(startFrame, true);
                steps.Add(stackframe);

                startFrame++;
            } while (stackframe.GetMethod() != null); //&& stackframe.GetMethod().ReflectedType.FullName == "Devmasters.Logger");

            if (IsStackframeNull(stackframe) && IsStackframeNull(prevStackframe) == false)
                stackframe = prevStackframe;
            if (IsStackframeNull(stackframe) == false)
            {
                steps.Add(stackframe);
                do
                {
                    stackframe = new StackFrame(startFrame, true);
                    steps.Add(stackframe);
                    //sb.Append(FormatStackFrame(stackframe));
                    startFrame++;
                } while (stackframe.GetMethod() != null);
            }


            if (steps.Count == 0)
                return "Unknown method";

            if (showFullStack)
                return string.Join("\n", steps.Select(m => formatter(m)));
            else
                return formatter(steps.First());
        }

        private static bool IsStackframeNull(StackFrame sf)
        {
            if (sf == null)
                return true;
            else if (sf.ToString() == "null\r\n")
                return true;
            else
                return false;
        }
        public static string ShortFormatStackFrame(StackFrame stackframe)
        {
            return FormatStackFrame(stackframe, true);
        }
            private static string FormatStackFrame(StackFrame stackframe, bool shortFormat)
        {
            if (stackframe == null || stackframe.GetMethod() == null)
                return string.Empty;

            if (shortFormat)
                return $"{stackframe.GetMethod().ReflectedType.FullName.Replace("+",".")}.{stackframe.GetMethod().Name} {(stackframe.GetFileName()== null ? "" : new System.IO.FileInfo( stackframe.GetFileName())?.Name)}({stackframe.GetFileLineNumber()},{stackframe.GetFileColumnNumber()})";
            else
                return (
                    string.Format("{0}.{1} (line {2}, col {3} in {4})",
                        stackframe.GetMethod().ReflectedType.FullName,
                        stackframe.GetMethod().Name,
                        stackframe.GetFileLineNumber().ToString(),
                        stackframe.GetFileColumnNumber(),
                        stackframe.GetFileName()
                        )
                    );
            //return stackframe.ToString();

        }

    }
}
