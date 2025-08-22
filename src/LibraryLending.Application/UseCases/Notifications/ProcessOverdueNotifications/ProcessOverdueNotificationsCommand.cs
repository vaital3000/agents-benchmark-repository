using MediatR;

namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public record ProcessOverdueNotificationsCommand() : IRequest;
