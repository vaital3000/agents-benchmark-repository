using System;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;
using LibraryLending.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace LibraryLending.E2E.Tests;

public class OverdueNotificationsTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public OverdueNotificationsTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Loan> SeedOverdueLoanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var book = new Book(Isbn.Create("9780132350884"), "Test", "Author", 1);
        var patron = new Patron("Patron", Email.Create("patron@test.local"));
        db.Books.Add(book);
        db.Patrons.Add(patron);
        var loan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-15)); // due yesterday
        db.Loans.Add(loan);
        await db.SaveChangesAsync();
        return loan;
    }

    [Fact(DisplayName = "Сценарий 1: успешное уведомление")]
    public async Task SuccessfulNotification()
    {
        var loan = await SeedOverdueLoanAsync();
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var client = _factory.CreateClient();
        var response = await client.PostAsync("/notifications/overdue/process", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.EmailServer.LogEntries.Count().Should().Be(1);
        var status = await client.GetFromJsonAsync<NotificationStatusDto>($"/notifications/{loan.Id}");
        status!.Status.Should().Be("Sent");
    }

    [Fact(DisplayName = "Сценарий 2: временный сбой доставки")]
    public async Task TemporaryFailureIsRetried()
    {
        var loan = await SeedOverdueLoanAsync();
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .InScenario("retry")
            .WillSetStateTo("failed")
            .RespondWith(Response.Create().WithStatusCode(500));
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .InScenario("retry")
            .WhenStateIs("failed")
            .RespondWith(Response.Create().WithStatusCode(200));

        var client = _factory.CreateClient();
        var first = await client.PostAsync("/notifications/overdue/process", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var status1 = await client.GetFromJsonAsync<NotificationStatusDto>($"/notifications/{loan.Id}");
        status1!.Status.Should().Be("Pending");

        var second = await client.PostAsync("/notifications/overdue/process", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.EmailServer.LogEntries.Count().Should().Be(2);
        var status2 = await client.GetFromJsonAsync<NotificationStatusDto>($"/notifications/{loan.Id}");
        status2!.Status.Should().Be("Sent");
    }

    [Fact(DisplayName = "Сценарий 3: отсутствие дублей")]
    public async Task NoDuplicates()
    {
        var loan = await SeedOverdueLoanAsync();
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var client = _factory.CreateClient();
        await client.PostAsync("/notifications/overdue/process", null);
        await client.PostAsync("/notifications/overdue/process", null);
        await client.PostAsync("/notifications/overdue/process", null);

        _factory.EmailServer.LogEntries.Count().Should().Be(1);
        var status = await client.GetFromJsonAsync<NotificationStatusDto>($"/notifications/{loan.Id}");
        status!.Status.Should().Be("Sent");
    }

    [Fact(DisplayName = "Сценарий 4: устойчивость к перезапуску")]
    public async Task ResilienceToRestart()
    {
        var loan = await SeedOverdueLoanAsync();
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .InScenario("restart")
            .WillSetStateTo("failed")
            .RespondWith(Response.Create().WithStatusCode(500));
        _factory.EmailServer
            .Given(Request.Create().WithPath("/send").UsingPost())
            .InScenario("restart")
            .WhenStateIs("failed")
            .RespondWith(Response.Create().WithStatusCode(200));

        var client = _factory.CreateClient();
        await client.PostAsync("/notifications/overdue/process", null);

        // simulate restart
        await _factory.DisposeAsync();
        var newFactory = new NotificationApiFactory();
        await newFactory.InitializeAsync();
        var newClient = newFactory.CreateClient();

        var second = await newClient.PostAsync("/notifications/overdue/process", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        newFactory.EmailServer.LogEntries.Count().Should().Be(2);
        var status = await newClient.GetFromJsonAsync<NotificationStatusDto>($"/notifications/{loan.Id}");
        status!.Status.Should().Be("Sent");

        await newFactory.DisposeAsync();
    }
}

public record NotificationStatusDto(Guid LoanId, string Status);
