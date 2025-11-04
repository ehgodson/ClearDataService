using Clear.DataService.Abstractions;
using Clear.DataService.Entities.Cosmos;
using Clear.DataService.Models;
using Clear.DataService.Utils;

namespace Clear.DataService.Examples;

/// <summary>
/// Examples demonstrating the enhanced GetList and GetDocuments methods with sorting support
/// </summary>
public class GetListSortingExamples
{
    private readonly ICosmosDbContext _context;

    public GetListSortingExamples(ICosmosDbContext context)
    {
        _context = context;
    }

    public class Product : ICosmosDbEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ============================================
    // SIMPLE SORTING EXAMPLES
    // ============================================

    /// <summary>
    /// Example 1: Simple sorting by name
    /// </summary>
    public async Task<List<Product>> GetProductsSortedByName()
    {
        var sort = SortBuilder<Product>.Create()
            .ThenBy(doc => doc.Data.Name);

        return await _context.GetList<Product>(
            "Products",
       sortBuilder: sort
        );
    }

    /// <summary>
    /// Example 2: Sort by price descending (most expensive first)
    /// </summary>
    public async Task<List<Product>> GetProductsByPriceDesc()
    {
        var sort = SortBuilder<Product>.Create()
          .ThenByDescending(doc => doc.Data.Price);

        return await _context.GetList<Product>(
    "Products",
 sortBuilder: sort
        );
    }

    // ============================================
    // FILTER + SORT EXAMPLES
    // ============================================

    /// <summary>
    /// Example 3: Get active products sorted by name
    /// </summary>
    public async Task<List<Product>> GetActiveProductsSorted()
    {
        var filter = FilterBuilder<Product>.Create()
          .And(doc => doc.Data.IsActive);

        var sort = SortBuilder<Product>.Create()
    .ThenBy(doc => doc.Data.Name);

        return await _context.GetList<Product>(
                  "Products",
          filter: filter,
         sortBuilder: sort
              );
    }

    /// <summary>
    /// Example 4: Get in-stock products sorted by price
    /// </summary>
    public async Task<List<Product>> GetInStockProductsByPrice()
    {
        var filter = FilterBuilder<Product>.Create()
             .And(doc => doc.Data.Stock > 0);

        var sort = SortBuilder<Product>.Create()
               .ThenByDescending(doc => doc.Data.Price);

        return await _context.GetList<Product>(
         "Products",
  filter: filter,
            sortBuilder: sort
   );
    }

    // ============================================
    // PARTITION + FILTER + SORT EXAMPLES
    // ============================================

    /// <summary>
    /// Example 5: Get electronics, in stock, sorted by price
    /// </summary>
    public async Task<List<Product>> GetElectronicsByPrice(string tenantId)
    {
        var partitionKey = CosmosDbPartitionKey.Create(tenantId, "electronics");

        var filter = FilterBuilder<Product>.Create()
            .And(doc => doc.Data.Stock > 0)
            .And(doc => doc.Data.IsActive);

        var sort = SortBuilder<Product>.Create()
            .ThenByDescending(doc => doc.Data.Price)
         .ThenBy(doc => doc.Data.Name);

        return await _context.GetList<Product>(
            "Products",
            partitionKey: partitionKey,
 filter: filter,
  sortBuilder: sort
     );
    }

    // ============================================
    // MULTI-LEVEL SORTING EXAMPLES
    // ============================================

    /// <summary>
    /// Example 6: Sort by category, then price, then name
    /// </summary>
    public async Task<List<Product>> GetProductsMultiSort()
    {
        var sort = SortBuilder<Product>.Create()
                  .ThenBy(doc => doc.Data.Category)        // First by category
                  .ThenByDescending(doc => doc.Data.Price) // Then by price desc
                  .ThenBy(doc => doc.Data.Name);           // Finally by name

        return await _context.GetList<Product>(
     "Products",
            sortBuilder: sort
        );
    }

    // ============================================
    // TOP N RESULTS EXAMPLES
    // ============================================

    /// <summary>
    /// Example 7: Get top 10 most expensive products
    /// </summary>
    public async Task<List<Product>> GetTop10MostExpensive()
    {
        var sort = SortBuilder<Product>.Create()
     .ThenByDescending(doc => doc.Data.Price);

        var allProducts = await _context.GetList<Product>(
         "Products",
     sortBuilder: sort
      );

        return allProducts.Take(10).ToList();
    }

    /// <summary>
    /// Example 8: Get newest 20 products in a category
    /// </summary>
    public async Task<List<Product>> GetNewestProductsInCategory(string category)
    {
        var filter = FilterBuilder<Product>.Create()
         .And(doc => doc.Data.Category == category);

        var sort = SortBuilder<Product>.Create()
            .ThenByDescending(doc => doc.Data.CreatedDate);

        var products = await _context.GetList<Product>(
       "Products",
                filter: filter,
              sortBuilder: sort
            );

        return products.Take(20).ToList();
    }

    // ============================================
    // CONDITIONAL SORTING EXAMPLES
    // ============================================

    /// <summary>
    /// Example 9: Dynamic sorting based on user preference
    /// </summary>
    public async Task<List<Product>> GetProductsWithDynamicSort(
        string? category = null,
        string sortBy = "name",
    bool descending = false)
    {
        // Build filter
        var filter = FilterBuilder<Product>.Create();
        if (!string.IsNullOrEmpty(category))
        {
            filter.And(doc => doc.Data.Category == category);
        }

        // Build sort
        var sort = SortBuilder<Product>.Create();
        var direction = descending ? SortDirection.Descending : SortDirection.Ascending;

        switch (sortBy.ToLower())
        {
            case "name":
                sort.ThenBy(doc => doc.Data.Name, direction);
                break;
            case "price":
                sort.ThenBy(doc => doc.Data.Price, direction);
                break;
            case "date":
                sort.ThenBy(doc => doc.Data.CreatedDate, direction);
                break;
            case "stock":
                sort.ThenBy(doc => doc.Data.Stock, direction);
                break;
            default:
                sort.ThenBy(doc => doc.Data.Name); // Default sort
                break;
        }

        return await _context.GetList<Product>(
              "Products",
                  filter: filter.HasFilters ? filter : null,
                sortBuilder: sort
              );
    }

    // ============================================
    // DOCUMENT OPERATIONS WITH SORTING
    // ============================================

    /// <summary>
    /// Example 10: Get documents with metadata, sorted
    /// </summary>
    public async Task<List<CosmosDbDocument<Product>>> GetProductDocumentsSorted()
    {
        var sort = SortBuilder<Product>.Create()
         .ThenBy(doc => doc.Data.Category)
              .ThenBy(doc => doc.Data.Name);

        var documents = await _context.GetDocuments<Product>(
               "Products",
       sortBuilder: sort
           );

        // Access both data and metadata
        foreach (var doc in documents)
        {
            Console.WriteLine($"Product: {doc.Data.Name}");
            Console.WriteLine($"  ETag: {doc.ETag}");
            Console.WriteLine($"  Timestamp: {doc.Timestamp}");
        }

        return documents;
    }

    /// <summary>
    /// Example 11: Get documents with filter and sort
    /// </summary>
    public async Task<List<CosmosDbDocument<Product>>> GetActiveProductDocuments()
    {
        var filter = FilterBuilder<Product>.Create()
     .And(doc => doc.Data.IsActive)
            .And(doc => doc.Data.Stock > 0);

        var sort = SortBuilder<Product>.Create()
         .ThenByDescending(doc => doc.Data.Price);

        return await _context.GetDocuments<Product>(
  "Products",
    filter: filter,
  sortBuilder: sort
        );
    }

    // ============================================
    // COMPARISON: GetList vs GetPagedList
    // ============================================

    /// <summary>
    /// Example 12: When to use GetList vs GetPagedList
    /// </summary>
    public async Task<(List<Product> allProducts, PagedResult<Product> pagedProducts)>
        CompareGetListVsGetPagedList()
    {
        var sort = SortBuilder<Product>.Create()
   .ThenBy(doc => doc.Data.Name);

        // GetList - Get all results at once (good for small datasets)
        var allProducts = await _context.GetList<Product>(
        "Products",
   partitionKey: CosmosDbPartitionKey.Create("small-tenant"),
            sortBuilder: sort
        );

        // GetPagedList - Get results in pages (good for large datasets)
        var pagedProducts = await _context.GetPagedList<Product>(
"Products",
         partitionKey: CosmosDbPartitionKey.Create("large-tenant"),
      pageSize: 50,
            sortBuilder: sort
        );

        return (allProducts, pagedProducts);
    }

    // ============================================
    // ADVANCED SORTING SCENARIOS
    // ============================================

    /// <summary>
    /// Example 13: Complex business logic sorting
    /// </summary>
    public async Task<List<Product>> GetProductsWithBusinessLogicSort()
    {
        // Get in-stock items first, then out-of-stock
        // Within each group, sort by price descending
        var sort = SortBuilder<Product>.Create()
            .ThenByDescending(doc => doc.Data.Stock > 0 ? 1 : 0) // In stock first
    .ThenByDescending(doc => doc.Data.Price)      // Then by price
            .ThenBy(doc => doc.Data.Name);       // Finally by name

        var filter = FilterBuilder<Product>.Create()
          .And(doc => doc.Data.IsActive);

        return await _context.GetList<Product>(
         "Products",
       filter: filter,
     sortBuilder: sort
        );
    }

    /// <summary>
    /// Example 14: Alphabetical grouping with secondary sort
    /// </summary>
    public async Task<List<Product>> GetProductsAlphabeticalGrouping()
    {
        // Group by first letter of name, then sort by price within group
        var sort = SortBuilder<Product>.Create()
   .ThenBy(doc => doc.Data.Name.Substring(0, 1)) // First letter
     .ThenByDescending(doc => doc.Data.Price);       // Price within group

        return await _context.GetList<Product>(
                "Products",
            sortBuilder: sort
            );
    }
}
