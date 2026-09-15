namespace Infrastructure.Configuration;

public class ContactOptions
{
    public const string SectionName = "Contact";
    public string AdminNotificationEmail { get; set; } = string.Empty;
}
