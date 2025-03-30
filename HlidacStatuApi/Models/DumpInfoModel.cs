namespace HlidacStatuApi.Models;

public class DumpInfoModel
{
    public string urlZip { get; set; }
    public string urlJson { get; set; }
    public DateTime? date { get; set; }
    public long size { get; set; }
    public bool fulldump { get; set; }
    public DateTime created { get; set; }
    public string dataType { get; set; }
}