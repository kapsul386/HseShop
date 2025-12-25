namespace PaymentsService.Application.Accounts.Dtos;

/// <summary>
/// Запрос на пополнение баланса.
/// </summary>
public sealed record TopUpRequest(decimal Amount);