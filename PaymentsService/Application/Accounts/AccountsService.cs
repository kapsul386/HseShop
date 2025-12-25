using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Accounts.Dtos;
using PaymentsService.Infrastructure.Http;
using PaymentsService.Persistence;

namespace PaymentsService.Application.Accounts;

/// <summary>
/// Бизнес-логика работы со счетами пользователей.
/// </summary>
public sealed class AccountsService : IAccountsService
{
    private readonly PaymentsDbContext _db;
    private readonly IUserContext _user;

    public AccountsService(PaymentsDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Идемпотентно создаёт счёт пользователя (не более одного счёта на userId).
    /// </summary>
    public async Task<AccountView> CreateAccountAsync(CancellationToken ct)
    {
        var userId = _user.UserId;

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

        return new AccountView(acc.UserId, acc.Balance);
    }

    /// <summary>
    /// Пополняет баланс пользователя.
    /// </summary>
    public async Task<AccountView> TopUpAsync(TopUpRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(req.Amount), "Amount must be positive.");

        var userId = _user.UserId;

        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (acc is null)
            throw new InvalidOperationException("Account not found. Create it first.");

        acc.Balance += req.Amount;
        acc.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AccountView(acc.UserId, acc.Balance);
    }

    /// <summary>
    /// Возвращает баланс пользователя или null, если счёт ещё не создан.
    /// </summary>
    public async Task<AccountView?> GetBalanceAsync(CancellationToken ct)
    {
        var userId = _user.UserId;

        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return acc is null ? null : new AccountView(acc.UserId, acc.Balance);
    }
}
