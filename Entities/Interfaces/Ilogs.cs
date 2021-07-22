using System;

namespace HlidacStatu.Entities
{
    public interface ILogs
    {
        DateTime Date { get; set; }
        string ProfileId { get; set; }
    }
}