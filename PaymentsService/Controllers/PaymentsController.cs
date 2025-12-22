using Microsoft.AspNetCore.Mvc;
using PaymentsService.Application.Accounts;
using PaymentsService.Application.Accounts.Dtos;

namespace PaymentsService.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IAccountsService _accounts;

    public PaymentsController(IAccountsService accounts) => _accounts = accounts;

    [HttpPost("account")]
    public async Task<ActionResult<AccountView>> CreateAccount(CancellationToken ct)
        => Ok(await _accounts.CreateAccountAsync(ct));

    [HttpPost("topup")]
    public async Task<ActionResult<AccountView>> TopUp([FromBody] TopUpRequest req, CancellationToken ct)
        => Ok(await _accounts.TopUpAsync(req, ct));

    [HttpGet("balance")]
    public async Task<ActionResult<AccountView>> Balance(CancellationToken ct)
    {
        var acc = await _accounts.GetBalanceAsync(ct);
        return acc is null ? NotFound("Account not found. Create it first.") : Ok(acc);
    }
}