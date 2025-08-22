namespace LibraryLending.Application.DTOs;

public record OverdueNotificationDto(Guid LoanId, string Status);
