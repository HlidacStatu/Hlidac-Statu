using HlidacStatu.Connectors;
using HlidacStatu.Entities.VZ;

namespace HlidacStatu.Extensions;

public static class VerejnaZakazkaExtension
{
    public static byte[]? GetDocumentLocalCopy(this VerejnaZakazka.Document document)
    {
        var storageId = document.GetHlidacStorageId();
        var destination = Init.VzPrilohaLocalCopy.GetFullPath(storageId);
        if (System.IO.File.Exists(destination) == false)
            return null;
        
        return System.IO.File.ReadAllBytes(destination);
    }
    
    public static string GetHlidacUrl(this VerejnaZakazka.Document document, string vzId)
    {
        //https://www.hlidacstatu.cz/verejnezakazky/priloha/?id=b7cd40112f4c47d18e72de8627dfd11a&storageId=66F0F6A79EDC06D6C1FDC97E952F539A4A338D25C95627C1004E0C29A7605E38_507827
        return $"https://www.hlidacstatu.cz/verejnezakazky/priloha/?id={vzId}&storageId={document.GetHlidacStorageId()}";
    }
}