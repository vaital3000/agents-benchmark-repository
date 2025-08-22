using LibraryLending.Domain.Entities;
namespace LibraryLending.Domain.Repositories;

public interface IOverdueNotificationRepository
{
    Task<OverdueNotification?> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task AddAsync(OverdueNotification notification, CancellationToken cancellationToken = default);
    Task<IEnumerable<OverdueNotification>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(OverdueNotification notification, CancellationToken cancellationToken = default);
}
