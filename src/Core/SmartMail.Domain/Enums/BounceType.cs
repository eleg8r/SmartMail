namespace SmartMail.Domain.Enums;

public enum BounceType
{
    None = 0,
    Hard = 1,      // Permanent failure (invalid email, domain doesn't exist)
    Soft = 2,      // Temporary failure (mailbox full, server down)
    Complaint = 3, // Spam complaint
    Suppression = 4 // Email on suppression list
}
