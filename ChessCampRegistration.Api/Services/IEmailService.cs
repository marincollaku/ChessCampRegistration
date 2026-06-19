using ChessCampRegistration.Api.Models;

namespace ChessCampRegistration.Api.Services;

public interface IEmailService
{
    Task SendRegistrationConfirmationAsync(Registration registration, CancellationToken cancellationToken = default);
}
