using SmartMail.Domain.Common;

namespace SmartMail.Domain.Entities;

public class EmailAttachment : Entity<Guid>
{
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeInBytes { get; private set; }
    public byte[] Content { get; private set; }
    public Guid EmailId { get; private set; }

    private EmailAttachment() { }

    private EmailAttachment(
        Guid id,
        string fileName,
        string contentType,
        byte[] content,
        Guid emailId)
    {
        Id = id;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        SizeInBytes = content.Length;
        EmailId = emailId;
    }

    public static EmailAttachment Create(
        string fileName,
        string contentType,
        byte[] content,
        Guid emailId,
        long maxSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (content == null || content.Length == 0)
            throw new ArgumentException("Attachment content cannot be empty", nameof(content));

        if (content.Length > maxSizeInBytes)
            throw new ArgumentException(
                $"Attachment size ({content.Length} bytes) exceeds maximum allowed size ({maxSizeInBytes} bytes)",
                nameof(content));

        return new EmailAttachment(Guid.NewGuid(), fileName, contentType, content, emailId);
    }
}
