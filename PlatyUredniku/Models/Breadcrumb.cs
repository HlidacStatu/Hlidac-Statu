using Nest;

namespace PlatyUredniku.Models;

public class Breadcrumb
{
    public string Name { get; set; }
    public string Action { get; set; }
    public string Id { get; set; }
    public int? Year { get; set; }
}