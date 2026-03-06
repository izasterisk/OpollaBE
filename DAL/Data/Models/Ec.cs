namespace DAL.Data.Models;

public class Ec
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal AvgPercent { get; set; }
    public DateTime CreatedAt { get; set; }
}
