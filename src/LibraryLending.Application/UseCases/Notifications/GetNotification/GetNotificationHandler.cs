using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.GetNotification;

public class GetNotificationHandler : IRequestHandler<GetNotificationQuery, OverdueNotificationDto?>
{
    private readonly IOverdueNotificationRepository _repository;

    public GetNotificationHandler(IOverdueNotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<OverdueNotificationDto?> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByLoanIdAsync(request.LoanId, cancellationToken);
        if (notification is null)
            return null;

        return new OverdueNotificationDto(notification.LoanId, notification.Status.ToString());
    }
}
