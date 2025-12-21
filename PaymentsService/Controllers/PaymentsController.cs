using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Persistence;

namespace PaymentsService.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentsDbContext _db;
    public PaymentsController(PaymentsDbContext db) => _db = db;

    // Создать счет пользователя (идемпотентно)
    [HttpPost("account")]
    public async Task<ActionResult<AccountView>> CreateAccount(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (acc is null)
        {
            acc = new Account
            {
                UserId = userId,
                Balance = 0m,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new AccountView(acc.UserId, acc.Balance));
    }

    // Пополнение счета
    [HttpPost("topup")]
    public async Task<ActionResult<AccountView>> TopUp([FromBody] TopUpRequest req, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        if (req.Amount <= 0) return BadRequest("Amount must be positive.");

        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (acc is null) return NotFound("Account not found. Create it first.");

        acc.Balance += req.Amount;
        acc.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new AccountView(acc.UserId, acc.Balance));
    }

    // Баланс счета
    [HttpGet("balance")]
    public async Task<ActionResult<AccountView>> Balance(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (acc is null) return NotFound("Account not found. Create it first.");

        return Ok(new AccountView(acc.UserId, acc.Balance));
    }

    private string GetUserIdOrThrow()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && !string.IsNullOrWhiteSpace(v))
            return v.ToString();
        throw new InvalidOperationException("Missing header X-User-Id");
    }
}

public record TopUpRequest(decimal Amount);
public record AccountView(string UserId, decimal Balance);
