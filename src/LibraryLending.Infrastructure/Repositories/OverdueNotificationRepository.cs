using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Enums;
using LibraryLending.Domain.Repositories;
using LibraryLending.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Repositories;

public class OverdueNotificationRepository : IOverdueNotificationRepository
{
    private readonly LibraryDbContext _context;

    public OverdueNotificationRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<OverdueNotification?> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications.FirstOrDefaultAsync(n => n.LoanId == loanId, cancellationToken);
    }

    public async Task AddAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
    {
        _context.OverdueNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<OverdueNotification>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
    {
        _context.OverdueNotifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
