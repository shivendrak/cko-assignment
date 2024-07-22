using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;

namespace PaymentGateway.Api.Tests.UnitTests;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator;
    private readonly Mock<IConfiguration> _configMock;

    public ProcessPaymentCommandValidatorTests()
    {
        var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("USD,EUR,GBP");
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x.GetSection("AllowedCurrencies")).Returns(configSection.Object);
        _validator = new ProcessPaymentCommandValidator(_configMock.Object);
    }

    private ProcessPaymentCommand CreateValidCommand() => new(
        MerchantId: "VALID_MERCHANT",
        MerchantTransactionKey: "VALID_KEY",
        CardNumber: "1234567890123456",
        ExpiryMonth: DateTime.Now.Month,
        ExpiryYear: DateTime.Now.Year + 1,
        Currency: "USD",
        Amount: 1000,
        CVV: "123"
    );

    [Fact]
    public void CardNumber_WhenValid_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("12345678901234567890")]
    [InlineData("1234abcd5678efgh")]
    public void CardNumber_WhenInvalid_ShouldHaveValidationError(string cardNumber)
    {
        var command = CreateValidCommand() with { CardNumber = cardNumber };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Fact]
    public void ExpiryYear_WhenInFuture_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { ExpiryYear = DateTime.Now.Year + 1 };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryYear);
    }

    [Fact]
    public void ExpiryYear_WhenInPast_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { ExpiryYear = DateTime.Now.Year - 1 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void ExpiryMonth_WhenValid_ShouldNotHaveValidationError(int month)
    {
        var command = CreateValidCommand() with { ExpiryMonth = month };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ExpiryMonth_WhenInvalid_ShouldHaveValidationError(int month)
    {
        var command = CreateValidCommand() with { ExpiryMonth = month };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Fact]
    public void ExpiryDate_WhenInFuture_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with 
        { 
            ExpiryMonth = DateTime.Now.Month, 
            ExpiryYear = DateTime.Now.Year + 1 
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExpiryDate_WhenInPast_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with 
        { 
            ExpiryMonth = DateTime.Now.Month, 
            ExpiryYear = DateTime.Now.Year - 1 
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void Currency_WhenValid_ShouldNotHaveValidationError(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("USDD")]
    [InlineData("JPY")]
    public void Currency_WhenInvalid_ShouldHaveValidationError(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000000)]
    public void Amount_WhenValid_ShouldNotHaveValidationError(int amount)
    {
        var command = CreateValidCommand() with { Amount = amount };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Amount_WhenInvalid_ShouldHaveValidationError(int amount)
    {
        var command = CreateValidCommand() with { Amount = amount };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void CVV_WhenValid_ShouldNotHaveValidationError(string cvv)
    {
        var command = CreateValidCommand() with { CVV = cvv };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CVV);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("abc")]
    public void CVV_WhenInvalid_ShouldHaveValidationError(string cvv)
    {
        var command = CreateValidCommand() with { CVV = cvv };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CVV);
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}