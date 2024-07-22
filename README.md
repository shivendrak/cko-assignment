## Required Setup

1. .Net 8
1. Docker
1. Any IDE (possibly VS Code)


## How to Run / Test

Clone the repo and use any of the following options.

1. Docker Compose (To Run)
    * Open Console
    * navigate to /payment-gateway-challenge-dotnet
    * execute `docker-compose up --build`
    * Navigate to http://localhost:5000/swagger in a browser

1. Build Solution
    * open Console
    * navigate to /payment-gateway-challenge-dotnet
    * execute `dotnet build PaymentGateway.sln`

1. Execute Tests
    * Open Console
    * navigate to /payment-gateway-challenge-dotnet
    * execute `dotnet test PaymentGateway.sln` 


Visit the following url in any browser
http://localhost:5000/swagger

## Known Issues
1. The mock bank implementation does not respond when the input data does not match its expected input. Due to this, the payment gateway attempts retries and responds with a 500 Internal Server Error. 

1. In the Process Payment input JSON, if "04" is passed as a value for the month instead of "4", the JSON converter fails. A solution involving a custom JSON converter was considered but deemed excessive for now.

## Design Decisions

### Project Structure
Some implementations have associated classes in the same file. This is done to avoid over-complicating the folder structure and to keep the project readable for reviewers. In a production-quality implementation, there would be many more classes, necessitating a more comprehensive folder structure.

#### In-Memory DB Implementation 
To simplify the process, I've implemented an in-memory payment repository. This setup simulates a database but doesn't incorporate best practices or high-quality considerations. In a real project, an ORM would typically be used to interact with the database, likely requiring a more complex setup with additional classes beyond a basic repository.

Additionally, I am not saving requests that are rejected due to incorrect input.

#### Handling Distributed Transactions
While I have given this ample consideration, it appears that this part is complex to implement and is not mentioned in the requirements. Therefore, to maintain simplicity, I am deeming this implementation as out of scope for this exercise.

Since this is a payment gateway that interacts with an external Bank API, there is a possibility that either system could go down during a transaction. This could result in an incorrect transaction state, particularly if the end user's payment has been deducted. To address this, I suggest the following:

1. **Implement Idempotency:**
   - Assign a unique idempotency key to each transaction request.
   - Store this key with the transaction details before making the bank request.
   - If a retry occurs, check if a transaction with this key already exists to prevent duplicates.

1. **Implement a Reconciliation Process:**
   - Periodically check with the bank for any discrepancies.
   - Automatically resolve issues or flag them for manual intervention.

To keep things simple, I have made a provision for the IdempotencyKey (MerchantTransactionKey), but the actual verification of duplicate transactions has not been implemented. Similarly, the Reconciliation service is not implemented. In my mind, the reconciliation service would run as a background job triggered periodically.

### BankService
The BankClient class is an abstraction for processing payments through a bank API. It encapsulates the logic for making HTTP requests to a bank's payment endpoint, handling responses, and managing errors. It also implements a retry policy using the Polly library to handle transient failures and network issues.

### IPaymentValidator
While I have only added input validation at the service layer, I have built the validators using FluentValidation. The same validator can be added to the MediatR pipeline as well, so that the validations are performed at the business layer too. I have omitted this part for simplicity.

### HealthCheck
The implemented health checks are rudimentary in this application. They are included to demonstrate the need in a production system but are not implemented in the way they should be for a real-world scenario.





