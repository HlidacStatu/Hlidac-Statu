using System;

namespace HlidacStatu.Repositories.Searching.Rules
{
    public class TransformPrefix
        : TransformPrefixWithValue
    {

        public TransformPrefix(string prefix, string newPrefix, string valueConstrain, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(prefix, $"{newPrefix}${{q}}", valueConstrain, stopFurtherProcessing, addLastCondition)
        {
            if (newPrefix.Contains("${q}"))
                throw new ArgumentException("Use class TransformPrefixWithValue");
        }


    }
}
