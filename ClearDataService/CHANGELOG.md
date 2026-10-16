# Changelog

## 6.0.3

- Fix cross-partition LINQ queries: an omitted partition key now leaves the Cosmos SDK query option unset instead of targeting `PartitionKey.None` (the undefined-key partition).
- Cover request options for filtered reads, ID discovery without a key, lists, LINQ pagination, and SQL pagination. Explicit single-level and hierarchical keys remain scoped, and point-read/write behavior is unchanged.

## 6.0.0

- Remove the `ICosmosDbRepo<T>`, `BaseCosmosDbRepo<T>`, `ISqlDbRepo<T>`, and `BaseSqlDbRepo<T>` repository APIs.
- Remove repository discovery and repository-based Cosmos container creation. Register and inject `ICosmosDbContext` or `ISqlDbContext` directly instead.
- Remove the repository-only `ContainerNameMissingFromRepoException`.
- Applications must now declare Cosmos containers explicitly with `CreateCosmosDatabaseAndContainers`.

## 5.0.1

- Execute repository filters in Cosmos DB through `FilterBuilder` instead of loading partitions into memory.
- Use point reads for ID lookups when a partition key is available.
- Bound single-result filtered queries to one item.
- Apply LINQ page size and continuation tokens to the Cosmos query iterator.
