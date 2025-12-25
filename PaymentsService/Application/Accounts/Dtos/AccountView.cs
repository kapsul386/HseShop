namespace PaymentsService.Application.Accounts.Dtos;

/// <summary>
/// Представление баланса пользователя (query-модель).
/// </summary>
public sealed record AccountView(string UserId, decimal Balance);