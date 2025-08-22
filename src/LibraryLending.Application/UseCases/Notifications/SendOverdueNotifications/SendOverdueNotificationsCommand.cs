using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.SendOverdueNotifications;

public record SendOverdueNotificationsCommand() : IRequest;

