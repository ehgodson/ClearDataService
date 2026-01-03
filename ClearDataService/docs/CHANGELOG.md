# Changelog

## [4.1.0] - 2026-01-03
### Fixed
- **Cosmos DB Batch Operations**: Complete refactoring of batch processing to fix critical issues
  - Fixed partition key handling: Now properly stores and uses `CosmosDbPartitionKey` objects instead of strings
  - Introduced `CosmosDbDocKeyCombo` record to couple partition keys with their documents
  - Fixed batch size limitations: Automatically chunks operations into groups of 100 (Cosmos DB limit)
  - Enhanced error reporting: Batch operations now report which chunk failed with item ranges
  - Fixed `AddToBatch` to use `GetKey()` method for consistent partition key string representation
  - Improved batch result messages to include chunk progress (e.g., "items 1-100 of 250, batch 1/3")

### Changed
- **ContainerBatchBuffer Model**: Complete refactoring for better type safety and partition key handling
  - Changed `PartitionedItems` from `Dictionary<string, List<ICosmosDbDocument>>` to `Dictionary<string, CosmosDbDocKeyCombo>`
  - Updated `GetDocuments()` to return `CosmosDbDocKeyCombo` instead of `IEnumerable<ICosmosDbDocument>`
  - Updated `AddDocument()` to accept `CosmosDbPartitionKey` objects instead of strings
  - Added proper partition key validation when adding documents
- **CosmosDbPartitionKey**: Added `GetKey()` method for consistent string representation across partition key levels
- **SaveBatchAsync Logic**: Major improvements to batch execution
  - Now processes documents in chunks of 100 items per transactional batch
  - Better error handling with detailed messages for each chunk
  - Improved success/failure tracking with item ranges
- **CosmosBatchResult**: Updated factory methods to accept nullable message parameters
- **IContainerBatchBuffer**: Moved interface definition to ContainerBatchBuffer.cs for better organization
- **Version**: Bumped from 4.0.0 to 4.1.0

### Technical Details
- **Root Cause**: Previous implementation stored partition keys as strings, losing hierarchical structure
- **Solution**: Store full `CosmosDbPartitionKey` objects and use `ToCosmosPartitionKey()` during batch execution
- **Performance**: Added chunking logic ensures batches never exceed Cosmos DB's 100-operation limit
- **Reliability**: Enhanced error messages now identify exactly which items failed in large batch operations

## [3.0.1] - 2025-09-29
### Added
- **Batch Operations for Cosmos DB**: Added comprehensive batch processing support for Cosmos DB operations
  - `AddToBatch<T>()`: Method to queue entities for batch processing with partition key validation
  - `SaveBatchAsync()`: Method to execute all queued batch operations in optimized transactional batches
  - `ContainerBatchBuffer`: Internal buffer management system for organizing batch operations by container and partition
  - `CosmosBatchResult`: Result tracking for batch operations with success/failure status and detailed error reporting
- **Enhanced ICosmosDbContext Interface**: Extended interface with new batch operation methods
- **Improved Error Handling**: Added comprehensive exception handling for batch operations including CosmosException and general Exception scenarios
- **Performance Optimizations**: Batch operations are grouped by container and partition key for optimal Cosmos DB transactional batch execution

### Changed
- **ICosmosDbContext Interface**: Added `AddToBatch<T>()` and `SaveBatchAsync()` method signatures
- **Clear.DataService.csproj**: Updated version number to 3.0.1
- **Using Statements**: Added `Clear.DataService.Models` namespace to ICosmosDbContext for batch result types

### Fixed
- **Solution File**: Added proper Visual Studio solution file structure for improved development experience

## [3.0.0] - 2025-04-12
### Added
- `global.cs`: Introduced global using directives for common namespaces.
- `CosmosDbSettings.cs`: Added a record for Cosmos DB configuration.
- Cosmos DB Abstractions:
  - `ICosmosDbContext`
  - `ICosmosDbEntity`
  - `ICosmosDbRepo`
- SQL DB Abstractions:
  - `ISqlDbContext`
  - `ISqlDbEntity`
  - `ISqlDbRepo`
- Contexts:
  - `CosmosDbContext`
  - `SqlDbContext`
- Dependency Injection Extensions:
  - `ServiceCollectionExtensions`
  - `SqlDbContextMiddlewareExtension`
- Entities:
  - Cosmos DB Entities:
    - `BaseCosmosDbEntity`
    - `BaseCosmosDbEntityWithString`
    - `CosmosDbDocument`
  - SQL DB Entities:
    - `BaseSqlDbEntity`
    - `BaseSqlDbEntityWithInt`
- Repositories:
  - `BaseCosmosDbRepo`
  - `BaseSqlDbRepo`
- Factory:
  - `CosmosDbClientFactory`

### Removed
- `IEntity.cs`
- `EntityWithInt.cs`

## [2.0.1] - 2025-01-31
### Changed
- IDataService 
	- Added GetOne
	- Added tracking option to Get
	- Added tracking option to GetAsQueryable
	- Added tracking option to FindAsQueryable
- Updated packages

## [2.0.0] - 2025-01-17
### Added
- IEntity
- IEntityRepo
- BaseEntityRepo
- BaseDataService
- DataService
- DataServiceMiddlewareExtension
### Removed
- IGenericService
- AbstractGenericService
- AbstractDataService
### Changed
- IDataService
- Updated packages

## [1.0.3] - 2024-09-07
### Changed
- IDataService
- AbstractDataService
- Updated packages


## [1.0.2] - 2024-04-06
### Added
- Initialize project files.
- IDataService
- IGenericService
- AbstractDataService
- AbstractGenericService

### Changed
- N/A

### Fixed
- N/A
