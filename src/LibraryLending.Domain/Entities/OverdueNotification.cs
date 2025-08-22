using System.Collections.ObjectModel;

namespace LibraryLending.Domain.Entities;

public class OverdueNotification
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid PatronId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public IReadOnlyCollection<NotificationHistory> History => _history.AsReadOnly();

    private readonly List<NotificationHistory> _history = new();

    // EF Core constructor
    private OverdueNotification() { }

    public OverdueNotification(Guid loanId, Guid patronId)
    {
        Id = Guid.NewGuid();
        LoanId = loanId;
        PatronId = patronId;
        Status = NotificationStatus.Pending;
    }

    public void MarkSent(DateTime date)
    {
        Status = NotificationStatus.Sent;
        _history.Add(new NotificationHistory(date, NotificationStatus.Sent));
    }

    public void RecordFailure(DateTime date)
    {
        _history.Add(new NotificationHistory(date, NotificationStatus.Failed));
    }
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public class NotificationHistory
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public NotificationStatus Status { get; private set; }

    // EF Core constructor
    private NotificationHistory() { }

    public NotificationHistory(DateTime date, NotificationStatus status)
    {
        Id = Guid.NewGuid();
        Date = date;
        Status = status;
    }
}

