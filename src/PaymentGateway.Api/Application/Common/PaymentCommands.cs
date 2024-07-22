using MediatR;

/// <summary>
/// Represents a command to process a payment.
/// </summary>
public record ProcessPaymentCommand(
    string MerchantId, 
    string MerchantTransactionKey,
    string CardNumber, 
    int ExpiryMonth, 
    int ExpiryYear, 
    string Currency, 
    int Amount, 
    string CVV) : IRequest<PaymentResponse>;

/// <summary>
/// Represents the response after processing a payment.
/// </summary>
public record PaymentResponse(
    string Id,
    string Status,
    string LastFourCardDigits,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount
);

/// <summary>
/// Represents the response when a payment is rejected.
/// </summary>
public record RejectedPaymentResponse(string Status, IList<string> Errors);

/// <summary>
/// Represents the possible statuses of a transaction.
/// </summary>
public enum TransactionStatus {  Initiated, Failed, Completed };

/// <summary>
/// Represents a query to get payment details.
/// </summary>
public record GetPaymentDetailsQuery(string PaymentId) : IRequest<PaymentResponse>;