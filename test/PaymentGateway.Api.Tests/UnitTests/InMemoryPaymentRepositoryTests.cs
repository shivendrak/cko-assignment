namespace PaymentGateway.Api.Tests.UnitTests;

public class InMemoryPaymentRepositoryTests
{
    private readonly InMemoryPaymentRepository _repository;
    private readonly Mock<ILogger<InMemoryPaymentRepository>> _mockLogger;

    public InMemoryPaymentRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryPaymentRepository>>();
        _repository = new InMemoryPaymentRepository(_mockLogger.Object);
    }

    private PaymentEntity CreateTestPaymentEntity(string id = null!)
    {
        return new PaymentEntity
        {
            MerchantId = Guid.NewGuid().ToString(),
            MerchantTransactionKey = Guid.NewGuid().ToString(),
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 100,
            CVV = "123",
            TransactionStatus = (int)TransactionStatus.Initiated,
            Id = id,
        };
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsPayment()
    {
        // Arrange
        var payment = CreateTestPaymentEntity();
        await _repository.AddAsync(payment);

        // Act
        var result = await _repository.GetByIdAsync(payment.Id!);

        // Assert
        result.Should().NotBeNull();
        result?.Id.Should().Be(payment.Id);
        result?.Amount.Should().Be(payment.Amount);
        
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("invalid");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetByIdAsync_NullOrEmptyId_ThrowsArgumentException(string id)
    {
        Func<Task> act = async () => await  _repository.GetByIdAsync(id);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddAsync_ValidPayment_AddsPayment()
    {
        // Arrange
        var payment = CreateTestPaymentEntity();

        // Act
        await _repository.AddAsync(payment);

        // Assert
        var result = await _repository.GetByIdAsync(payment.Id!);
        result.Should().NotBeNull();
        result?.Amount.Should().Be(payment.Amount);
        result?.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task AddAsync_NullPayment_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = async () => await _repository.AddAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(Skip = "This test is relevant when duplication detection is implemented. It has been included to highlight a necessity in the production system.")]
    public async Task AddAsync_DuplicatePayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = CreateTestPaymentEntity();
        await _repository.AddAsync(payment);

        // Act & Assert
        Func<Task> act = async () => await _repository.AddAsync(payment);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidPayment_UpdatesPayment()
    {
        // Arrange
        var payment = CreateTestPaymentEntity();
        await _repository.AddAsync(payment);
        payment.Amount = 200;

        // Act
        await _repository.UpdateAsync(payment);

        // Assert
        var result = await _repository.GetByIdAsync(payment.Id!);
        result.Should().NotBeNull();
        result?.Amount.Should().Be(payment.Amount);
    }

    [Fact]
    public async Task UpdateAsync_NullPayment_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = async () => await  _repository.UpdateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var payment = CreateTestPaymentEntity();
        payment.Id = "invalid";

        // Act & Assert
        Func<Task> act = async () => await _repository.UpdateAsync(payment);
        await act.Should().ThrowAsync<KeyNotFoundException>();        
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task UpdateAsync_NullOrEmptyId_ThrowsArgumentException(string paymentId)
    {
        // Arrange
        var payment = CreateTestPaymentEntity(paymentId);
        Func<Task> act = async () => await _repository.UpdateAsync(payment);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}

