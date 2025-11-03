namespace SmartMail.Persistence;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string ConnectionString { get; set; } = string.Empty;
}
