namespace Note.Backend.Models;

public class BusinessExpense
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
