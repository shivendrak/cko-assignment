
using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Tests.UnitTests;

public class PaymentControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<PaymentController>> _loggerMock;
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<PaymentController>>();
        _controller = new PaymentController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPayment_ValidCommand_ReturnsOkResult()
    {
        // Arrange
        var command = new ProcessPaymentCommand("Merchant1", "Trans1", "1234567890123456", 12, 2025, "USD", 1000, "123");
        var expectedResponse = new PaymentResponse("PaymentId1", "Authorized", "3456", 12, 2025, "USD", 1000);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ProcessPayment(command);

        // Assert
        var returnValue = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PaymentResponse>()
            .Which.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ProcessPayment_NullCommand_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ProcessPayment(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcessPayment_MediatorThrowsPaymentValidationException_ReturnsBadRequest()
    {
        var errors = new List<string>() { "There is an error" };
        // Arrange
        var command = new ProcessPaymentCommand("Merchant1", "Trans1", "1234567890123456", 12, 2025, "USD", 1000, "123");
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentValidationException("Rejected", errors));

        // Act
        var result = await _controller.ProcessPayment(command);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public async Task ProcessPayment_MediatorThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var command = new ProcessPaymentCommand("Merchant1", "Trans1", "1234567890123456", 12, 2025, "USD", 1000, "123");
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.ProcessPayment(command);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPaymentDetails_ValidId_ReturnsOkResult()
    {
        // Arrange
        var paymentId = "PaymentId1";
        var expectedResponse = new PaymentResponse(paymentId, "Authorized", "3456", 12, 2025, "USD", 1000);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPaymentDetails(paymentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PaymentResponse>()
            .Which.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetPaymentDetails_NullOrEmptyId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPaymentDetails("");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPaymentDetails_PaymentNotFound_ReturnsNotFound()
    {
        // Arrange
        var paymentId = "NonExistentId";
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException(paymentId));

        // Act
        var result = await _controller.GetPaymentDetails(paymentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPaymentDetails_MediatorThrowsKeyPaymentNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var paymentId = "NonExistentId";
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException(paymentId));

        // Act
        var result = await _controller.GetPaymentDetails(paymentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPaymentDetails_MediatorThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var paymentId = "PaymentId1";
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetPaymentDetails(paymentId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }
}

