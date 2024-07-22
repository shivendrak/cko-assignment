
/// <summary>
/// Represents exceptions that occur during payment validation.
/// </summary>
public class PaymentValidationException : Exception
{
     /// <summary>
    /// Gets the detailed information about the validation errors.
    /// </summary>
    public IList<string> Errors { get; }

    public PaymentValidationException(string message, IList<string> validationErrors) 
        : base(message)
    {
        Errors = validationErrors;
    }
}

/// <summary>
/// Represents exceptions that occur when a payment is not found.
/// </summary>
public class PaymentNotFoundException : Exception
{
    public PaymentNotFoundException(string paymentId) 
        : base($"Payment not found for ID: {paymentId}") {}
}

/// <summary>
/// Represents exceptions that occur during payment processing.
/// </summary>
public class PaymentProcessingException : Exception
{
    public PaymentProcessingException(string message, Exception innerException) 
        : base(message, innerException) {}
}