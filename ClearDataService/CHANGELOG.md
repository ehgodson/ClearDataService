# Changelog

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
- **ClearDataService.csproj**: Updated version number to 3.0.1
- **Using Statements**: Added `ClearDataService.Models` namespace to ICosmosDbContext for batch result types

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
