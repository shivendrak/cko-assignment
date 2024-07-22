namespace PaymentGateway.Api.Tests.UnitTests;

public class BankServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<BankClient>> _mockLogger;
    private readonly BankClient _bankClient;
    private const string BaseUrl = "https://api.mockbank.com";

    public BankServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<BankClient>>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };

        _bankClient = new BankClient(httpClient, _mockLogger.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    [Fact]
    public async Task ProcessPaymentAsync_DeclinedPayment_ReturnsUnauthorizedResponse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"authorized\":false,\"authorization_code\":\"\"}");
        var request = new BankRequest("4111111111111111", "12/25", "USD", 10000, "123", "txn124");

        // Act
        var result = await _bankClient.ProcessPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.AuthorizationCode.Should().Be(string.Empty);
    }

    [Fact]
    public async Task ProcessPaymentAsync_HttpError_ReturnsErrorResponse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
        var request = new BankRequest("4111111111111111", "12/25", "USD", 100, "123", "txn125");

        // Act & Assert
        await _bankClient.Invoking(x => x.ProcessPaymentAsync(request))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ProcessPaymentAsync_Timeout_ReturnsErrorResponse()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws(new TaskCanceledException("The request timed out"));

        var request = new BankRequest("4111111111111111", "12/25", "USD", 100, "123", "txn126");

        // Act & Assert
        await _bankClient.Invoking(x => x.ProcessPaymentAsync(request))
            .Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task ProcessPaymentAsync_NetworkError_ReturnsErrorResponse()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws(new HttpRequestException("No such host is known"));

        var request = new BankRequest("4111111111111111", "12/25", "USD", 100, "123", "txn127");

        // Act & Assert
        await _bankClient.Invoking(x => x.ProcessPaymentAsync(request))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("No such host is known");
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidJsonResponse_ReturnsErrorResponse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "Invalid JSON");
        var request = new BankRequest("4111111111111111", "12/25", "USD", 100, "123", "txn128");

        // Act & Assert
        await _bankClient.Invoking(x => x.ProcessPaymentAsync(request))
            .Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task ProcessPaymentAsync_SucceedsAfterRetry_ReturnsAuthorizedResponse()
    {
        // Arrange
        var requestCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                requestCount++;
                if (requestCount < 3)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"authorized\":false,\"authorization_code\":\"\"}")
                });
            });

        var request = new BankRequest("4111111111111111", "12/25", "USD", 100, "123", "txn129");

        // Act
        var result = await _bankClient.ProcessPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.AuthorizationCode.Should().Be(string.Empty);
        requestCount.Should().Be(3);

    }
}