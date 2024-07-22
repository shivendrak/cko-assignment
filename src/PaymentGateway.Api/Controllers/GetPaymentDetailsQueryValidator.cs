using FluentValidation;

/// <summary>
/// Validates the GetPaymentDetailsQuery to ensure the payment ID is valid.
/// </summary>
public class GetPaymentDetailsQueryValidator : AbstractValidator<GetPaymentDetailsQuery>
{
    public GetPaymentDetailsQueryValidator()
    {
        RuleFor(query => query.PaymentId)
            .NotEmpty().WithMessage("Payment ID is required.")
            .Must(BeValidGuid).WithMessage("Payment ID must be a valid GUID.");
    }

    private bool BeValidGuid(string paymentId)
    {
        return Guid.TryParse(paymentId, out _);
    }
}