using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for handling payment-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator, ILogger<PaymentController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] ProcessPaymentCommand command)
    {
        if (command == null)
        {
            _logger.LogWarning("Received null ProcessPaymentCommand");
            return BadRequest("Invalid payment command");
        }
      
        _logger.LogInformation("Processing Payment: {MerchantTransactionKey}", command.MerchantTransactionKey);

        try
        {
            var response = await _mediator.Send(command);            
            _logger.LogInformation("Payment processed successfully: {MerchantTransactionKey}", command.MerchantTransactionKey);
            return Ok(response);
        }
        catch (PaymentValidationException ex)
        {
             _logger.LogWarning("Payment validation failed: {MerchantTransactionKey}", command.MerchantTransactionKey);
            return BadRequest(ex.Errors); 
        }
        catch (PaymentProcessingException ex)
        {
            _logger.LogError(ex, "Error processing payment: {MerchantTransactionKey}", command.MerchantTransactionKey);
            return StatusCode(500, "An error occurred while processing the payment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in payment processing: {MerchantTransactionKey}", command.MerchantTransactionKey);
            return StatusCode(500, "An unexpected error occurred while processing the payment.");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Received null or empty payment ID");
            return BadRequest("Invalid payment ID");
        }

        _logger.LogInformation("Retrieving payment details for ID: {PaymentId}", id);

        try
        {
            var query = new GetPaymentDetailsQuery(id);
            var payment = await _mediator.Send(query);
            _logger.LogInformation("Payment details retrieved successfully: {PaymentId}", id);
            return Ok(payment);
        }
        catch (PaymentNotFoundException ex)
        {
            _logger.LogWarning(ex, "Payment not found: {PaymentId}", id);
            return NotFound(ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid payment query: {PaymentId}", id);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment details: {PaymentId}", id);
            return StatusCode(500, "An unexpected error occurred while retrieving the payment details.");
        }
    }
}