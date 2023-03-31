namespace HlidacStatu.Entities;

public interface IDocumentHash
{ 
    /// <summary>
    /// Calculates hash of all properties (except current dates), to check if two documents are the same.
    /// </summary>
    /// <returns></returns>
    string GetDocumentHash();
}