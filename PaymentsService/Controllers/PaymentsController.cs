using Microsoft.AspNetCore.Mvc;

namespace PaymentsService.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive.");

        if (request.OrderId == Guid.Empty)
            return BadRequest("OrderId is required.");

        return Ok(new
        {
            paymentId = Guid.NewGuid(),
            orderId = request.OrderId,
            amount = request.Amount,
            status = "created",
            createdAtUtc = DateTime.UtcNow
        });
    }
}

public record CreatePaymentRequest(Guid OrderId, decimal Amount);