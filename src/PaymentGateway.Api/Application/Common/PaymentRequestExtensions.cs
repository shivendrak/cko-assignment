/// <summary>
/// Provides extension methods for payment-related operations.
/// </summary>
public static class PaymentRequestExtensions
{
    public static BankRequest ToBankRequest(this ProcessPaymentCommand request, string tranasctionId)
    {
        return new BankRequest(request.CardNumber,
        $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
        request.Currency,
        request.Amount,
        request.CVV,
        tranasctionId);
    }

    public static PaymentEntity ToPaymentEntity(this ProcessPaymentCommand request)
    {
        return new PaymentEntity(){
            MerchantId = request.MerchantId,
            MerchantTransactionKey = request.MerchantTransactionKey,
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
            CVV = request.CVV,   
            TransactionStatus = (int)TransactionStatus.Initiated
        };
    }

    public static PaymentResponse ToPaymentResponse(this PaymentEntity entity)
    {
        var status = (entity.BankIsAuthorized != null && entity.BankIsAuthorized == true) ? "Authorized" : "Declined";
        return new PaymentResponse(
            entity.Id!,
            status,
            entity.CardNumber.Substring(entity.CardNumber.Length - 4),
            entity.ExpiryMonth,
            entity.ExpiryYear,
            entity.Currency,
            entity.Amount
        );
    }
}