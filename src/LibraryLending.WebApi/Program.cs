using FluentValidation;
using LibraryLending.Application.UseCases.Books.AddBook;
using LibraryLending.Application.UseCases.Books.GetBooks;
using LibraryLending.Application.UseCases.Loans.LoanBook;
using LibraryLending.Application.UseCases.Loans.ReturnBook;
using LibraryLending.Application.UseCases.Patrons.GetPatron;
using LibraryLending.Application.UseCases.Patrons.RegisterPatron;
using LibraryLending.Application.Services;
using LibraryLending.Domain.Repositories;
using LibraryLending.Infrastructure.Data;
using LibraryLending.Infrastructure.Repositories;
using LibraryLending.Infrastructure.Services;
using LibraryLending.WebApi.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MediatR
builder.Services.AddMediatR(typeof(RegisterPatronCommand).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RegisterPatronValidator).Assembly);

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("LibraryDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<LibraryDbContext>(options =>
        options.UseInMemoryDatabase("LibraryLendingDb"));
}
else
{
    builder.Services.AddDbContext<LibraryDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Add repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IPatronRepository, PatronRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IOverdueNotificationRepository, OverdueNotificationRepository>();
builder.Services.AddHttpClient<IEmailService, EmailService>(client =>
{
    var baseUrl = builder.Configuration["EmailService:BaseUrl"] ?? "http://localhost";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed data
    await SeedDataAsync(app);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    
    if (!context.Books.Any())
    {
        var book1 = new LibraryLending.Domain.Entities.Book(
            LibraryLending.Domain.ValueObjects.Isbn.Create("9780134685991"),
            "Effective Java",
            "Joshua Bloch",
            3);
            
        var book2 = new LibraryLending.Domain.Entities.Book(
            LibraryLending.Domain.ValueObjects.Isbn.Create("9780135166307"),
            "Clean Code",
            "Robert C. Martin",
            2);
            
        context.Books.AddRange(book1, book2);
    }
    
    if (!context.Patrons.Any())
    {
        var patron = new LibraryLending.Domain.Entities.Patron(
            "John Doe",
            LibraryLending.Domain.ValueObjects.Email.Create("john.doe@example.com"));
            
        context.Patrons.Add(patron);
    }
    
    await context.SaveChangesAsync();
}

public partial class Program { }
