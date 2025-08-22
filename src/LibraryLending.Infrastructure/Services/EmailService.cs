using LibraryLending.Application.Services;
using System.Net.Http.Json;

namespace LibraryLending.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _client;

    public EmailService(HttpClient client)
    {
        _client = client;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var payload = new { to, subject, body };
        var response = await _client.PostAsJsonAsync("/send", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
