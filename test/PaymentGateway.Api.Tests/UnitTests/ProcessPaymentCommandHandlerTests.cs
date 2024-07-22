using FluentValidation;
using FluentValidation.Results;

namespace PaymentGateway.Api.Tests.UnitTests;

public class ProcessPaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<IBankClient> _mockBankClient;
    private readonly Mock<IValidator<ProcessPaymentCommand>> _mockValidator;
    private readonly Mock<ILogger<ProcessPaymentCommandHandler>> _mockLogger;
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockBankClient = new Mock<IBankClient>();
        _mockValidator = new Mock<IValidator<ProcessPaymentCommand>>();
        _mockLogger = new Mock<ILogger<ProcessPaymentCommandHandler>>();
        _handler = new ProcessPaymentCommandHandler(
            _mockRepository.Object,
            _mockBankClient.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessfulPaymentResponse()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupValidCommand(command);
        SetupSuccessfulBankResponse();
        _mockValidator.Setup(r => r.ValidateAsync(command, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<PaymentResponse>()
            .Which.Status.Should().Be("Authorized");
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PaymentEntity>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PaymentEntity>()), Times.Once);
        _mockBankClient.Verify(b => b.ProcessPaymentAsync(It.IsAny<BankRequest>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await _handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    } 
   
    [Fact]
    public async Task Handle_BankClientThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupValidCommand(command);
        _mockBankClient.Setup(b => b.ProcessPaymentAsync(It.IsAny<BankRequest>()))
            .ThrowsAsync(new Exception("Bank error"));
        _mockValidator.Setup(r => r.ValidateAsync(command, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<PaymentProcessingException>();

        _mockRepository.Verify(r => r.UpdateAsync(It.Is<PaymentEntity>(p => p.TransactionStatus == (int)TransactionStatus.Failed)), Times.Once);
    }

    private ProcessPaymentCommand CreateValidCommand()
    {
        return new ProcessPaymentCommand(
            "Merchant1",
            "MerchTrans1",
            "1234567890123456",
            12,
            2025,
            "USD",
            1000,
            "123"
        );
    }

    private void SetupValidCommand(ProcessPaymentCommand command)
    {
        _mockValidator.Setup(v => v.Validate(command))
            .Returns(new ValidationResult());
    }

    private void SetupInvalidCommand(ProcessPaymentCommand command)
    {
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("CardNumber", "Invalid card number")
        };
        _mockValidator.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(validationFailures));
    }

    private void SetupSuccessfulBankResponse()
    {
        _mockBankClient.Setup(b => b.ProcessPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(new BankResponse(true, "AUTH123" ));
    }
}