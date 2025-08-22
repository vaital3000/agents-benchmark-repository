using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.GetNotification;

public record GetNotificationQuery(Guid LoanId) : IRequest<OverdueNotificationDto?>;
