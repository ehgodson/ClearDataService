# Changelog

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
