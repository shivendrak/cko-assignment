public interface IPaymentRepository
{
    Task<PaymentEntity?> GetByIdAsync(string id);
    Task AddAsync(PaymentEntity payment);
    Task UpdateAsync(PaymentEntity payment);
}

public class PaymentEntity
{
    public string? Id { get; set;}
    public required string MerchantId { get; set;}
    public required string MerchantTransactionKey { get; set;}
    public required string CardNumber {get; set;}
    public required int ExpiryMonth {get; set;}
    public required int ExpiryYear {get; set;}
    public required string Currency {get; set;}
    public required int Amount {get; set;}
    public required string CVV {get; set;}
    public required int TransactionStatus {get; set;}
    public string? BankAuthorizationCode {get;set;}
    public bool? BankIsAuthorized { get; set; }
}

