using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _context;

    public LoanRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Patron)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetActiveLoansForPatronAsync(Guid patronId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Patron)
            .Where(l => l.PatronId == patronId && l.ReturnedAt == null)
            .OrderBy(l => l.DueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Patron)
            .Where(l => l.ReturnedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync(cancellationToken);
    }
}