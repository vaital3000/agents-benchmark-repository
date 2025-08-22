using LibraryLending.Domain.Entities;

namespace LibraryLending.Application.Services;

public interface INotificationService
{
    Task SendOverdueNotificationAsync(Patron patron, Loan loan, CancellationToken cancellationToken = default);
}

