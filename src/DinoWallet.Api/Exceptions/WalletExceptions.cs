namespace DinoWallet.Api.Exceptions;

public class InsufficientFundsException : Exception
{
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(decimal currentBalance, decimal requestedAmount)
        : base($"Insufficient funds. Balance: {currentBalance}, Requested: {requestedAmount}")
    {
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

public class AccountNotFoundException : Exception
{
    public Guid AccountId { get; }

    public AccountNotFoundException(Guid accountId)
        : base($"Account {accountId} not found.")
    {
        AccountId = accountId;
    }
}

public class IdempotentRequestException : Exception
{
    public long ExistingTransactionId { get; }

    public IdempotentRequestException(long existingTransactionId)
        : base($"Request already processed. Transaction: {existingTransactionId}")
    {
        ExistingTransactionId = existingTransactionId;
    }
}

public class InvalidAmountException : Exception
{
    public InvalidAmountException(string message) : base(message) { }
}
