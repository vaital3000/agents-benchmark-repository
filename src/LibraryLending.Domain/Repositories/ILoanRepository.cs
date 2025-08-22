using LibraryLending.Domain.Entities;

namespace LibraryLending.Domain.Repositories;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Loan>> GetActiveLoansForPatronAsync(Guid patronId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Loan>> GetOverdueLoansAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default);
}