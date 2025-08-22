using System.Linq;
using LibraryLending.Application.Services;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Enums;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public class ProcessOverdueNotificationsHandler : IRequestHandler<ProcessOverdueNotificationsCommand>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IOverdueNotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;

    public ProcessOverdueNotificationsHandler(
        ILoanRepository loanRepository,
        IOverdueNotificationRepository notificationRepository,
        IEmailService emailService)
    {
        _loanRepository = loanRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
    }

    public async Task<Unit> Handle(ProcessOverdueNotificationsCommand request, CancellationToken cancellationToken)
    {
        var overdueLoans = await _loanRepository.GetOverdueLoansAsync(cancellationToken);
        foreach (var loan in overdueLoans)
        {
            var existing = await _notificationRepository.GetByLoanIdAsync(loan.Id, cancellationToken);
            if (existing is null)
            {
                var notification = new OverdueNotification(loan.Id);
                await _notificationRepository.AddAsync(notification, cancellationToken);
            }
        }

        var pendingNotifications = await _notificationRepository.GetPendingAsync(cancellationToken);
        foreach (var notification in pendingNotifications)
        {
            var loan = overdueLoans.FirstOrDefault(l => l.Id == notification.LoanId)
                ?? await _loanRepository.GetByIdAsync(notification.LoanId, cancellationToken);
            if (loan is null)
                continue;

            try
            {
                await _emailService.SendEmailAsync(
                    loan.Patron.Email.Value,
                    "Overdue book",
                    $"Please return '{loan.Book.Title}'",
                    cancellationToken);

                notification.MarkSent(DateTime.UtcNow);
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
            }
            catch
            {
                // leave as pending for retry
            }
        }

        return Unit.Value;
    }
}
