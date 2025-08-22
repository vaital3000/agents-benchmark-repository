using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Patron> Patrons { get; set; } = null!;
    public DbSet<Loan> Loans { get; set; } = null!;
    public DbSet<OverdueNotification> OverdueNotifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to snake_case
        modelBuilder.Entity<Book>().ToTable("books");
        modelBuilder.Entity<Patron>().ToTable("patrons");
        modelBuilder.Entity<Loan>().ToTable("loans");

        // Configure Book entity
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            
            entity.Property(e => e.Isbn)
                .HasColumnName("isbn")
                .HasMaxLength(17)
                .IsRequired()
                .HasConversion(
                    isbn => isbn.Value,
                    value => Isbn.Create(value));

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Author)
                .HasColumnName("author")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.TotalCopies)
                .HasColumnName("total_copies")
                .IsRequired();

            entity.Property(e => e.AvailableCopies)
                .HasColumnName("available_copies")
                .IsRequired();

            entity.Property(e => e.RowVersion)
                .HasColumnName("row_version")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => e.Isbn).IsUnique();
        });

        // Configure Patron entity
        modelBuilder.Entity<Patron>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired()
                .HasConversion(
                    email => email.Value,
                    value => Email.Create(value));

            entity.Property(e => e.Active)
                .HasColumnName("active")
                .IsRequired();

            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Loan entity
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.BookId)
                .HasColumnName("book_id")
                .IsRequired();

            entity.Property(e => e.PatronId)
                .HasColumnName("patron_id")
                .IsRequired();

            entity.Property(e => e.LoanedAt)
                .HasColumnName("loaned_at")
                .IsRequired();

            entity.Property(e => e.DueAt)
                .HasColumnName("due_at")
                .IsRequired();

            entity.Property(e => e.ReturnedAt)
                .HasColumnName("returned_at");

            // Configure relationships
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patron)
                .WithMany()
                .HasForeignKey(e => e.PatronId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.BookId);
            entity.HasIndex(e => e.PatronId);
            entity.HasIndex(e => new { e.PatronId, e.ReturnedAt });
        });

        modelBuilder.Entity<OverdueNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("overdue_notifications");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoanId).HasColumnName("loan_id").IsRequired();
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.HasIndex(e => e.LoanId).IsUnique();
        });
    }
}