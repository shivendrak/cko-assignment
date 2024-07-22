using MediatR;

/// <summary>
/// Handles the processing of payment commands.
/// </summary>
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponse>
{
    private readonly IBankClient _bankClient;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IBankClient bankClient,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _bankClient = bankClient ?? throw new ArgumentNullException(nameof(bankClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentEntity = request.ToPaymentEntity();

        try
        {
            await _paymentRepository.AddAsync(paymentEntity);
            _logger.LogInformation("Payment initiated with Id: {PaymentId} and Merchant Key: {MerchantTransactionKey}", paymentEntity.Id, request.MerchantTransactionKey);

            var bankRequest = request.ToBankRequest(paymentEntity.Id!);
            var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest);

            _logger.LogInformation("Bank response received for Payment Id: {PaymentId}, Status: {Status}", paymentEntity.Id, bankResponse.AuthorizationCode);

            ProcessBankResponse(bankResponse, paymentEntity);

            await _paymentRepository.UpdateAsync(paymentEntity);
            _logger.LogInformation("Payment updated with Id: {PaymentId}, Status: {Status}", paymentEntity.Id, paymentEntity.TransactionStatus);

            return paymentEntity.ToPaymentResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment with Id: {PaymentId}", paymentEntity.Id);
            paymentEntity.TransactionStatus = (int)TransactionStatus.Failed;
            await _paymentRepository.UpdateAsync(paymentEntity);
            throw new PaymentProcessingException($"Error processing payment with Id: {paymentEntity.Id}", ex);
        }
    }

    private void ProcessBankResponse(BankResponse bankResponse, PaymentEntity paymentEntity)
    {
        if (bankResponse == null)
        {
            throw new ArgumentNullException(nameof(bankResponse));
        }

        paymentEntity.TransactionStatus = (int)TransactionStatus.Completed;
        paymentEntity.BankAuthorizationCode = bankResponse.AuthorizationCode;
        paymentEntity.BankIsAuthorized = bankResponse.IsAuthorized;

        _logger.LogInformation("Processed bank response for Payment Id: {PaymentId}, Status: {Status}", paymentEntity.Id, paymentEntity.TransactionStatus);
    }
}