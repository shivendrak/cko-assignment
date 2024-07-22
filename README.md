# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read the [README.md](https://github.com/cko-recruitment) in the root of this organisation, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

## Required Setup

1. .Net 8
1. Docker
1. Any IDE (possibly VS Code)


## How to Run / Test

Clone the repo and use any of the following options.

1. Docker Compose (To Run)
    a. Open Console
    b. navigate to /payment-gateway-challenge-dotnet
    c. execute `docker-compose up --build`
    d. Navigate to http://localhost:5000/swagger in a browser

1. Build Solution
    a. open Console
    b. navigate to /payment-gateway-challenge-dotnet
    c. execute `dotnet build PaymentGateway.sln`

1. Execute Tests
    a. Open Console
    b. navigate to /payment-gateway-challenge-dotnet
    c. execute `dotnet test PaymentGateway.sln` 


Visit the following url in any browser
http://localhost:5000/swagger

## Known Issue
1. The mock bank implementation does not respond when the input data does not match with its expected input. Due to this the payment gateway attempts retries and responds with 500 Internal server error. 

1. In the Process Payment input json, if 04 is passed as a value to month, instead of 4, the json coverter fails. A found a solution to building my custom json converter, but it seems a bit too much for now. 

## Design Decisions

### Project Structure
There are few implementations which has associated classes in the same file. This is done in the interest not overdoing the folder structure and keep the project readable for reviewers. In a production quality implementation, there will me many more classes and so a better folder structure will be a necessity. 

#### In Memory DB implementation: 
To simplify the process, I've implemented an in-memory payment repository. This setup is designed to simulate a database but doesn't incorporate best practices or high-quality considerations. In a real project, an ORM would typically be used to interact with the database, likely requiring a more complex setup with additional classes beyond a basic repository.

Additionally, I am not saving the requests which are getting rejected due to incorrect input.

#### Handling Distributed Transaction:
```While I have given this ample consideration, it appears that this part is complex to implement and is not mentioned in the requirements. Therefore, to maintain simplicity, I am deeming this implementation as out of scope for this exercise.```

Since this is a payment gateway that interacts with an external Bank API, there is a possibility that either system could go down during a transaction. This could result in an incorrect transaction state, particularly if the end user's payment has been deducted. To address this, I suggest the following:

1. **Implement Idempotency:**
   - Assign a unique idempotency key to each transaction request.
   - Store this key with the transaction details before making the bank request.
   - If a retry occurs, check if a transaction with this key already exists to prevent duplicates.

2. **Implement a Reconciliation Process:**
   - Periodically check with the bank for any discrepancies.
   - Automatically resolve issues or flag them for manual intervention.

To keep things simple, I have made a provision for the IdempotencyKey (MerchantTransactionKey), but the actual verification of duplicate transactions has not been implemented. Similarly, the Reconciliation service is not implemented. In my mind, the reconciliation service would run as a background job triggered periodically.

### BankService
The BankClient class is an abstraction for processing payments through a bank API. It encapsulates the logic for making HTTP requests to a bank's payment endpoint, handling responses, and managing errors. It also implements a retry policy using Polly library to handle transient failures and network issues.

### IPaymentValidator
While I have only added input validation at the service layer, I have build the validators using FluentValidations. The same validator can be added to Mediatr pipeline as well, so that the validations are performed at business layer as well. I have ommited this part for simpicity. 

### HealthCheck
The implemented healthchecks are totally useless in this application, they are kept to demonstrate the need in a production system and are no way implemented the way they must have. 





