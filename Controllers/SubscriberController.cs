using System.Security.Claims;
using IBSMobile.Contracts;
using IBSMobile.DTOs;
using IBSMobile.Statics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSMobile.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriberController : ControllerBase
{
    private readonly ISubscriberService _subscriberService;

    public SubscriberController(ISubscriberService subscriberService) =>
        _subscriberService = subscriberService;

    [HttpGet("financial-info")]
    public async Task<ActionResult<DtoFinancialInfo>> GetFinancialInfoAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.FinancialInfoAsync(userId));
    }

    [HttpGet("subscription-info")]
    public async Task<ActionResult<Response>> GetSubscriptionInfoAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.SubscriptionInfoAsync(userId));
    }

    [HttpGet("packages")]
    public async Task<ActionResult<List<DtoProfilePackage>>> GetPackagesAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.GetPackagesAsync(userId));
    }

    [HttpPost("refill")]
    public async Task<ActionResult<Response>> RefillSubscriptionAsync(
        [FromBody] DtoRefillRequest dto,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _subscriberService.RefillSubscriptionAsync(userId, dto.SaleType, dto.ProfileId);
        return result.error ? BadRequest(result) : Ok(result);
    }

    [HttpGet("amount-due")]
    public async Task<ActionResult<decimal>> GetAmountDueAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.GetAmountDue(userId));
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<List<DtoActivation>>> GetInvoicesAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.GetInvoicesAsync(userId));
    }

    [HttpGet("receivables")]
    public async Task<ActionResult<List<DtoReceivable>>> GetReceivablesAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        return Ok(await _subscriberService.GetReceivableAsync(userId));
    }

    [HttpPost("payback")]
    public async Task<ActionResult<Response>> PayBackAmountAsync(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _subscriberService.PayBackAmountAsync(userId);
        return result.error ? BadRequest(result) : Ok(result);
    }

    [HttpPost("payment")]
    public async Task<ActionResult<Response>> CreatePaymentAsync(
        [FromBody] DtoCreatePaymentRequest dto,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _subscriberService.CreatePaymentAsync(
            userId,
            dto.Amount,
            dto.ProfileId,
            dto.SaleType,
            string.IsNullOrWhiteSpace(dto.Purpose) && (dto.ProfileId is null or <= 0)
                ? "Debt"
                : dto.Purpose,
            dto.ReturnUrl);
        return result.error ? BadRequest(result) : Ok(result);
    }

    [HttpPost("payment/confirm")]
    public async Task<ActionResult<Response>> ConfirmPaymentAsync(
        [FromBody] DtoConfirmPaymentRequest dto,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _subscriberService.ConfirmPaymentAsync(
            userId,
            dto.PaymentId,
            dto.RequestId,
            dto.Status);
        return result.error ? BadRequest(result) : Ok(result);
    }

    private int GetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("معرف المستخدم غير صالح");

        return userId;
    }
}
