using System.Diagnostics;
using Polly;
using Polly.Retry;
using Polly.Extensions.Http;
using System.Text.Json;

public interface IBankClient
{
    Task<BankResponse> ProcessPaymentAsync(BankRequest request);
}

/// <summary>
/// Implements IBankClient interface to process payments through a bank API.
/// Includes retry logic and logging for robust error handling.
/// </summary>
public class BankClient : IBankClient
{
    private readonly ILogger<BankClient> _logger;
    private readonly HttpClient _bankClient;

    // Defines the retry policy for HTTP requests
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private const string PaymentEndpoint = "/payments";

    public BankClient(HttpClient client, ILogger<BankClient> logger)
    {
        _bankClient = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Executes the bank request with retry policy
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrTransientHttpError()
            .WaitAndRetryAsync(
                3, // Number of retries
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryAttempt);
                }
            );
    }

    public async Task<BankResponse> ProcessPaymentAsync(BankRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Initiating bank transaction with id: {TransactionId} for amount {Amount} {Currency}", 
                request.TransactionId, request.Amount, request.Currency);

            var response = await ExecuteBankRequestAsync(request);
            var bankResponse = await DeserializeBankResponseAsync(response);

            LogSuccessfulTransaction(stopwatch, request, bankResponse);
            return bankResponse;
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    private async Task<HttpResponseMessage> ExecuteBankRequestAsync(BankRequest request)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var httpResponse = await _bankClient.PostAsJsonAsync(PaymentEndpoint, request);
            httpResponse.EnsureSuccessStatusCode();
            return httpResponse;
        });
    }

    private async Task<BankResponse> DeserializeBankResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            await LogFailedResponseAsync(response);
            throw new HttpRequestException($"Bank API returned non-success status code: {response.StatusCode}");
        }

        var bankResponse = await response.Content.ReadFromJsonAsync<BankResponse>();
        if (bankResponse == null)
        {
            throw new InvalidOperationException("Bank response was null or could not be deserialized.");
        }

        return bankResponse;
    }

    private async Task LogFailedResponseAsync(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError("Bank API returned non-success status code. Status: {StatusCode}, Content: {ErrorContent}", 
            (int)response.StatusCode, errorContent);
    }

    private void LogSuccessfulTransaction(Stopwatch stopwatch, BankRequest request, BankResponse bankResponse)
    {
        stopwatch.Stop();
        _logger.LogInformation("Bank API responded in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
        _logger.LogInformation("Bank transaction completed successfully. Transaction ID: {TransactionId}, Status: {IsAuthorized}, AuthorizationCode {AuthorizationCode}", 
            request.TransactionId, bankResponse.IsAuthorized, bankResponse.AuthorizationCode);
    }


    private void LogException(Exception ex)
    {
        switch (ex)
        {
            case HttpRequestException:
                _logger.LogError(ex, "HTTP error occurred while processing bank transaction");
                break;
            case JsonException:
                _logger.LogError(ex, "Error deserializing bank response");
                break;
            case TaskCanceledException:
                _logger.LogError(ex, "Bank API is not reachable");
                break;
            default:
                _logger.LogError(ex, "Unexpected error occurred while processing bank transaction");
                break;
        }
    }
}