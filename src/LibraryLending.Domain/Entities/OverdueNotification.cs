using LibraryLending.Domain.Enums;

namespace LibraryLending.Domain.Entities;

public class OverdueNotification
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private OverdueNotification() { }

    public OverdueNotification(Guid loanId)
    {
        Id = Guid.NewGuid();
        LoanId = loanId;
        CreatedAt = DateTime.UtcNow;
        Status = NotificationStatus.Pending;
    }

    public void MarkSent(DateTime sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
    }
}
