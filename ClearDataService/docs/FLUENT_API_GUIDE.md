# Fluent API for CosmosDbContainerInfo

## Overview

The `CosmosDbContainerInfo` now supports a **fluent API** for intuitive container configuration, while maintaining **full backward compatibility** with existing code.

---

## Fluent API Examples

### 1. Single Partition Key (1 Level)

```csharp
// Simple and clean
var products = CosmosDbContainerInfo.Create("Products");

// Result: ["/partitionKey"]
```

### 2. Two-Level Hierarchy (Fluent)

```csharp
// Start with default PK, then add second level
var users = CosmosDbContainerInfo
    .CreateWithDefaultPK("Users")
    .AddSecondPKPath("/userId");

// Or without the forward slash (auto-normalized)
var users = CosmosDbContainerInfo
    .CreateWithDefaultPK("Users")
    .AddSecondPKPath("userId");  // Automatically becomes "/userId"

// Result: ["/partitionKey", "/userId"]
```

### 3. Three-Level Hierarchy (Fluent Chain)

```csharp
// Chain the methods for maximum levels
var orders = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
    .AddSecondPKPath("/customerId")
    .AddThirdPKPath("/orderId");

// Or with auto-normalization
var orders = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
    .AddSecondPKPath("customerId")
    .AddThirdPKPath("orderId");

// Result: ["/partitionKey", "/customerId", "/orderId"]
```

---

## All Available APIs

### Fluent API (New - Recommended)

```csharp
// 1 level
var simple = CosmosDbContainerInfo.Create("Container");

// 2 levels
var twoLevel = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/path2");

// 3 levels
var threeLevel = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/path2")
    .AddThirdPKPath("/path3");
```

### Legacy API (Preserved for Backward Compatibility)

```csharp
// Single
var simple = CosmosDbContainerInfo.CreateSingle("Container");
var simple2 = new CosmosDbContainerInfo("Container");
CosmosDbContainerInfo simple3 = "Container";  // Implicit conversion

// Hierarchical
var twoLevel = CosmosDbContainerInfo.CreateHierarchical("Container", "/path2");
var threeLevel = CosmosDbContainerInfo.CreateHierarchical("Container", "/path2", "/path3");

// Multi-tenant
var multiTenant2 = CosmosDbContainerInfo.CreateMultiTenant("Container", "/path2");
var multiTenant3 = CosmosDbContainerInfo.CreateMultiTenant("Container", "/path2", "/path3");
```

---

## Real-World Examples

### E-Commerce Application

```csharp
// Products - simple partitioning
var products = CosmosDbContainerInfo.Create("Products");

// Users - tenant + user isolation
var users = CosmosDbContainerInfo
    .CreateWithDefaultPK("Users")
    .AddSecondPKPath("userId");

// Orders - complex hierarchy
var orders = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
    .AddSecondPKPath("customerId")
    .AddThirdPKPath("orderId");

// Cart - tenant + user
var cart = CosmosDbContainerInfo
    .CreateWithDefaultPK("ShoppingCart")
    .AddSecondPKPath("userId");

// Reviews - product reviews
var reviews = CosmosDbContainerInfo
    .CreateWithDefaultPK("Reviews")
    .AddSecondPKPath("productId");
```

### Multi-Tenant SaaS Application

```csharp
// Settings - tenant-level only
var settings = CosmosDbContainerInfo.Create("TenantSettings");

// Users - tenant + user
var users = CosmosDbContainerInfo
    .CreateWithDefaultPK("Users")
    .AddSecondPKPath("userId");

// Documents - tenant + folder + document
var documents = CosmosDbContainerInfo
    .CreateWithDefaultPK("Documents")
    .AddSecondPKPath("folderId")
    .AddThirdPKPath("documentId");

// Audit logs - tenant + service + timestamp
var auditLogs = CosmosDbContainerInfo
    .CreateWithDefaultPK("AuditLogs")
  .AddSecondPKPath("serviceId")
    .AddThirdPKPath("timestamp");
```

### IoT Application

```csharp
// Devices - simple
var devices = CosmosDbContainerInfo.Create("Devices");

// Telemetry - device + sensor
var telemetry = CosmosDbContainerInfo
    .CreateWithDefaultPK("Telemetry")
    .AddSecondPKPath("deviceId");

// Events - device + sensor + event type
var events = CosmosDbContainerInfo
    .CreateWithDefaultPK("Events")
    .AddSecondPKPath("deviceId")
    .AddThirdPKPath("eventType");
```

---

## Container Setup in Application

```csharp
// Program.cs - fluent style
app.CreateCosmosDatabaseAndContainers(
    // Simple
    CosmosDbContainerInfo.Create("Products"),
    CosmosDbContainerInfo.Create("Categories"),
    
    // 2-level hierarchy
    CosmosDbContainerInfo
        .CreateWithDefaultPK("Users")
        .AddSecondPKPath("userId"),
    
    CosmosDbContainerInfo
   .CreateWithDefaultPK("Cart")
        .AddSecondPKPath("userId"),
    
    // 3-level hierarchy
 CosmosDbContainerInfo
        .CreateWithDefaultPK("Orders")
        .AddSecondPKPath("customerId")
   .AddThirdPKPath("orderId"),
    
    CosmosDbContainerInfo
    .CreateWithDefaultPK("Documents")
        .AddSecondPKPath("folderId")
 .AddThirdPKPath("documentId")
);
```

---

## Static Configuration Class Pattern

```csharp
// Define all containers in one place
public static class AppContainers
{
    // Simple containers
    public static readonly CosmosDbContainerInfo Products = 
        CosmosDbContainerInfo.Create("Products");
    
    public static readonly CosmosDbContainerInfo Categories = 
        CosmosDbContainerInfo.Create("Categories");
    
    // 2-level containers
    public static readonly CosmosDbContainerInfo Users = 
     CosmosDbContainerInfo
 .CreateWithDefaultPK("Users")
.AddSecondPKPath("userId");
    
    public static readonly CosmosDbContainerInfo Cart = 
        CosmosDbContainerInfo
        .CreateWithDefaultPK("ShoppingCart")
      .AddSecondPKPath("userId");
    
    // 3-level containers
    public static readonly CosmosDbContainerInfo Orders = 
        CosmosDbContainerInfo
   .CreateWithDefaultPK("Orders")
            .AddSecondPKPath("customerId")
  .AddThirdPKPath("orderId");
    
    public static readonly CosmosDbContainerInfo Documents = 
        CosmosDbContainerInfo
         .CreateWithDefaultPK("Documents")
   .AddSecondPKPath("folderId")
      .AddThirdPKPath("documentId");
}

// Usage in Program.cs
app.CreateCosmosDatabaseAndContainers(
    AppContainers.Products,
    AppContainers.Categories,
    AppContainers.Users,
    AppContainers.Cart,
    AppContainers.Orders,
    AppContainers.Documents
);
```

---

## Error Handling

### Validation Errors

```csharp
// ? Error: Cannot add second path twice
var invalid = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/path2")
    .AddSecondPKPath("/path2");  // InvalidOperationException

// ? Error: Must add second before third
var invalid2 = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddThirdPKPath("/path3");  // InvalidOperationException

// ? Error: Cannot use /partitionKey as additional path
var invalid3 = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/partitionKey");  // ArgumentException
```

### Correct Order

```csharp
// ? Correct: Add second, then third
var correct = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/path2")   // ? Valid
    .AddThirdPKPath("/path3");   // ? Valid after second

// ? Correct: Can stop at any level
var level1 = CosmosDbContainerInfo.Create("Container");  // ? 1 level

var level2 = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
.AddSecondPKPath("/path2");  // ? 2 levels

var level3 = CosmosDbContainerInfo
    .CreateWithDefaultPK("Container")
    .AddSecondPKPath("/path2")
    .AddThirdPKPath("/path3");   // ? 3 levels
```

---

## Path Normalization

The fluent API automatically normalizes paths:

```csharp
// All of these are equivalent:
var c1 = CosmosDbContainerInfo.CreateWithDefaultPK("Users").AddSecondPKPath("/userId");
var c2 = CosmosDbContainerInfo.CreateWithDefaultPK("Users").AddSecondPKPath("userId");

// Both result in: ["/partitionKey", "/userId"]

// Works for all levels
var orders1 = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
  .AddSecondPKPath("/customerId")
    .AddThirdPKPath("/orderId");

var orders2 = CosmosDbContainerInfo
.CreateWithDefaultPK("Orders")
    .AddSecondPKPath("customerId")    // Auto-normalized to "/customerId"
    .AddThirdPKPath("orderId");       // Auto-normalized to "/orderId"

// Both result in: ["/partitionKey", "/customerId", "/orderId"]
```

---

## Comparison: Fluent vs Legacy

### Fluent API (Recommended)

**Advantages:**
- ? More intuitive and readable
- ? Progressive disclosure (start simple, add complexity)
- ? Self-documenting intent
- ? Clear progression: Create ? Add2nd ? Add3rd

```csharp
var container = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
    .AddSecondPKPath("customerId")
    .AddThirdPKPath("orderId");
```

### Legacy API (Still Supported)

**When to use:**
- Existing code that already uses it
- Prefer one-liner syntax
- Building from configuration/dynamic sources

```csharp
var container = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
```

---

## Migration Guide

### From Legacy to Fluent API

**Before:**
```csharp
var simple = CosmosDbContainerInfo.CreateSingle("Products");
var twoLevel = CosmosDbContainerInfo.CreateHierarchical("Users", "/userId");
var threeLevel = CosmosDbContainerInfo.CreateHierarchical("Orders", "/customerId", "/orderId");
```

**After (Fluent):**
```csharp
var simple = CosmosDbContainerInfo.Create("Products");

var twoLevel = CosmosDbContainerInfo
    .CreateWithDefaultPK("Users")
    .AddSecondPKPath("userId");

var threeLevel = CosmosDbContainerInfo
    .CreateWithDefaultPK("Orders")
    .AddSecondPKPath("customerId")
    .AddThirdPKPath("orderId");
```

**Note:** Both styles work! No breaking changes. Choose the style you prefer.

---

## Summary

### Fluent API Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| `Create(name)` | Simple 1-level container | `CosmosDbContainerInfo` |
| `CreateWithDefaultPK(name)` | Start fluent chain | `CosmosDbContainerInfo` |
| `AddSecondPKPath(path)` | Add 2nd level | `CosmosDbContainerInfo` |
| `AddThirdPKPath(path)` | Add 3rd level (max) | `CosmosDbContainerInfo` |

### Key Benefits

1. ? **Intuitive**: Reads like English - "Create with default PK, add second path"
2. ? **Progressive**: Start simple, build complexity as needed
3. ? **Type-Safe**: Compiler enforces correct order
4. ? **Flexible**: Auto-normalizes paths (adds `/` if missing)
5. ? **Compatible**: Legacy API still works - no breaking changes

### Quick Reference

```csharp
// 1 level
CosmosDbContainerInfo.Create("Name")

// 2 levels
CosmosDbContainerInfo.CreateWithDefaultPK("Name").AddSecondPKPath("/path2")

// 3 levels (maximum)
CosmosDbContainerInfo.CreateWithDefaultPK("Name").AddSecondPKPath("/path2").AddThirdPKPath("/path3")
```

---

**Version**: 3.0.2+  
**Status**: Production Ready  
**Breaking Changes**: None - Fully backward compatible
