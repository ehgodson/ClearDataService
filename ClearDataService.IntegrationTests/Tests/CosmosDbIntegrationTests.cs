using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using ClearDataService.IntegrationTests.TestEntities;

namespace ClearDataService.IntegrationTests.Tests;

/// <summary>
/// Integration tests for CosmosDbContext
/// </summary>
public class CosmosDbIntegrationTests
{
    private readonly ICosmosDbContext _context;
    private readonly string _containerName;
    private readonly string _partitionKey;

    public CosmosDbIntegrationTests(ICosmosDbContext context, string containerName, string partitionKey)
    {
        _context = context;
        _containerName = containerName;
        _partitionKey = partitionKey;
    }

    public async Task<bool> RunAllTests()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("   COSMOSDB INTEGRATION TESTS");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var allPassed = true;

        allPassed &= await RunTest("1. Save Single Entity", TestSave);
        allPassed &= await RunTest("2. Upsert Entity", TestUpsert);
        allPassed &= await RunTest("3. Get Entity by ID", TestGetById);
        allPassed &= await RunTest("4. Get Entity with Filter", TestGetWithFilter);
        allPassed &= await RunTest("5. Get List of Entities", TestGetList);
        allPassed &= await RunTest("6. Get List with Filter", TestGetListWithFilter);
        allPassed &= await RunTest("7. Get Document by ID", TestGetDocument);
        allPassed &= await RunTest("8. Get Documents with Filter", TestGetDocuments);
        allPassed &= await RunTest("9. Paged Results - Basic", TestPagedResults);
        allPassed &= await RunTest("10. Paged Results with Filter", TestPagedResultsWithFilter);
        allPassed &= await RunTest("11. Paged Results with Sorting", TestPagedResultsWithSorting);
        allPassed &= await RunTest("12. Paged Results with SQL", TestPagedResultsWithSql);
        allPassed &= await RunTest("13. Get as Queryable", TestGetAsQueryable);
        allPassed &= await RunTest("14. Batch Operations", TestBatchOperations);
        
        // Note: Hierarchical partition key tests require a container configured with multiple partition key paths
        // These tests are included but may fail if the container uses a single partition key path
        Console.WriteLine("\n  Note: Tests 15 and 18 require a container with hierarchical partition key support");
        allPassed &= await RunTest("15. Hierarchical Partition Key", TestHierarchicalPartitionKey);
        
        allPassed &= await RunTest("16. Delete Entity", TestDelete);
        allPassed &= await RunTest("17. Delete All Entities in Partition", TestDeleteAll);
        allPassed &= await RunTest("18. Delete All with Hierarchical Partition Key", TestDeleteAllHierarchical);

        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine($"   CosmosDB Tests {(allPassed ? "PASSED" : "FAILED")}");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        return allPassed;
    }

    private async Task<bool> RunTest(string testName, Func<Task> testFunc)
    {
        try
        {
            Console.Write($"  {testName}... ");
            await testFunc();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ PASSED");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ FAILED");
            Console.WriteLine($"    Error: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private async Task TestSave()
    {
        var product = new Product
        {
            Name = "Test Product",
            Price = 99.99m,
            Category = "Electronics",
            StockQuantity = 10
        };

        var result = await _context.Save(_containerName, product, _partitionKey);

        if (result == null || result.Data.Id != product.Id)
            throw new Exception("Save failed to return correct document");
    }

    private async Task TestUpsert()
    {
        var product = new Product
        {
            Id = "upsert-test-" + Guid.NewGuid(),
            Name = "Upsert Product",
            Price = 49.99m,
            Category = "Books",
            StockQuantity = 5
        };

        // First upsert (create)
        var result1 = await _context.Upsert(_containerName, product, _partitionKey);

        // Second upsert (update)
        product.Price = 59.99m;
        var result2 = await _context.Upsert(_containerName, product, _partitionKey);

        if (result2.Data.Price != 59.99m)
            throw new Exception("Upsert failed to update the entity");
    }

    private async Task TestGetById()
    {
        var product = new Product
        {
            Id = "get-test-" + Guid.NewGuid(),
            Name = "Get Test Product",
            Price = 29.99m,
            Category = "Toys",
            StockQuantity = 15
        };

        await _context.Save(_containerName, product, _partitionKey);

        var retrieved = await _context.Get<Product>(_containerName, product.Id, _partitionKey);

        if (retrieved == null || retrieved.Name != product.Name)
            throw new Exception("Get by ID failed");
    }

    private async Task TestGetWithFilter()
    {
        var product = new Product
        {
            Id = "filter-test-" + Guid.NewGuid(),
            Name = "Unique Filter Test Product",
            Price = 79.99m,
            Category = "Gadgets",
            StockQuantity = 20
        };

        await _context.Save(_containerName, product, _partitionKey);

        var filter = FilterBuilder<Product>.Create()
            .And(doc => doc.Data.Name == "Unique Filter Test Product");

        var retrieved = await _context.Get(_containerName, filter, _partitionKey);

        if (retrieved == null || retrieved.Id != product.Id)
            throw new Exception("Get with filter failed");
    }

    private async Task TestGetList()
    {
        // Create multiple products
        for (int i = 0; i < 3; i++)
        {
            var product = new Product
            {
                Name = $"List Test Product {i}",
                Price = 10m * (i + 1),
                Category = "TestCategory",
                StockQuantity = 5 * (i + 1)
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var list = await _context.GetList<Product>(_containerName, _partitionKey);

        if (list == null || list.Count == 0)
            throw new Exception("GetList returned no results");
    }

    private async Task TestGetListWithFilter()
    {
        var category = "FilterListCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 3; i++)
        {
            var product = new Product
            {
                Name = $"Filtered Product {i}",
                Price = 20m * (i + 1),
                Category = category,
                StockQuantity = 10
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var filter = FilterBuilder<Product>.Create()
      .And(doc => doc.Data.Category == category);

        var list = await _context.GetList<Product>(_containerName, _partitionKey, filter);

        if (list.Count != 3)
            throw new Exception($"GetList with filter expected 3 items but got {list.Count}");
    }
    private async Task TestGetDocument()
    {
        var product = new Product
        {
            Id = "doc-test-" + Guid.NewGuid(),
            Name = "Document Test Product",
            Price = 39.99m,
            Category = "Documents",
            StockQuantity = 8
        };

        await _context.Save(_containerName, product, _partitionKey);

        var doc = await _context.GetDocument<Product>(_containerName, product.Id, _partitionKey);

        if (doc == null || doc.EntityType != nameof(Product))
            throw new Exception("GetDocument failed");
    }

    private async Task TestGetDocuments()
    {
        var category = "DocListCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 2; i++)
        {
            var product = new Product
            {
                Name = $"Doc Product {i}",
                Price = 15m * (i + 1),
                Category = category,
                StockQuantity = 5
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var filter = FilterBuilder<Product>.Create()
            .And(doc => doc.Data.Category == category);

        var docs = await _context.GetDocuments<Product>(_containerName, _partitionKey, filter);

        if (docs.Count != 2)
            throw new Exception($"GetDocuments expected 2 but got {docs.Count}");
    }

    private async Task TestPagedResults()
    {
        // Create multiple products
        for (int i = 0; i < 5; i++)
        {
            var product = new Product
            {
                Name = $"Paged Product {i}",
                Price = 25m * (i + 1),
                Category = "PagedTest",
                StockQuantity = 3 * i
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var pagedResult = await _context.GetPagedList<Product>(
            _containerName,
   pageSize: 2,
      partitionKey: _partitionKey);

        if (pagedResult.Count == 0)
            throw new Exception("Paged results returned no items");

        if (pagedResult.Items.Count > 2)
            throw new Exception($"Paged results should have max 2 items but got {pagedResult.Items.Count}");
    }

    private async Task TestPagedResultsWithFilter()
    {
        var category = "PagedFilterCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 4; i++)
        {
            var product = new Product
            {
                Name = $"Paged Filtered {i}",
                Price = 30m * (i + 1),
                Category = category,
                StockQuantity = 2 * i,
                IsActive = i % 2 == 0
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var filter = FilterBuilder<Product>.Create()
 .And(doc => doc.Data.Category == category)
      .And(doc => doc.Data.IsActive == true);

        var pagedResult = await _context.GetPagedList<Product>(
               _containerName,
     pageSize: 3,
         partitionKey: _partitionKey,
      filter: filter);

        if (pagedResult.Count == 0)
            throw new Exception("Paged filtered results returned no items");
    }

    private async Task TestPagedResultsWithSorting()
    {
        var category = "SortedPagedCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 3; i++)
        {
            var product = new Product
            {
                Name = $"Sorted {i}",
                Price = 10m * (3 - i), // Descending prices
                Category = category,
                StockQuantity = i + 1
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var sortBuilder = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Price, SortDirection.Ascending);

        var pagedResult = await _context.GetPagedList<Product>(
          _containerName,
            pageSize: 10,
               partitionKey: _partitionKey,
   sortBuilder: sortBuilder);

        if (pagedResult.Items.Count >= 2 && pagedResult.Items[0].Price > pagedResult.Items[1].Price)
            throw new Exception("Sorting failed - items not in ascending price order");
    }

    private async Task TestPagedResultsWithSql()
    {
        var category = "SqlPagedCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 3; i++)
        {
            var product = new Product
            {
                Name = $"SQL Paged {i}",
                Price = 40m + i,
                Category = category,
                StockQuantity = 10
            };
            await _context.Save(_containerName, product, _partitionKey);
        }

        var parameters = new Dictionary<string, object>
   {
        { "category", category }
    };

        var pagedResult = await _context.GetPagedListWithSql<Product>(
     _containerName,
            whereClause: "c.data.category = @category",
    parameters: parameters,
   pageSize: 10,
       partitionKey: _partitionKey);

        if (pagedResult.Count == 0)
            throw new Exception("SQL paged results returned no items");
    }

    private async Task TestGetAsQueryable()
    {
        var product = new Product
        {
            Id = "queryable-test-" + Guid.NewGuid(),
            Name = "Queryable Test",
            Price = 99.99m,
            Category = "QueryableCategory",
            StockQuantity = 50
        };

        await _context.Save(_containerName, product, _partitionKey);

        var queryable = _context.GetAsQueryable<Product>(_containerName, _partitionKey);

        if (queryable == null)
            throw new Exception("GetAsQueryable returned null");
    }

    private async Task TestBatchOperations()
    {
        var batchCategory = "BatchCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 3; i++)
        {
            var product = new Product
            {
                Name = $"Batch Product {i}",
                Price = 50m + i,
                Category = batchCategory,
                StockQuantity = 5
            };
            _context.AddToBatch(_containerName, _partitionKey, product);
        }

        var results = await _context.SaveBatchAsync();

        if (results.Count == 0)
            throw new Exception("Batch operations returned no results");

        var successCount = results.Count(r => r.Successful);
        if (successCount == 0)
            throw new Exception("No successful batch operations");
    }

    private async Task TestHierarchicalPartitionKey()
    {
        var hpk = CosmosDbPartitionKey.Create("Region1", "Customer1");

        var order = new Order
        {
            CustomerId = "Customer1",
            Region = "Region1",
            TotalAmount = 150.00m,
            Status = "Confirmed",
            Items = new List<OrderItem>
    {
  new OrderItem { ProductId = "P1", ProductName = "Product 1", Quantity = 2, UnitPrice = 50m },
        new OrderItem { ProductId = "P2", ProductName = "Product 2", Quantity = 1, UnitPrice = 50m }
            }
        };

        var savedOrder = await _context.Save(_containerName, order, hpk);

        if (savedOrder == null || savedOrder.Data.Id != order.Id)
            throw new Exception("Hierarchical partition key save failed");

        var retrievedOrder = await _context.Get<Order>(_containerName, order.Id, hpk);

        if (retrievedOrder == null || retrievedOrder.CustomerId != "Customer1")
            throw new Exception("Hierarchical partition key get failed");
    }

    private async Task TestDelete()
    {
        var product = new Product
        {
            Id = "delete-test-" + Guid.NewGuid(),
            Name = "Delete Test Product",
            Price = 19.99m,
            Category = "ToBeDeleted",
            StockQuantity = 1
        };

        await _context.Save(_containerName, product, _partitionKey);
        await _context.Delete<Product>(_containerName, product.Id, _partitionKey);

        // Try to get the deleted item - should throw or return null
        try
        {
            var retrieved = await _context.Get<Product>(_containerName, product.Id, _partitionKey);
            if (retrieved != null)
                throw new Exception("Delete failed - item still exists");
        }
        catch (Exception ex) when (!ex.Message.Contains("Delete failed"))
        {
            // Expected - item not found
        }
    }

    private async Task TestDeleteAll()
    {
        // Create a unique partition key for this test
        var testPartitionKey = "delete-all-test-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Create multiple products in the same partition
        var productIds = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var product = new Product
            {
                Id = $"delete-all-product-{i}-{Guid.NewGuid()}",
                Name = $"Delete All Test Product {i}",
                Price = 10m * (i + 1),
                Category = "DeleteAllTest",
                StockQuantity = i + 1
            };
            productIds.Add(product.Id);
            await _context.Save(_containerName, product, testPartitionKey);
        }

        // Verify products exist
        var listBefore = await _context.GetList<Product>(_containerName, testPartitionKey);
        if (listBefore.Count != 5)
            throw new Exception($"Expected 5 products before delete but found {listBefore.Count}");

        // Delete all products in the partition
        await _context.DeleteAll(_containerName, testPartitionKey);

        // Verify all products are deleted
        var listAfter = await _context.GetList<Product>(_containerName, testPartitionKey);
        if (listAfter.Count != 0)
            throw new Exception($"Expected 0 products after DeleteAll but found {listAfter.Count}");
    }

    private async Task TestDeleteAllHierarchical()
    {
        // Create hierarchical partition key
        var region = "Region-DeleteTest-" + Guid.NewGuid().ToString().Substring(0, 8);
        var customer = "Customer-DeleteTest-" + Guid.NewGuid().ToString().Substring(0, 8);
        var hpk = CosmosDbPartitionKey.Create(region, customer);

        // Create multiple orders in the same hierarchical partition
        var orderIds = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var order = new Order
            {
                Id = $"delete-all-order-{i}-{Guid.NewGuid()}",
                CustomerId = customer,
                Region = region,
                TotalAmount = 100m * (i + 1),
                Status = "Pending",
                Items = new List<OrderItem>
                {
                    new OrderItem 
                    { 
                        ProductId = $"P{i}", 
                        ProductName = $"Product {i}", 
                        Quantity = i + 1, 
                        UnitPrice = 100m 
                    }
                }
            };
            orderIds.Add(order.Id);
            await _context.Save(_containerName, order, hpk);
        }

        // Verify orders exist
        var listBefore = await _context.GetList<Order>(_containerName, hpk);
        if (listBefore.Count != 3)
            throw new Exception($"Expected 3 orders before delete but found {listBefore.Count}");

        // Delete all orders in the hierarchical partition
        await _context.DeleteAll(_containerName, hpk);

        // Verify all orders are deleted
        var listAfter = await _context.GetList<Order>(_containerName, hpk);
        if (listAfter.Count != 0)
            throw new Exception($"Expected 0 orders after DeleteAll but found {listAfter.Count}");
    }
}
