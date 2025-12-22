using PaymentsService.Application.Accounts.Dtos;

namespace PaymentsService.Application.Accounts;

public interface IAccountsService
{
    Task<AccountView> CreateAccountAsync(CancellationToken ct);
    Task<AccountView> TopUpAsync(TopUpRequest req, CancellationToken ct);
    Task<AccountView?> GetBalanceAsync(CancellationToken ct);
}