namespace BLL.DTOs.Ec;

public class EcDTO
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string AvgPercent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}