namespace OrdersService.Infrastructure.Http;

/// <summary>
/// Контекст пользователя, извлекаемый из HTTP-запроса.
/// </summary>
public interface IUserContext
{
    string UserId { get; }
}