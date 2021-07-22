namespace HlidacStatu.Entities
{
    public interface IAuditable
    {
        string ToAuditJson();
        string ToAuditObjectTypeName();
        string ToAuditObjectId();
    }
}