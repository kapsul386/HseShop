using Microsoft.AspNetCore.Http;

namespace PaymentsService.Infrastructure.Http;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string UserId
    {
        get
        {
            var ctx = _accessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
            if (ctx.Request.Headers.TryGetValue("X-User-Id", out var v) && !string.IsNullOrWhiteSpace(v))
                return v.ToString();

            throw new InvalidOperationException("Missing header X-User-Id");
        }
    }
}