namespace Note.Backend.Models;

public class PackChoice
{
    public int Id { get; set; }
    public string PackProductId { get; set; } = string.Empty;
    public Product PackProduct { get; set; } = null!;
    public string ChoiceProductId { get; set; } = string.Empty;
    public Product ChoiceProduct { get; set; } = null!;
}
