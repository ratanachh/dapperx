using System.Data;
using Dapper;

namespace Dapper.Npa.SampleApp;

public static class AppDb
{
    public static async Task EnsureSchemaAsync(IDbConnection connection, string provider)
    {
        foreach (var ddl in GetDdl(provider))
            await connection.ExecuteAsync(ddl);
    }

    private static IEnumerable<string> GetDdl(string provider) =>
        provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) ? SqliteDdl() : SqlServerDdl();

    private static IEnumerable<string> SqlServerDdl()
    {
        yield return """
            IF OBJECT_ID('sample_order_lines', 'U') IS NOT NULL DROP TABLE sample_order_lines;
            IF OBJECT_ID('sample_orders', 'U') IS NOT NULL DROP TABLE sample_orders;
            IF OBJECT_ID('sample_graph_children', 'U') IS NOT NULL DROP TABLE sample_graph_children;
            IF OBJECT_ID('sample_graph_parents', 'U') IS NOT NULL DROP TABLE sample_graph_parents;
            IF OBJECT_ID('sample_employees', 'U') IS NOT NULL DROP TABLE sample_employees;
            IF OBJECT_ID('sample_departments', 'U') IS NOT NULL DROP TABLE sample_departments;
            IF OBJECT_ID('user_profiles', 'U') IS NOT NULL DROP TABLE user_profiles;
            IF OBJECT_ID('member_profiles', 'U') IS NOT NULL DROP TABLE member_profiles;
            IF OBJECT_ID('members', 'U') IS NOT NULL DROP TABLE members;
            IF OBJECT_ID('app_users', 'U') IS NOT NULL DROP TABLE app_users;
            IF OBJECT_ID('catalog_products', 'U') IS NOT NULL DROP TABLE catalog_products;
            """;
        yield return """
            CREATE TABLE catalog_products (
              id INT IDENTITY(1,1) PRIMARY KEY,
              sku NVARCHAR(64) NOT NULL,
              name NVARCHAR(128) NOT NULL,
              category NVARCHAR(64) NOT NULL,
              price DECIMAL(18,2) NOT NULL,
              in_stock BIT NOT NULL DEFAULT 1,
              status NVARCHAR(32) NOT NULL DEFAULT 'Active',
              secret_payload NVARCHAR(256) NOT NULL DEFAULT '',
              created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
              updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """;
        yield return """
            CREATE TABLE app_users (
              id INT IDENTITY(1,1) PRIMARY KEY,
              email NVARCHAR(256) NOT NULL,
              region NVARCHAR(16) NOT NULL DEFAULT 'US',
              address_city NVARCHAR(64) NOT NULL DEFAULT '',
              address_country NVARCHAR(64) NOT NULL DEFAULT '',
              tenant_id UNIQUEIDENTIFIER NOT NULL,
              is_deleted BIT NOT NULL DEFAULT 0,
              row_version INT NOT NULL DEFAULT 0,
              created_at DATETIME2 NOT NULL,
              modified_at DATETIME2 NOT NULL,
              created_by NVARCHAR(64) NOT NULL,
              modified_by NVARCHAR(64) NOT NULL,
              db_created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """;
        yield return """
            CREATE TABLE members (
              id INT IDENTITY(1,1) PRIMARY KEY,
              email NVARCHAR(256) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE member_profiles (
              member_id INT NOT NULL PRIMARY KEY,
              bio NVARCHAR(512) NOT NULL DEFAULT ''
            )
            """;
        yield return """
            CREATE TABLE user_profiles (
              id INT NOT NULL PRIMARY KEY,
              display_name NVARCHAR(128) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE sample_departments (
              id INT IDENTITY(1,1) PRIMARY KEY,
              name NVARCHAR(128) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE sample_employees (
              id INT IDENTITY(1,1) PRIMARY KEY,
              department_id INT NOT NULL,
              employee_code NVARCHAR(32) NOT NULL,
              full_name NVARCHAR(128) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE sample_orders (
              id INT IDENTITY(1,1) PRIMARY KEY,
              code NVARCHAR(64) NOT NULL,
              total_with_tax DECIMAL(18,2) NOT NULL DEFAULT 0,
              created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            )
            """;
        yield return """
            CREATE TABLE sample_order_lines (
              id INT IDENTITY(1,1) PRIMARY KEY,
              order_id INT NOT NULL,
              sku NVARCHAR(64) NOT NULL,
              position INT NOT NULL
            )
            """;
        yield return """
            CREATE TABLE sample_graph_parents (
              id INT IDENTITY(1,1) PRIMARY KEY,
              name NVARCHAR(128) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE sample_graph_children (
              id INT IDENTITY(1,1) PRIMARY KEY,
              parent_id INT NOT NULL,
              label NVARCHAR(128) NOT NULL
            )
            """;
    }

    private static IEnumerable<string> SqliteDdl()
    {
        yield return "DROP TABLE IF EXISTS sample_graph_children";
        yield return "DROP TABLE IF EXISTS sample_graph_parents";
        yield return "DROP TABLE IF EXISTS sample_order_lines";
        yield return "DROP TABLE IF EXISTS sample_orders";
        yield return "DROP TABLE IF EXISTS sample_employees";
        yield return "DROP TABLE IF EXISTS sample_departments";
        yield return "DROP TABLE IF EXISTS user_profiles";
        yield return "DROP TABLE IF EXISTS member_profiles";
        yield return "DROP TABLE IF EXISTS members";
        yield return "DROP TABLE IF EXISTS app_users";
        yield return "DROP TABLE IF EXISTS catalog_products";
        yield return "CREATE TABLE catalog_products (id INTEGER PRIMARY KEY AUTOINCREMENT, sku TEXT NOT NULL, name TEXT NOT NULL, category TEXT NOT NULL, price REAL NOT NULL, in_stock INTEGER NOT NULL DEFAULT 1, status TEXT NOT NULL DEFAULT 'Active', secret_payload TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL, updated_at TEXT NOT NULL)";
        yield return "CREATE TABLE app_users (id INTEGER PRIMARY KEY AUTOINCREMENT, email TEXT NOT NULL, region TEXT NOT NULL DEFAULT 'US', address_city TEXT NOT NULL DEFAULT '', address_country TEXT NOT NULL DEFAULT '', tenant_id TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0, row_version INTEGER NOT NULL DEFAULT 0, created_at TEXT NOT NULL, modified_at TEXT NOT NULL, created_by TEXT NOT NULL, modified_by TEXT NOT NULL, db_created_at TEXT NOT NULL)";
        yield return "CREATE TABLE members (id INTEGER PRIMARY KEY AUTOINCREMENT, email TEXT NOT NULL)";
        yield return "CREATE TABLE member_profiles (member_id INTEGER NOT NULL PRIMARY KEY, bio TEXT NOT NULL DEFAULT '')";
        yield return "CREATE TABLE user_profiles (id INTEGER NOT NULL PRIMARY KEY, display_name TEXT NOT NULL)";
        yield return "CREATE TABLE sample_departments (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL)";
        yield return "CREATE TABLE sample_employees (id INTEGER PRIMARY KEY AUTOINCREMENT, department_id INTEGER NOT NULL, employee_code TEXT NOT NULL, full_name TEXT NOT NULL)";
        yield return "CREATE TABLE sample_orders (id INTEGER PRIMARY KEY AUTOINCREMENT, code TEXT NOT NULL, total_with_tax REAL NOT NULL DEFAULT 0, created_at TEXT NOT NULL)";
        yield return "CREATE TABLE sample_order_lines (id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, sku TEXT NOT NULL, position INTEGER NOT NULL)";
        yield return "CREATE TABLE sample_graph_parents (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL)";
        yield return "CREATE TABLE sample_graph_children (id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER NOT NULL, label TEXT NOT NULL)";
    }
}
