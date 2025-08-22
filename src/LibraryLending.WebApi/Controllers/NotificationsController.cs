using LibraryLending.Application.DTOs;
using LibraryLending.Application.UseCases.Notifications.GetNotification;
using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("overdue/process")]
    public async Task<IActionResult> ProcessOverdue(CancellationToken cancellationToken)
    {
        await _mediator.Send(new ProcessOverdueNotificationsCommand(), cancellationToken);
        return Ok();
    }

    [HttpGet("{loanId:guid}")]
    public async Task<ActionResult<OverdueNotificationDto>> Get(Guid loanId, CancellationToken cancellationToken)
    {
        var notification = await _mediator.Send(new GetNotificationQuery(loanId), cancellationToken);
        if (notification is null)
            return NotFound();
        return Ok(notification);
    }
}
