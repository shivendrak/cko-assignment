using FluentValidation;

/// <summary>
/// Validator for the ProcessPaymentCommand, ensuring all payment details are valid before processing.
/// </summary>
public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private readonly string[] _allowedCurrencies;

    public ProcessPaymentCommandValidator(IConfiguration configuration)
    {
        _allowedCurrencies = configuration.GetSection("AllowedCurrencies")
            .Value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        // Validate card number
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Card number is required.")
            .Length(14, 19).WithMessage("Card number must be between 14 and 19 characters long.")
            .Matches(@"^\d+$").WithMessage("Card number must only contain numeric characters.");

        // Validate expiry year
        RuleFor(x => x.ExpiryYear)
            .NotEmpty().WithMessage("Expiry year is required.")
            .Must(BeInTheFuture).WithMessage("Expiry year must be in the future.");

        // Validate expiry month
        RuleFor(x => x.ExpiryMonth)
            .NotEmpty().WithMessage("Expiry month is required.")
            .InclusiveBetween(1, 12).WithMessage("Expiry month must be between 1 and 12.");

        // Validate expiry date as a whole
        RuleFor(x => x)
            .Must(HaveValidExpiryDate)
            .WithMessage("The expiry date must be in the future.");

        // Validate currency
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be 3 characters long.")
            .Must(BeValidCurrency).WithMessage("Currency is not valid. Accepted currencies are: " + string.Join(", ", _allowedCurrencies));

        // Validate amount
        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Amount is required.")
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .Must(BeValidAmount).WithMessage("Amount must be an integer representing the minor currency unit.");

        // Validate CVV
        RuleFor(x => x.CVV)
            .NotEmpty().WithMessage("CVV is required.")
            .Length(3, 4).WithMessage("CVV must be 3 or 4 characters long.")
            .Matches(@"^\d+$").WithMessage("CVV must only contain numeric characters.");
    }

    private bool BeInTheFuture(int year)
    {
        return year > DateTime.Now.Year;
    }

    private bool HaveValidExpiryDate(ProcessPaymentCommand command)
    {
        if (command.ExpiryYear <= 0 || command.ExpiryMonth <= 0 || command.ExpiryMonth > 12)
        {
            return false;
        }

        var currentDate = DateTime.Now;
        var expiryDate = new DateTime(command.ExpiryYear, command.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
        return expiryDate > currentDate;
    }

    private bool BeValidCurrency(string currency)
    {
        return _allowedCurrencies.Contains(currency);
    }

    private bool BeValidAmount(int amount)
    {
        // This check ensures the amount is a positive integer.
        // The requirement for it to represent the minor currency unit is implicitly met
        // as we're accepting an integer value.
        return amount > 0;
    }
}