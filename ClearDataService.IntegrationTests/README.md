# ClearDataService Integration Tests

A comprehensive console application for integration testing all features of the ClearDataService package.

## Overview

This console application provides end-to-end integration tests for both **CosmosDB** and **SQL Server** implementations of the ClearDataService package. It tests all methods and features of the `CosmosDbContext` and `SqlDbContext` classes.

## Features Tested

### CosmosDB Tests (16 Tests)
1. **Save Single Entity** - Tests creating a new entity
2. **Upsert Entity** - Tests creating and updating entities
3. **Get Entity by ID** - Tests retrieving a single entity by ID
4. **Get Entity with Filter** - Tests retrieving entities using FilterBuilder
5. **Get List of Entities** - Tests retrieving all entities in a container
6. **Get List with Filter** - Tests retrieving filtered lists
7. **Get Document by ID** - Tests retrieving CosmosDbDocument wrapper
8. **Get Documents with Filter** - Tests retrieving filtered documents
9. **Paged Results - Basic** - Tests basic pagination
10. **Paged Results with Filter** - Tests pagination with filtering
11. **Paged Results with Sorting** - Tests pagination with SortBuilder
12. **Paged Results with SQL** - Tests SQL-based pagination
13. **Get as Queryable** - Tests LINQ queryable interface
14. **Batch Operations** - Tests batch insert operations
15. **Hierarchical Partition Key** - Tests hierarchical partition key support
16. **Delete Entity** - Tests entity deletion

### SQL Server Tests (22 Tests)
1. **Save Single Entity (Int ID)** - Tests saving entities with integer IDs
2. **Save Single Entity (String ID)** - Tests saving entities with string IDs
3. **Save Multiple Entities** - Tests bulk save operations
4. **Get by ID (Int)** - Tests retrieving by integer ID
5. **Get by ID (String)** - Tests retrieving by string ID
6. **Get All Entities** - Tests retrieving all entities
7. **Get with Predicate** - Tests filtered retrieval
8. **Get One** - Tests getting first entity
9. **Find with Predicate** - Tests finding multiple entities
10. **Count All** - Tests counting all entities
11. **Count with Predicate** - Tests filtered count
12. **Exists Check** - Tests existence checking
13. **Update Entity** - Tests single entity update
14. **Update Multiple Entities** - Tests bulk updates
15. **Get as Queryable** - Tests LINQ queryable interface
16. **Find as Queryable** - Tests filtered queryable
17. **Batch Insert/Update/Delete** - Tests batch operations
18. **Execute SQL** - Tests raw SQL execution
19. **Query with Dapper** - Tests Dapper queries
20. **Delete Entity** - Tests single deletion
21. **Delete with Predicate** - Tests bulk deletion by predicate
22. **Delete Multiple** - Tests bulk deletion

## Usage

### Running the Tests

1. Build and run the console application:
   ```bash
   dotnet run --project ClearDataService.IntegrationTests
   ```

2. Select test type:
   - Enter `1` for CosmosDB tests
   - Enter `2` for SQL Server tests

### CosmosDB Tests

When running CosmosDB tests, you'll be prompted for:

1. **Connection String**: Your Azure CosmosDB connection string
   ```
   Example: AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key==;
   ```

2. **Database Name**: The name of the CosmosDB database
   ```
   Example: TestDatabase
   ```

3. **Container Name**: The name of the container to use for tests
   ```
   Example: TestContainer
   ```

4. **Partition Key**: The partition key value for test entities
   ```
   Example: test-partition
   ```

The application will automatically:
- Create the database if it doesn't exist
- Create the container with `/partitionKey` as the partition key path
- Run all 16 integration tests
- Display pass/fail status for each test

### SQL Server Tests

When running SQL Server tests, you'll be prompted for:

1. **Connection String**: Your SQL Server connection string
   ```
   Example: Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=true;
   ```

The application will automatically:
- Create the database if it doesn't exist
- Create all required tables
- Run all 22 integration tests
- Display pass/fail status for each test
- Optionally clean up the test database after completion

## Test Entities

### CosmosDB Test Entities

- **Product**: Basic entity with properties like Name, Price, Category, etc.
- **Order**: Entity demonstrating hierarchical partition keys with nested OrderItems

### SQL Server Test Entities

- **SqlProduct**: Entity with auto-incrementing integer ID
- **SqlOrder**: Entity with string ID (GUID-based)
- **SqlOrderItem**: Child entity with foreign key relationship

## Connection String Examples

### CosmosDB (Azure)
```
AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key-here==;
```

### CosmosDB (Emulator)
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;
```

### SQL Server (Local)
```
Server=localhost;Database=ClearDataServiceTests;Integrated Security=true;TrustServerCertificate=true;
```

### SQL Server (Azure)
```
Server=tcp:your-server.database.windows.net,1433;Database=TestDb;User ID=your-user;Password=your-password;Encrypt=true;
```

## Requirements

- .NET 9.0 or later
- Azure CosmosDB account (for CosmosDB tests) or CosmosDB Emulator
- SQL Server instance (for SQL Server tests)

## Test Data Cleanup

### CosmosDB
Test data remains in your CosmosDB container. You can manually delete test documents or use the Azure Portal to clean up.

### SQL Server
After SQL Server tests complete, you'll be prompted to clean up the test database. Choosing 'y' will delete the entire test database.

## Troubleshooting

### CosmosDB Connection Issues
- Verify your connection string is correct
- Ensure the CosmosDB account is accessible
- Check if firewall rules allow your IP address
- For emulator, ensure it's running and accessible

### SQL Server Connection Issues
- Verify the connection string is correct
- Ensure SQL Server is running
- Check if the user has sufficient permissions
- Verify network connectivity

### Test Failures
- Each test displays specific error messages
- Review the error message for details
- Ensure the database/container has proper schema
- Check for any permission issues

## Contributing

When adding new features to ClearDataService:
1. Add corresponding test methods to the integration test classes
2. Follow the existing test naming and structure patterns
3. Ensure all tests are self-contained and don't depend on each other
4. Use unique identifiers (GUIDs) to avoid test data conflicts

## License

This integration test suite is part of the ClearDataService package and follows the same MIT license.
