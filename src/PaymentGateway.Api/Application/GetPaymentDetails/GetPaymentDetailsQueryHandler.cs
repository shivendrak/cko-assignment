using MediatR;

/// <summary>
/// Handles queries for retrieving payment details.
/// </summary>
public class GetPaymentDetailsQueryHandler : IRequestHandler<GetPaymentDetailsQuery, PaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentDetailsQueryHandler> _logger;

    public GetPaymentDetailsQueryHandler(IPaymentRepository paymentRepository, ILogger<GetPaymentDetailsQueryHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentResponse> Handle(GetPaymentDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNullOrEmpty(request.PaymentId);

        _logger.LogInformation("Retrieving payment details for Payment ID: {PaymentId}", request.PaymentId);

        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for ID: {PaymentId}", request.PaymentId);
                throw new PaymentNotFoundException(request.PaymentId);
            }

            _logger.LogInformation("Payment details retrieved successfully for ID: {PaymentId}", request.PaymentId);

            return payment.ToPaymentResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment details for ID: {PaymentId}", request.PaymentId);
            throw;
        }
    }
}