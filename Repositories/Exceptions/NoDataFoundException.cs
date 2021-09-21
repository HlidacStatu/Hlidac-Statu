using System;

namespace HlidacStatu.Repositories.Exceptions
{
    public class NoDataFoundException : Exception
    {
        public NoDataFoundException()
        {
        }

        public NoDataFoundException(string message)
            : base(message)
        {
        }

    }
}