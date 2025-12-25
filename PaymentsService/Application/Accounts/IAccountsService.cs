using PaymentsService.Application.Accounts.Dtos;

namespace PaymentsService.Application.Accounts;

/// <summary>
/// Контракт для операций со счётом пользователя.
/// Все операции выполняются в контексте текущего userId (из HTTP-запроса).
/// </summary>
public interface IAccountsService
{
    /// <summary>
    /// Создаёт счёт пользователя, если он ещё не создан (идемпотентно).
    /// </summary>
    Task<AccountView> CreateAccountAsync(CancellationToken ct);

    /// <summary>
    /// Пополняет баланс текущего пользователя на указанную сумму.
    /// </summary>
    Task<AccountView> TopUpAsync(TopUpRequest req, CancellationToken ct);

    /// <summary>
    /// Возвращает текущий баланс пользователя или null, если счёт отсутствует.
    /// </summary>
    Task<AccountView?> GetBalanceAsync(CancellationToken ct);
}