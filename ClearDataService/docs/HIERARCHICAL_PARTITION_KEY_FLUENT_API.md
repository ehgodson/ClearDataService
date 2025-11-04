# HierarchicalPartitionKey - Fluent API Guide

## Overview

The `HierarchicalPartitionKey` now supports a **fluent API** with **compile-time enforcement** of Azure Cosmos DB's 3-level maximum, while maintaining **full backward compatibility**.

---

## Fluent API Examples

### 1. Single Level (1 Value)

```csharp
// Simple - just the tenant ID
var key = HierarchicalPartitionKey.Create("tenant-123");

// Or using fluent start
var key = HierarchicalPartitionKey.WithLevel1("tenant-123");

// Result: ["tenant-123"]
```

### 2. Two Levels (2 Values)

```csharp
// Direct creation
var key = HierarchicalPartitionKey.Create("tenant-123", "user-456");

// Fluent style
var key = HierarchicalPartitionKey
    .WithLevel1("tenant-123")
    .AddLevel2("user-456");

// Result: ["tenant-123", "user-456"]
```

### 3. Three Levels (3 Values - Maximum)

```csharp
// Direct creation
var key = HierarchicalPartitionKey.Create("tenant-123", "customer-456", "order-789");

// Fluent style (most readable)
var key = HierarchicalPartitionKey
    .WithLevel1("tenant-123")
    .AddLevel2("customer-456")
    .AddLevel3("order-789");

// Result: ["tenant-123", "customer-456", "order-789"]
```

---

## Quick Reference

```csharp
// 1 level
HierarchicalPartitionKey.Create("level1")
HierarchicalPartitionKey.WithLevel1("level1")

// 2 levels
HierarchicalPartitionKey.Create("level1", "level2")
HierarchicalPartitionKey.WithLevel1("level1").AddLevel2("level2")

// 3 levels (maximum)
HierarchicalPartitionKey.Create("level1", "level2", "level3")
HierarchicalPartitionKey.WithLevel1("level1").AddLevel2("level2").AddLevel3("level3")
```

---

## Key Benefits

1. ? **Type-Safe**: Compile-time enforcement of 3-level maximum
2. ? **Fluent**: Progressive construction with method chaining
3. ? **Validated**: Runtime checks prevent invalid configurations
4. ? **Flexible**: Multiple APIs for different use cases
5. ? **Compatible**: Legacy APIs still work - no breaking changes

---

**Version**: 3.0.2+  
**Status**: Production Ready  
**Azure Cosmos DB Limit**: Maximum 3 hierarchical partition key levels (enforced)
