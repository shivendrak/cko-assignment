using System.Collections.Concurrent;

/// <summary>
/// In-memory implementation of IPaymentRepository for storing and retrieving payment entities.
/// Uses ConcurrentDictionary for thread-safe operations.
/// </summary>
public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<string, PaymentEntity> _payments = new ConcurrentDictionary<string, PaymentEntity>();
    private readonly ILogger<InMemoryPaymentRepository> _logger;

    public InMemoryPaymentRepository(ILogger<InMemoryPaymentRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentEntity?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Attempted to get payment with null or empty ID");
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }

        _logger.LogDebug("Attempting to retrieve payment with ID: {PaymentId}", id);

        if (_payments.TryGetValue(id, out PaymentEntity? payment))
        {
            _logger.LogInformation("Successfully retrieved payment with ID: {PaymentId}", id);
            return payment;
        }

        _logger.LogWarning("Payment with ID: {PaymentId} not found", id);

        // This statement is to supress the warning
        // warning CS1998: This async method lacks 'await' operators
        await Task.CompletedTask;

        return null;
    }

    public async Task AddAsync(PaymentEntity payment)
    {
        if (payment == null)
        {
            _logger.LogError("Attempted to add null payment");
            throw new ArgumentNullException(nameof(payment));
        }

        payment.Id = Guid.NewGuid().ToString();
        _logger.LogDebug("Attempting to add payment with ID: {PaymentId}", payment.Id);

        if (!_payments.TryAdd(payment.Id, payment))
        {
            _logger.LogWarning("Failed to add payment. A payment with ID: {PaymentId} already exists", payment.Id);
            throw new InvalidOperationException($"A payment with ID {payment.Id} already exists.");
        }

        _logger.LogInformation("Successfully added payment with ID: {PaymentId}", payment.Id);

        // This statement is to supress the warning
        // warning CS1998: This async method lacks 'await' operators
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(PaymentEntity payment)
    {
        if (payment == null)
        {
            _logger.LogError("Attempted to update null payment");
            throw new ArgumentNullException(nameof(payment));
        }

        if (string.IsNullOrWhiteSpace(payment.Id))
        {
            _logger.LogError("Attempted to update payment with null or empty ID");
            throw new ArgumentException("Payment ID cannot be null or empty", nameof(payment));
        }

        _logger.LogDebug("Attempting to update payment with ID: {PaymentId}", payment.Id);

        if (_payments.TryGetValue(payment.Id, out _))
        {
            _payments[payment.Id] = payment;
            _logger.LogInformation("Successfully updated payment with ID: {PaymentId}", payment.Id);
        }
        else
        {
            _logger.LogWarning("Failed to update payment. Payment with ID: {PaymentId} not found", payment.Id);
            throw new KeyNotFoundException($"Payment with ID {payment.Id} not found.");
        }

        // This statement is to supress the warning
        // warning CS1998: This async method lacks 'await' operators
        await Task.CompletedTask;
    }
}