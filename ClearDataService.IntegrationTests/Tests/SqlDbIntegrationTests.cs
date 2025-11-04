using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using ClearDataService.IntegrationTests.Data;
using ClearDataService.IntegrationTests.TestEntities;

namespace ClearDataService.IntegrationTests.Tests;

/// <summary>
/// Integration tests for SqlDbContext
/// </summary>
public class SqlDbIntegrationTests
{
    private readonly ISqlDbContext _context;

    public SqlDbIntegrationTests(ISqlDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RunAllTests()
    {
        Console.WriteLine("\n???????????????????????????????????????????????????????????");
        Console.WriteLine("   SQL SERVER INTEGRATION TESTS");
        Console.WriteLine("???????????????????????????????????????????????????????????\n");

  var allPassed = true;

   allPassed &= await RunTest("1. Save Single Entity (Int ID)", TestSaveIntId);
        allPassed &= await RunTest("2. Save Single Entity (String ID)", TestSaveStringId);
   allPassed &= await RunTest("3. Save Multiple Entities", TestSaveMultiple);
        allPassed &= await RunTest("4. Get by ID (Int)", TestGetByIntId);
        allPassed &= await RunTest("5. Get by ID (String)", TestGetByStringId);
        allPassed &= await RunTest("6. Get All Entities", TestGetAll);
        allPassed &= await RunTest("7. Get with Predicate", TestGetWithPredicate);
        allPassed &= await RunTest("8. Get One", TestGetOne);
        allPassed &= await RunTest("9. Find with Predicate", TestFind);
     allPassed &= await RunTest("10. Count All", TestCount);
        allPassed &= await RunTest("11. Count with Predicate", TestCountWithPredicate);
        allPassed &= await RunTest("12. Exists Check", TestExists);
   allPassed &= await RunTest("13. Update Entity", TestUpdate);
  allPassed &= await RunTest("14. Update Multiple Entities", TestUpdateMultiple);
        allPassed &= await RunTest("15. Get as Queryable", TestGetAsQueryable);
   allPassed &= await RunTest("16. Find as Queryable", TestFindAsQueryable);
    allPassed &= await RunTest("17. Batch Insert/Update/Delete", TestBatchOperations);
        allPassed &= await RunTest("18. Execute SQL", TestExecuteSql);
     allPassed &= await RunTest("19. Query with Dapper", TestDapperQuery);
        allPassed &= await RunTest("20. Delete Entity", TestDelete);
        allPassed &= await RunTest("21. Delete with Predicate", TestDeleteWithPredicate);
        allPassed &= await RunTest("22. Delete Multiple", TestDeleteMultiple);

        Console.WriteLine("\n???????????????????????????????????????????????????????????");
        Console.WriteLine($"   SQL Server Tests {(allPassed ? "PASSED" : "FAILED")}");
        Console.WriteLine("???????????????????????????????????????????????????????????\n");

        return allPassed;
    }

    private async Task<bool> RunTest(string testName, Func<Task> testFunc)
    {
        try
  {
 Console.Write($"  {testName}... ");
          await testFunc();
      Console.ForegroundColor = ConsoleColor.Green;
   Console.WriteLine("? PASSED");
          Console.ResetColor();
  return true;
        }
      catch (Exception ex)
        {
   Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($"? FAILED");
   Console.WriteLine($"    Error: {ex.Message}");
            Console.ResetColor();
   return false;
      }
    }

    private async Task TestSaveIntId()
    {
      var product = new SqlProduct
     {
   Id = 0, // Will be auto-generated
   Name = "Test Product",
 Price = 99.99m,
            Category = "Electronics",
          StockQuantity = 10
   };

        var result = await _context.Save(product);
        
        if (result.Id == 0)
throw new Exception("Save failed - ID was not generated");
    }

    private async Task TestSaveStringId()
    {
        var order = new SqlOrder
    {
            Id = "ORD-" + Guid.NewGuid().ToString().Substring(0, 8),
            CustomerId = "CUST001",
Region = "North",
  TotalAmount = 150.00m,
            Status = "Pending"
        };

        var result = await _context.Save(order);
        
    if (string.IsNullOrEmpty(result.Id))
          throw new Exception("Save failed - ID is empty");
    }

    private async Task TestSaveMultiple()
    {
        var products = new List<SqlProduct>
    {
            new SqlProduct
      {
         Id = 0,
                Name = "Bulk Product 1",
           Price = 25.00m,
       Category = "Books",
      StockQuantity = 5
            },
new SqlProduct
            {
     Id = 0,
     Name = "Bulk Product 2",
    Price = 35.00m,
        Category = "Books",
          StockQuantity = 8
  }
        };

   var savedProducts = await _context.Save(products);
  
        if (savedProducts.Count != 2)
     throw new Exception($"Save multiple expected 2 but saved {savedProducts.Count}");
}

    private async Task TestGetByIntId()
    {
        var product = new SqlProduct
        {
      Id = 0,
  Name = "Get Test Product",
            Price = 45.00m,
       Category = "Toys",
       StockQuantity = 15
        };

        var saved = await _context.Save(product);
        var retrieved = await _context.Get<SqlProduct>(saved.Id);

        if (retrieved == null || retrieved.Name != product.Name)
      throw new Exception("Get by Int ID failed");
    }

    private async Task TestGetByStringId()
    {
        var orderId = "ORD-GET-" + Guid.NewGuid().ToString().Substring(0, 8);
        var order = new SqlOrder
        {
      Id = orderId,
            CustomerId = "CUST002",
            Region = "South",
       TotalAmount = 200.00m,
   Status = "Confirmed"
  };

        await _context.Save(order);
        var retrieved = await _context.Get<SqlOrder>(orderId);

        if (retrieved == null || retrieved.CustomerId != "CUST002")
throw new Exception("Get by String ID failed");
    }

    private async Task TestGetAll()
    {
        // Ensure at least one product exists
      await _context.Save(new SqlProduct
        {
   Id = 0,
      Name = "GetAll Test",
  Price = 10.00m,
    Category = "Test",
            StockQuantity = 1
        });

  var all = await _context.Get<SqlProduct>(trackEntities: false);

     if (all == null || all.Count == 0)
            throw new Exception("Get all returned no results");
    }

    private async Task TestGetWithPredicate()
 {
        var uniqueName = "Predicate Test " + Guid.NewGuid().ToString().Substring(0, 8);
    var product = new SqlProduct
        {
            Id = 0,
      Name = uniqueName,
     Price = 55.00m,
  Category = "Gadgets",
            StockQuantity = 20
        };

  await _context.Save(product);

        var retrieved = await _context.Get<SqlProduct>(p => p.Name == uniqueName, trackEntities: false);

        if (retrieved == null)
      throw new Exception("Get with predicate failed");
    }

    private async Task TestGetOne()
    {
     // Ensure at least one product exists
        await _context.Save(new SqlProduct
      {
        Id = 0,
       Name = "GetOne Test",
 Price = 12.00m,
    Category = "Test",
 StockQuantity = 1
        });

        var one = await _context.GetOne<SqlProduct>(trackEntities: false);

        if (one == null)
 throw new Exception("GetOne returned null");
    }

    private async Task TestFind()
    {
        var category = "FindCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

        for (int i = 0; i < 3; i++)
        {
await _context.Save(new SqlProduct
            {
        Id = 0,
 Name = $"Find Test {i}",
    Price = 20.00m + i,
                Category = category,
        StockQuantity = 5
    });
        }

    var found = await _context.Find<SqlProduct>(p => p.Category == category, trackEntities: false);

        if (found.Count != 3)
        throw new Exception($"Find expected 3 items but got {found.Count}");
    }

    private async Task TestCount()
    {
        var initialCount = _context.Count<SqlProduct>();

        await _context.Save(new SqlProduct
        {
            Id = 0,
         Name = "Count Test",
       Price = 15.00m,
            Category = "Test",
 StockQuantity = 1
        });

        var newCount = _context.Count<SqlProduct>();

        if (newCount <= initialCount)
            throw new Exception("Count did not increase after save");
    }

    private async Task TestCountWithPredicate()
    {
     var category = "CountCategory_" + Guid.NewGuid().ToString().Substring(0, 8);

  for (int i = 0; i < 2; i++)
        {
          await _context.Save(new SqlProduct
            {
          Id = 0,
       Name = $"Count Predicate {i}",
         Price = 30.00m,
     Category = category,
      StockQuantity = 3
            });
        }

        var count = _context.Count<SqlProduct>(p => p.Category == category);

        if (count != 2)
   throw new Exception($"Count with predicate expected 2 but got {count}");
    }

    private async Task TestExists()
    {
    var uniqueName = "Exists Test " + Guid.NewGuid().ToString().Substring(0, 8);
        
        var exists1 = await _context.Exists<SqlProduct>(p => p.Name == uniqueName);
        if (exists1)
        throw new Exception("Exists returned true for non-existent item");

        await _context.Save(new SqlProduct
        {
            Id = 0,
      Name = uniqueName,
      Price = 40.00m,
            Category = "Test",
     StockQuantity = 1
    });

  var exists2 = await _context.Exists<SqlProduct>(p => p.Name == uniqueName);
        if (!exists2)
    throw new Exception("Exists returned false for existing item");
    }

    private async Task TestUpdate()
    {
        var product = new SqlProduct
    {
      Id = 0,
            Name = "Update Test",
          Price = 50.00m,
   Category = "UpdateCategory",
          StockQuantity = 10
        };

        var saved = await _context.Save(product);
        saved.Price = 60.00m;
        
        var updated = await _context.Update(saved);

        if (updated.Price != 60.00m)
         throw new Exception("Update failed to change price");
    }

    private async Task TestUpdateMultiple()
    {
        var category = "UpdateMultiple_" + Guid.NewGuid().ToString().Substring(0, 8);
var products = new List<SqlProduct>();

 for (int i = 0; i < 2; i++)
        {
            var product = await _context.Save(new SqlProduct
          {
    Id = 0,
     Name = $"Update Multi {i}",
        Price = 70.00m,
     Category = category,
 StockQuantity = 5
      });
            products.Add(product);
        }

     foreach (var p in products)
  {
       p.Price = 80.00m;
  }

        var updatedProducts = await _context.Update(products);

        if (updatedProducts.Count != 2)
            throw new Exception($"Update multiple expected 2 but updated {updatedProducts.Count}");
    }

    private async Task TestGetAsQueryable()
    {
   await _context.Save(new SqlProduct
        {
         Id = 0,
         Name = "Queryable Test",
            Price = 90.00m,
            Category = "QueryTest",
            StockQuantity = 7
 });

        var queryable = _context.GetAsQueryable<SqlProduct>(trackEntities: false);

      if (queryable == null)
            throw new Exception("GetAsQueryable returned null");

    var count = queryable.Count();
        if (count == 0)
            throw new Exception("Queryable has no items");
    }

    private async Task TestFindAsQueryable()
    {
     var category = "FindQueryable_" + Guid.NewGuid().ToString().Substring(0, 8);

        await _context.Save(new SqlProduct
        {
            Id = 0,
       Name = "Find Queryable",
      Price = 100.00m,
    Category = category,
        StockQuantity = 12
        });

    var queryable = _context.FindAsQueryable<SqlProduct>(
            p => p.Category == category,
            trackEntities: false);

        if (queryable == null)
 throw new Exception("FindAsQueryable returned null");

var list = queryable.ToList();
        if (list.Count == 0)
   throw new Exception("FindAsQueryable returned no items");
    }

    private async Task TestBatchOperations()
  {
        // Batch insert
        var products = new List<SqlProduct>
        {
        new SqlProduct { Id = 0, Name = "Batch 1", Price = 11.00m, Category = "Batch", StockQuantity = 1 },
       new SqlProduct { Id = 0, Name = "Batch 2", Price = 22.00m, Category = "Batch", StockQuantity = 2 }
        };

        foreach (var p in products)
        {
   _context.AddForInsert(p);
        }

        var insertCount = await _context.SaveChanges();
    if (insertCount != 2)
    throw new Exception($"Batch insert expected 2 but got {insertCount}");

        // Batch update
        var toUpdate = await _context.Find<SqlProduct>(p => p.Category == "Batch", trackEntities: false);
        foreach (var p in toUpdate)
    {
            p.Price += 5.00m;
       _context.AddForUpdate(p);
        }

        var updateCount = await _context.SaveChanges();
    if (updateCount != 2)
            throw new Exception($"Batch update expected 2 but got {updateCount}");

        // Batch delete
        foreach (var p in toUpdate)
   {
            _context.AddForDelete(p);
 }

    var deleteCount = await _context.SaveChanges();
        if (deleteCount != 2)
      throw new Exception($"Batch delete expected 2 but got {deleteCount}");
    }

    private async Task TestExecuteSql()
    {
   var tableName = "Products";
        var sql = $"UPDATE {tableName} SET StockQuantity = StockQuantity WHERE 1=0"; // No-op update

      var count = await _context.ExecuteSql(sql);

        // Should execute without error
    }

    private async Task TestDapperQuery()
    {
        var product = new SqlProduct
        {
            Id = 0,
    Name = "Dapper Test",
        Price = 110.00m,
        Category = "DapperCategory",
       StockQuantity = 25
        };

        var saved = await _context.Save(product);

        var sql = "SELECT * FROM Products WHERE Id = @Id";
        var result = await _context.QueryFirstOrDefault<SqlProduct>(sql, new { Id = saved.Id });

 if (result == null)
    throw new Exception("Dapper query returned null");

        if (result.Name != "Dapper Test")
     throw new Exception("Dapper query returned incorrect data");
    }

    private async Task TestDelete()
    {
        var product = new SqlProduct
      {
       Id = 0,
  Name = "Delete Test",
            Price = 19.99m,
     Category = "ToDelete",
            StockQuantity = 1
   };

        var saved = await _context.Save(product);
    var count = await _context.Delete(saved);

        if (count != 1)
          throw new Exception("Delete failed");

        var retrieved = await _context.Get<SqlProduct>(saved.Id);
        if (retrieved != null)
            throw new Exception("Delete failed - item still exists");
    }

    private async Task TestDeleteWithPredicate()
    {
   var category = "DeletePredicate_" + Guid.NewGuid().ToString().Substring(0, 8);

     for (int i = 0; i < 2; i++)
        {
         await _context.Save(new SqlProduct
            {
         Id = 0,
  Name = $"Delete Pred {i}",
       Price = 29.99m,
          Category = category,
   StockQuantity = 1
 });
  }

      var count = await _context.Delete<SqlProduct>(p => p.Category == category);

if (count != 2)
            throw new Exception($"Delete with predicate expected 2 but deleted {count}");
    }

    private async Task TestDeleteMultiple()
    {
        var category = "DeleteMultiple_" + Guid.NewGuid().ToString().Substring(0, 8);
        var products = new List<SqlProduct>();

  for (int i = 0; i < 2; i++)
        {
            var product = await _context.Save(new SqlProduct
   {
         Id = 0,
    Name = $"Delete Multi {i}",
     Price = 39.99m,
      Category = category,
   StockQuantity = 1
  });
     products.Add(product);
        }

        var count = await _context.Delete(products);

    if (count != 2)
     throw new Exception($"Delete multiple expected 2 but deleted {count}");
    }
}
