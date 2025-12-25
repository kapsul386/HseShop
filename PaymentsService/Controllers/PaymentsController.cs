using Microsoft.AspNetCore.Mvc;
using PaymentsService.Application.Accounts;
using PaymentsService.Application.Accounts.Dtos;

namespace PaymentsService.Controllers;

/// <summary>
/// HTTP API для операций со счетом пользователя: создание, пополнение, просмотр баланса.
/// </summary>
[ApiController]
[Route("payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IAccountsService _accounts;

    public PaymentsController(IAccountsService accounts) => _accounts = accounts;

    /// <summary>
    /// Создаёт счёт пользователя (идемпотентно).
    /// </summary>
    [HttpPost("account")]
    public async Task<ActionResult<AccountView>> CreateAccount(CancellationToken ct)
        => Ok(await _accounts.CreateAccountAsync(ct));

    /// <summary>
    /// Пополняет баланс пользователя.
    /// </summary>
    [HttpPost("topup")]
    public async Task<ActionResult<AccountView>> TopUp([FromBody] TopUpRequest req, CancellationToken ct)
        => Ok(await _accounts.TopUpAsync(req, ct));

    /// <summary>
    /// Возвращает текущий баланс пользователя.
    /// </summary>
    [HttpGet("balance")]
    public async Task<ActionResult<AccountView>> Balance(CancellationToken ct)
    {
        var acc = await _accounts.GetBalanceAsync(ct);
        return acc is null ? NotFound("Account not found. Create it first.") : Ok(acc);
    }
}