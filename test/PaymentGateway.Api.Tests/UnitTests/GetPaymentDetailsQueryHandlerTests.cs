using FluentValidation;
using FluentValidation.Results;

namespace PaymentGateway.Api.Tests.UnitTests;

public class GetPaymentDetailsQueryHandlerTests
{
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private readonly Mock<ILogger<GetPaymentDetailsQueryHandler>> _loggerMock;
    private readonly GetPaymentDetailsQueryHandler _handler;
    private readonly Mock<IValidator<GetPaymentDetailsQuery>> _mockValidator;

    public GetPaymentDetailsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<GetPaymentDetailsQueryHandler>>();
        _mockValidator = new Mock<IValidator<GetPaymentDetailsQuery>>();
        _handler = new GetPaymentDetailsQueryHandler(_repositoryMock.Object, _loggerMock.Object); //, _mockValidator.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaymentResponse()
    {
        // Arrange
        var paymentId = "PaymentId1";
        var query = new GetPaymentDetailsQuery(paymentId);
        var paymentEntity = new PaymentEntity
        {
            Id = paymentId,
            MerchantId = paymentId,
            MerchantTransactionKey = Guid.NewGuid().ToString(),
            TransactionStatus = (int)TransactionStatus.Completed,
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            CVV = "222",
            BankIsAuthorized = true,
            BankAuthorizationCode = "23423423"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync(paymentEntity);
        _mockValidator.Setup(r => r.ValidateAsync(query, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(paymentId);
        result.Status.Should().Be("Authorized");
        result.LastFourCardDigits.Should().Be("3456");
        result.ExpiryMonth.Should().Be(paymentEntity.ExpiryMonth);
        result.ExpiryYear.Should().Be(paymentEntity.ExpiryYear);
        result.Currency.Should().Be("USD");
        result.Amount.Should().Be(paymentEntity.Amount);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await  _handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var paymentId = "NonExistentId";
        var query = new GetPaymentDetailsQuery(paymentId);

        _repositoryMock.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync((PaymentEntity)null!);
        _mockValidator.Setup(r => r.ValidateAsync(query, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_Rethrows()
    {
        // Arrange
        var paymentId = "PaymentId1";
        var query = new GetPaymentDetailsQuery(paymentId);
        var exception = new Exception("Database error");

        _repositoryMock.Setup(r => r.GetByIdAsync(paymentId))
            .ThrowsAsync(exception);

        _mockValidator.Setup(r => r.ValidateAsync(query, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage(exception.Message);
    }
}
