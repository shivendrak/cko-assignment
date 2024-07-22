using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

using Xunit.Sdk;

namespace PaymentGateway.Api.Tests.IntegrationTests;
public class PaymentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IBankClient> _mockBankClient;

    public PaymentControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _mockBankClient = new Mock<IBankClient>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[] {new KeyValuePair<string, string?>("AllowedCurrencies", "USD,EUR,GBP")});
            });
            
            builder.ConfigureServices(services =>
            {
                // Remove the existing IPaymentRepository and IBankClient registrations
                var descriptors = services.Where(
                    d => d.ServiceType == typeof(IPaymentRepository) ||
                         d.ServiceType == typeof(IBankClient)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add InMemoryPaymentRepository
                services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

                // Add mock IBankClient
                services.AddSingleton(_mockBankClient.Object);
            });
        });
    }

    [Fact]
    public async Task ProcessPayment_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new ProcessPaymentCommand("Merchant1", "Trans1", "1234567890123456", 12, DateTime.Now.AddYears(1).Year, "USD", 1000, "123");
        var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");

        _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(new BankResponse(true, "AUTH123"));

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseString = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonConvert.DeserializeObject<PaymentResponse>(responseString);



        paymentResponse.Should().NotBeNull();
        paymentResponse.Should().BeEquivalentTo(command, options=> options
            .Excluding(o=> o.MerchantId)
            .Excluding(o=> o.MerchantTransactionKey)
            .Excluding(o => o.CardNumber)
            .Excluding(o=> o.CVV)
        );
        
        paymentResponse.Status.Should().Be("Authorized");
        paymentResponse.LastFourCardDigits.Should().Be(command.CardNumber[^4..]);
        paymentResponse.Id.Should().NotBeNull();      

        _mockBankClient.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_BankDeclines_ReturnsDeclined()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new ProcessPaymentCommand("Merchant1", "Trans2", "1234567890123456", 12, 2025, "USD", 1000, "123");
        var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");

        _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()))
            .ReturnsAsync(new BankResponse(false, ""));

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseString = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonConvert.DeserializeObject<PaymentResponse>(responseString);

        paymentResponse.Should().NotBeNull();
        paymentResponse.Status.Should().Be("Declined");
        paymentResponse.LastFourCardDigits.Should().Be("3456");

        _mockBankClient.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_BankClientThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new ProcessPaymentCommand("Merchant1", "Trans3", "1234567890123456", 12, 2025, "USD", 1000, "123");
        var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");

        _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()))
            .ThrowsAsync(new Exception("Bank service unavailable"));

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _mockBankClient.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidCommand = new ProcessPaymentCommand("", "", "", 0, 0, "", 0, "");
        var content = new StringContent(JsonConvert.SerializeObject(invalidCommand), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentDetails_ExistingPayment_ReturnsOkResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        var repository = _factory.Services.GetRequiredService<IPaymentRepository>();

        // Create a payment to retrieve
        var payment = new PaymentEntity
        {
            MerchantId = "Merchant1",
            MerchantTransactionKey = "Trans1",
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            CVV = "123",
            TransactionStatus = (int)TransactionStatus.Completed
        };
        await repository.AddAsync(payment);

        // Act
        var response = await client.GetAsync($"/api/payment/{payment.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseString = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonConvert.DeserializeObject<PaymentResponse>(responseString);

        paymentResponse.Should().NotBeNull();
        paymentResponse.Amount.Should().Be(payment.Amount);
        paymentResponse.LastFourCardDigits.Should().Be(payment.CardNumber.Substring(payment.CardNumber.Length - 4));
        paymentResponse.Currency.Should().Be(payment.Currency);
        paymentResponse.ExpiryMonth.Should().Be(payment.ExpiryMonth);
        paymentResponse.ExpiryYear.Should().Be(payment.ExpiryYear);
        paymentResponse.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GetPaymentDetails_NonExistentPayment_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentPaymentId = Guid.NewGuid().ToString();

        // Act
        var response = await client.GetAsync($"/api/payment/{nonExistentPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public async Task ProcessPayment_DifferentCurrencies_ReturnsOkResult(string currency)
    {

        _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()))
           .ReturnsAsync((BankRequest br) => new BankResponse(true, br.TransactionId));

        // Arrange
        var client = _factory.CreateClient();
        var command = new ProcessPaymentCommand("Merchant1", $"Trans_{currency}", "1234567890123456", 12, 2025, currency, 1000, "123");
        var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonConvert.DeserializeObject<PaymentResponse>(responseString);

        paymentResponse.Should().NotBeNull();
        paymentResponse.Currency.Should().Be(currency);

    }

    [Fact]
    public async Task ProcessPayment_ExpiredCard_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var pastYear = DateTime.Now.Year - 1;
        var command = new ProcessPaymentCommand("Merchant1", "Trans_Expired", "1234567890123456", 12, pastYear, "USD", 1000, "123");
        var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_MalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var malformedJson = "{\"merchantId\":\"Merchant1\",\"amount\":1000,}"; // Note the extra comma
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payment", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_ConcurrentRequests_AllSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();

        _mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankRequest>()))
           .ReturnsAsync((BankRequest br) => new BankResponse(true, br.TransactionId));

        for (int i = 0; i < 10; i++)
        {
            var command = new ProcessPaymentCommand("Merchant1", $"Trans_Concurrent_{i}", "1234567890123456", 12, 2025, "USD", 1000, "123");
            var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            tasks.Add(client.PostAsync("/api/payment", content));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            task.Result.EnsureSuccessStatusCode();
        }
    }

}
