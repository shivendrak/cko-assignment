using System.Text.Json.Serialization;

public record BankRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; }

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; }

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string CVV { get; init; }

    [JsonIgnore]
    public string TransactionId { get; init; }

    public BankRequest(string cardNumber, string expiryDate, string currency, int amount, string cvv, string transactionId)
    {
        CardNumber = cardNumber;
        ExpiryDate = expiryDate;
        Currency = currency;
        Amount = amount;
        CVV = cvv;
        TransactionId = transactionId;
    }
}

public record BankResponse
{
    [JsonPropertyName("authorized")]
    public bool IsAuthorized { get; init; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; init; }

    [JsonConstructor]
    public BankResponse(bool isAuthorized, string authorizationCode)
    {
        IsAuthorized = isAuthorized;
        AuthorizationCode = authorizationCode;
    }
}