using Clear.DataService.Contexts;
using Clear.DataService.Utils;
using ClearDataService.IntegrationTests.Data;
using ClearDataService.IntegrationTests.Tests;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace ClearDataService.IntegrationTests;

class Program
{
    static async Task Main(string[] args)
    {
        PrintHeader();

        var testType = GetTestTypeChoice();

        if (testType == TestType.CosmosDb)
        {
            await RunCosmosDbTests();
        }
        else if (testType == TestType.SqlServer)
        {
            await RunSqlServerTests();
        }
        else
        {
            Console.WriteLine("Invalid choice. Exiting...");
            return;
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void PrintHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     ║");
        Console.WriteLine("║     ClearDataService Integration Test Console       ║");
        Console.WriteLine("║  ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    static TestType GetTestTypeChoice()
    {
        Console.WriteLine("Select the type of integration test to run:\n");
        Console.WriteLine("  1. CosmosDB Integration Tests");
        Console.WriteLine("  2. SQL Server Integration Tests");
        Console.WriteLine();
        Console.Write("Enter your choice (1 or 2): ");

        var choice = Console.ReadLine();

        return choice switch
        {
            "1" => TestType.CosmosDb,
            "2" => TestType.SqlServer,
            _ => TestType.Invalid
        };
    }

    static async Task RunCosmosDbTests()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("   COSMOSDB CONFIGURATION");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        Console.Write("Enter CosmosDB Connection String: ");
        var connectionString = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Connection string cannot be empty.");
            Console.ResetColor();
            return;
        }

        Console.Write("Enter Database Name: ");
        var databaseName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Database name cannot be empty.");
            Console.ResetColor();
            return;
        }

        Console.Write("Enter Container Name: ");
        var containerName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(containerName))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Container name cannot be empty.");
            Console.ResetColor();
            return;
        }

        Console.Write("Enter Partition Key (e.g., 'test-partition'): ");
        var partitionKey = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Partition key cannot be empty.");
            Console.ResetColor();
            return;
        }

        try
        {
            Console.WriteLine("\nInitializing CosmosDB Client...");

            var cosmosClient = new CosmosClient(connectionString);
            var settings = new CosmosDbSettings(databaseName);
            var context = new CosmosDbContext(cosmosClient, settings);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ CosmosDB Client initialized successfully");
            Console.ResetColor();

            // Ensure database and container exist
            var containerConfig = await EnsureCosmosDbSetup(cosmosClient, databaseName, containerName);

            // Check if container supports hierarchical partition keys
            if (containerConfig?.PartitionKeyPaths?.Count == 1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠ Note: Container uses single partition key path. Hierarchical partition key tests will be skipped.");
                Console.WriteLine("  To test hierarchical partition keys, create a container with multiple partition key paths.");
                Console.ResetColor();
            }

            var tests = new CosmosDbIntegrationTests(context, containerName, partitionKey);
            var allPassed = await tests.RunAllTests();

            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ All CosmosDB tests completed successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Some CosmosDB tests failed. Please review the output above.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Error running CosmosDB tests: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            Console.ResetColor();
        }
    }

    static async Task RunSqlServerTests()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("   SQL SERVER CONFIGURATION");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        Console.Write("Enter SQL Server Connection String: ");
        var connectionString = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Connection string cannot be empty.");
            Console.ResetColor();
            return;
        }

        try
        {
            Console.WriteLine("\nInitializing SQL Server Context...");

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var dbContext = new TestDbContext(options);

            // Ensure database is created and migrated
            Console.WriteLine("Ensuring database exists...");
            await dbContext.Database.EnsureCreatedAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ SQL Server Context initialized successfully");
            Console.ResetColor();

            var sqlContext = new SqlDbContext(dbContext);
            var tests = new SqlDbIntegrationTests(sqlContext);

            var allPassed = await tests.RunAllTests();

            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ All SQL Server tests completed successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Some SQL Server tests failed. Please review the output above.");
                Console.ResetColor();
            }

            // Optional: Clean up test data
            Console.Write("\nDo you want to clean up test data? (y/n): ");
            var cleanup = Console.ReadLine();
            if (cleanup?.ToLower() == "y")
            {
                Console.WriteLine("Cleaning up test database...");
                await dbContext.Database.EnsureDeletedAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Test database cleaned up successfully");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Error running SQL Server tests: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            Console.ResetColor();
        }
    }

    static async Task<ContainerProperties?> EnsureCosmosDbSetup(CosmosClient client, string databaseName, string containerName)
    {
        try
        {
            Console.WriteLine("Ensuring CosmosDB database and container exist...");

            // Create database if it doesn't exist
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            var database = databaseResponse.Database;

            // Create container if it doesn't exist with single partition key
            var containerProperties = new ContainerProperties
            {
                Id = containerName,
                PartitionKeyPath = "/partitionKey"
            };

            var response = await database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
            var container = response.Container;

            // Read the actual container properties to check configuration
            var actualProperties = await container.ReadContainerAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Database '{databaseName}' and Container '{containerName}' are ready");
            Console.ResetColor();

            return actualProperties.Resource;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ Warning: Could not ensure database/container setup: {ex.Message}");
            Console.WriteLine("Proceeding with tests - ensure the database and container exist manually.");
            Console.ResetColor();
            return null;
        }
    }
}

enum TestType
{
    Invalid,
    CosmosDb,
    SqlServer
}

/// <summary>
/// CosmosDB Settings implementation
/// </summary>
public class CosmosDbSettings : ICosmosDbSettings
{
    public string EndpointUri { get; }
    public string PrimaryKey { get; }
  public string DatabaseName { get; }

  public CosmosDbSettings(string databaseName)
    {
 DatabaseName = databaseName;
     EndpointUri = string.Empty;  // Not needed when using connection string
  PrimaryKey = string.Empty;   // Not needed when using connection string
    }
}
