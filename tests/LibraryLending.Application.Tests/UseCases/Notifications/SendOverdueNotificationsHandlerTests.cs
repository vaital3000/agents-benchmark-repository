using FluentAssertions;
using LibraryLending.Application.Services;
using LibraryLending.Application.UseCases.Notifications.SendOverdueNotifications;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using Moq;

namespace LibraryLending.Application.Tests.UseCases.Notifications;

public class SendOverdueNotificationsHandlerTests
{
    private static Loan CreateOverdueLoan()
    {
        var book = new Book(Isbn.Create("9780134685991"), "Test", "Author", 1);
        var patron = new Patron("Test Patron", Email.Create("test@example.com"));
        var loan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-15));
        loan.GetType().GetProperty("Book")!.SetValue(loan, book);
        loan.GetType().GetProperty("Patron")!.SetValue(loan, patron);
        return loan;
    }

    [Fact]
    public async Task ShouldSendNotificationAndRecordHistory_WhenLoanIsOverdue()
    {
        var loan = CreateOverdueLoan();
        var loanRepository = new InMemoryLoanRepository(new[] { loan });
        var notificationRepository = new InMemoryNotificationRepository();
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SendOverdueNotificationsHandler(loanRepository, notificationRepository, notificationService.Object);

        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);

        notificationService.Verify(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()), Times.Once);
        notificationRepository.Notifications.Should().HaveCount(1);
        var record = notificationRepository.Notifications.Single();
        record.Status.Should().Be(NotificationStatus.Sent);
        record.History.Should().HaveCount(1);
        record.History.First().Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task ShouldRetryNotification_WhenSendingFailsTemporarily()
    {
        var loan = CreateOverdueLoan();
        var loanRepository = new InMemoryLoanRepository(new[] { loan });
        var notificationRepository = new InMemoryNotificationRepository();
        var notificationService = new Mock<INotificationService>();
        notificationService.SetupSequence(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception())
            .Returns(Task.CompletedTask);

        var handler = new SendOverdueNotificationsHandler(loanRepository, notificationRepository, notificationService.Object);

        // First attempt fails
        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);
        notificationRepository.Notifications.Single().Status.Should().Be(NotificationStatus.Pending);
        notificationRepository.Notifications.Single().History.Should().HaveCount(1);
        notificationRepository.Notifications.Single().History.First().Status.Should().Be(NotificationStatus.Failed);

        // Second attempt succeeds
        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);
        notificationService.Verify(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()), Times.Exactly(2));
        notificationRepository.Notifications.Single().Status.Should().Be(NotificationStatus.Sent);
        notificationRepository.Notifications.Single().History.Should().HaveCount(2);
        notificationRepository.Notifications.Single().History.Last().Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task ShouldNotDuplicateNotifications_WhenRunMultipleTimes()
    {
        var loan = CreateOverdueLoan();
        var loanRepository = new InMemoryLoanRepository(new[] { loan });
        var notificationRepository = new InMemoryNotificationRepository();
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SendOverdueNotificationsHandler(loanRepository, notificationRepository, notificationService.Object);

        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);
        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);

        notificationService.Verify(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()), Times.Once);
        notificationRepository.Notifications.Should().HaveCount(1);
        notificationRepository.Notifications.Single().History.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldContinueProcessingAfterRestart_WhenNotificationsPending()
    {
        var loan = CreateOverdueLoan();
        var loanRepository = new InMemoryLoanRepository(new[] { loan });
        var notificationRepository = new InMemoryNotificationRepository();

        var failingService = new Mock<INotificationService>();
        failingService.Setup(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var handler = new SendOverdueNotificationsHandler(loanRepository, notificationRepository, failingService.Object);
        await handler.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);

        notificationRepository.Notifications.Single().Status.Should().Be(NotificationStatus.Pending);

        var successService = new Mock<INotificationService>();
        successService.Setup(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handlerAfterRestart = new SendOverdueNotificationsHandler(loanRepository, notificationRepository, successService.Object);
        await handlerAfterRestart.Handle(new SendOverdueNotificationsCommand(), CancellationToken.None);

        successService.Verify(s => s.SendOverdueNotificationAsync(loan.Patron, loan, It.IsAny<CancellationToken>()), Times.Once);
        notificationRepository.Notifications.Single().Status.Should().Be(NotificationStatus.Sent);
    }

    private class InMemoryNotificationRepository : IOverdueNotificationRepository
    {
        public List<OverdueNotification> Notifications { get; } = new();

        public Task AddAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<OverdueNotification?> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.SingleOrDefault(n => n.LoanId == loanId));
        }

        public Task UpdateAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class InMemoryLoanRepository : ILoanRepository
    {
        private readonly List<Loan> _loans;

        public InMemoryLoanRepository(IEnumerable<Loan> loans)
        {
            _loans = loans.ToList();
        }

        public Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
        {
            _loans.Add(loan);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Loan>> GetActiveLoansForPatronAsync(Guid patronId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loans.Where(l => l.PatronId == patronId && !l.IsReturned));
        }

        public Task<IEnumerable<Loan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loans.Where(l => !l.IsReturned));
        }

        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loans.SingleOrDefault(l => l.Id == id));
        }

        public Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

