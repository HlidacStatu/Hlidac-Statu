namespace PoliticiEditor.Data;

public class PoliticiEditorUser
{
    public int Id { get; set; }
    public string NameId { get; set; }
    public string? Email { get; set; }
    public string? EmailUpper { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsLockedOut { get; set; }
}