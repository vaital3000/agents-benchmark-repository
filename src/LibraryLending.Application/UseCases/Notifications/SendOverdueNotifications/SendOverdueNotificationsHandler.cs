using LibraryLending.Application.Services;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.SendOverdueNotifications;

public class SendOverdueNotificationsHandler : IRequestHandler<SendOverdueNotificationsCommand>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IOverdueNotificationRepository _notificationRepository;
    private readonly INotificationService _notificationService;

    public SendOverdueNotificationsHandler(
        ILoanRepository loanRepository,
        IOverdueNotificationRepository notificationRepository,
        INotificationService notificationService)
    {
        _loanRepository = loanRepository;
        _notificationRepository = notificationRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(SendOverdueNotificationsCommand request, CancellationToken cancellationToken)
    {
        var loans = await _loanRepository.GetAllActiveAsync(cancellationToken);

        foreach (var loan in loans.Where(l => l.IsOverdue))
        {
            var notification = await _notificationRepository.GetByLoanIdAsync(loan.Id, cancellationToken);
            if (notification != null && notification.Status == NotificationStatus.Sent)
                continue;

            var isNew = notification == null;
            notification ??= new OverdueNotification(loan.Id, loan.PatronId);

            try
            {
                await _notificationService.SendOverdueNotificationAsync(loan.Patron, loan, cancellationToken);
                notification.MarkSent(DateTime.UtcNow);
            }
            catch
            {
                notification.RecordFailure(DateTime.UtcNow);
            }

            if (isNew)
                await _notificationRepository.AddAsync(notification, cancellationToken);
            else
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }

        return Unit.Value;
    }
}

