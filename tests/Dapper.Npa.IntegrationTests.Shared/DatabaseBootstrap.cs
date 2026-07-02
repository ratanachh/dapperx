using System.Data;
using Dapper;

namespace Dapper.Npa.IntegrationTests.Shared;

public static class DatabaseBootstrap
{
    public static async Task CreateSchemaAsync(IDbConnection connection, string provider)
    {
        foreach (var ddl in GetDdl(provider))
            await connection.ExecuteAsync(ddl);

        if (provider != "Sqlite")
            await IntegrationProcedureBootstrap.CreateProceduresAsync(connection, provider);
    }

    private static IEnumerable<string> GetDdl(string provider) => provider switch
    {
        "Sqlite" => SqliteDdl(),
        "PostgreSql" => PostgreSqlDdl(),
        "MySql" => MySqlDdl(),
        _ => SqlServerDdl(),
    };

    private static IEnumerable<string> SqlServerDdl()
    {
        yield return """
            CREATE TABLE integ_catalog (
              id INT NOT NULL PRIMARY KEY,
              sku NVARCHAR(64) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE integ_archived (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL,
              is_deleted BIT NOT NULL DEFAULT 0
            )
            """;
        yield return """
            CREATE TABLE integ_tenant_items (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL,
              tenant_id UNIQUEIDENTIFIER NOT NULL
            )
            """;
        yield return """
            CREATE TABLE integ_audited (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL,
              created_at DATETIME2 NOT NULL,
              created_by NVARCHAR(64) NOT NULL,
              modified_at DATETIME2 NOT NULL,
              modified_by NVARCHAR(64) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE integ_bulk (
              id BIGINT NOT NULL PRIMARY KEY,
              code NVARCHAR(32) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE integ_parents (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL
            )
            """;
        yield return """
            CREATE TABLE integ_children (
              id INT NOT NULL PRIMARY KEY,
              parent_id INT NOT NULL,
              label NVARCHAR(128) NOT NULL
            )
            """;
        foreach (var ddl in SqlServerAdvancedDdl())
            yield return ddl;
        foreach (var ddl in RuntimeExtrasDdl("SqlServer"))
            yield return ddl;
    }

    private static IEnumerable<string> PostgreSqlDdl()
    {
        yield return "CREATE TABLE integ_catalog (id SERIAL PRIMARY KEY, sku VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_archived (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL, is_deleted BOOLEAN NOT NULL DEFAULT FALSE)";
        yield return "CREATE TABLE integ_tenant_items (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL, tenant_id UUID NOT NULL)";
        yield return "CREATE TABLE integ_audited (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL, created_at TIMESTAMP NOT NULL, created_by VARCHAR(64) NOT NULL, modified_at TIMESTAMP NOT NULL, modified_by VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_bulk (id BIGINT PRIMARY KEY, code VARCHAR(32) NOT NULL)";
        yield return "CREATE TABLE integ_parents (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_children (id SERIAL PRIMARY KEY, parent_id INT NOT NULL, label VARCHAR(128) NOT NULL)";
        foreach (var ddl in PostgreSqlAdvancedDdl())
            yield return ddl;
        foreach (var ddl in RuntimeExtrasDdl("PostgreSql"))
            yield return ddl;
    }

    private static IEnumerable<string> MySqlDdl()
    {
        yield return "CREATE TABLE integ_catalog (id INT AUTO_INCREMENT PRIMARY KEY, sku VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_archived (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL, is_deleted TINYINT(1) NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_tenant_items (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL, tenant_id CHAR(36) NOT NULL)";
        yield return "CREATE TABLE integ_audited (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL, created_at DATETIME NOT NULL, created_by VARCHAR(64) NOT NULL, modified_at DATETIME NOT NULL, modified_by VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_bulk (id BIGINT PRIMARY KEY, code VARCHAR(32) NOT NULL)";
        yield return "CREATE TABLE integ_parents (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_children (id INT AUTO_INCREMENT PRIMARY KEY, parent_id INT NOT NULL, label VARCHAR(128) NOT NULL)";
        foreach (var ddl in MySqlAdvancedDdl())
            yield return ddl;
        foreach (var ddl in RuntimeExtrasDdl("MySql"))
            yield return ddl;
    }

    private static IEnumerable<string> SqliteDdl()
    {
        yield return "CREATE TABLE integ_catalog (id INTEGER PRIMARY KEY AUTOINCREMENT, sku TEXT NOT NULL)";
        yield return "CREATE TABLE integ_archived (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_tenant_items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, tenant_id TEXT NOT NULL)";
        yield return "CREATE TABLE integ_audited (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, created_at TEXT NOT NULL, created_by TEXT NOT NULL, modified_at TEXT NOT NULL, modified_by TEXT NOT NULL)";
        yield return "CREATE TABLE integ_bulk (id INTEGER PRIMARY KEY, code TEXT NOT NULL)";
        yield return "CREATE TABLE integ_parents (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL)";
        yield return "CREATE TABLE integ_children (id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER NOT NULL, label TEXT NOT NULL)";
        foreach (var ddl in SqliteAdvancedDdl())
            yield return ddl;
        foreach (var ddl in RuntimeExtrasDdl("Sqlite"))
            yield return ddl;
    }

    private static IEnumerable<string> RuntimeExtrasDdl(string provider) => provider switch
    {
        "Sqlite" =>
        [
            "CREATE TABLE integ_filtered_catalog (id INTEGER PRIMARY KEY, sku TEXT NOT NULL, is_active INTEGER NOT NULL DEFAULT 1)",
            "CREATE TABLE integ_transform_products (id INTEGER PRIMARY KEY, name TEXT NOT NULL)",
            "CREATE TABLE integ_query_customers (id INTEGER PRIMARY KEY, name TEXT NOT NULL)",
            "CREATE TABLE integ_query_products (id INTEGER PRIMARY KEY, sku TEXT NOT NULL, customer_id INTEGER NOT NULL)",
            "CREATE TABLE integ_tenant_region_users (id INTEGER PRIMARY KEY AUTOINCREMENT, email TEXT NOT NULL, region TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0, tenant_id TEXT NOT NULL)",
        ],
        "PostgreSql" =>
        [
            "CREATE TABLE integ_filtered_catalog (id SERIAL PRIMARY KEY, sku VARCHAR(64) NOT NULL, is_active BOOLEAN NOT NULL DEFAULT TRUE)",
            "CREATE TABLE integ_transform_products (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL)",
            "CREATE TABLE integ_query_customers (id SERIAL PRIMARY KEY, name VARCHAR(128) NOT NULL)",
            "CREATE TABLE integ_query_products (id SERIAL PRIMARY KEY, sku VARCHAR(64) NOT NULL, customer_id INT NOT NULL)",
            "CREATE TABLE integ_tenant_region_users (id SERIAL PRIMARY KEY, email VARCHAR(128) NOT NULL, region VARCHAR(16) NOT NULL, is_deleted BOOLEAN NOT NULL DEFAULT FALSE, tenant_id UUID NOT NULL)",
        ],
        "MySql" =>
        [
            "CREATE TABLE integ_filtered_catalog (id INT AUTO_INCREMENT PRIMARY KEY, sku VARCHAR(64) NOT NULL, is_active TINYINT(1) NOT NULL DEFAULT 1)",
            "CREATE TABLE integ_transform_products (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL)",
            "CREATE TABLE integ_query_customers (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(128) NOT NULL)",
            "CREATE TABLE integ_query_products (id INT AUTO_INCREMENT PRIMARY KEY, sku VARCHAR(64) NOT NULL, customer_id INT NOT NULL)",
            "CREATE TABLE integ_tenant_region_users (id INT AUTO_INCREMENT PRIMARY KEY, email VARCHAR(128) NOT NULL, region VARCHAR(16) NOT NULL, is_deleted TINYINT(1) NOT NULL DEFAULT 0, tenant_id CHAR(36) NOT NULL)",
        ],
        _ =>
        [
            """
            CREATE TABLE integ_filtered_catalog (
              id INT NOT NULL PRIMARY KEY,
              sku NVARCHAR(64) NOT NULL,
              is_active BIT NOT NULL DEFAULT 1
            )
            """,
            """
            CREATE TABLE integ_transform_products (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL
            )
            """,
            """
            CREATE TABLE integ_query_customers (
              id INT NOT NULL PRIMARY KEY,
              name NVARCHAR(128) NOT NULL
            )
            """,
            """
            CREATE TABLE integ_query_products (
              id INT NOT NULL PRIMARY KEY,
              sku NVARCHAR(64) NOT NULL,
              customer_id INT NOT NULL
            )
            """,
            """
            CREATE TABLE integ_tenant_region_users (
              id INT IDENTITY(1,1) PRIMARY KEY,
              email NVARCHAR(128) NOT NULL,
              region NVARCHAR(16) NOT NULL,
              is_deleted BIT NOT NULL DEFAULT 0,
              tenant_id UNIQUEIDENTIFIER NOT NULL
            )
            """,
        ],
    };

    private static IEnumerable<string> SqlServerAdvancedDdl()
    {
        yield return "CREATE TABLE integ_composite_items (order_id INT NOT NULL, product_id INT NOT NULL, quantity INT NOT NULL, PRIMARY KEY (order_id, product_id))";
        yield return "CREATE TABLE integ_documents (id INT NOT NULL PRIMARY KEY, title NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_document_details (document_id INT NOT NULL PRIMARY KEY, summary NVARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_users (id INT NOT NULL PRIMARY KEY, email NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_user_profiles (id INT NOT NULL PRIMARY KEY, display_name NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_gallery_products (id INT NOT NULL PRIMARY KEY, sku NVARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_product_images (product_id INT NOT NULL, image_url NVARCHAR(256) NOT NULL, image_caption NVARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_departments (id INT NOT NULL PRIMARY KEY, name NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_employees (id INT NOT NULL PRIMARY KEY, department_id INT NOT NULL, employee_code NVARCHAR(32) NOT NULL, full_name NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_parents (id INT NOT NULL PRIMARY KEY, name NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_children (id INT NOT NULL PRIMARY KEY, parent_id INT NOT NULL, label NVARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_orders (id INT NOT NULL PRIMARY KEY, code NVARCHAR(64) NOT NULL, is_deleted BIT NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_graph_order_lines (id INT NOT NULL PRIMARY KEY, order_id INT NOT NULL, sku NVARCHAR(64) NOT NULL, is_deleted BIT NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_proc_orders (id INT NOT NULL PRIMARY KEY, code NVARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_proc_lines (id INT NOT NULL PRIMARY KEY, order_id INT NOT NULL, sku NVARCHAR(64) NOT NULL)";
    }

    private static IEnumerable<string> PostgreSqlAdvancedDdl()
    {
        yield return "CREATE TABLE integ_composite_items (order_id INT NOT NULL, product_id INT NOT NULL, quantity INT NOT NULL, PRIMARY KEY (order_id, product_id))";
        yield return "CREATE TABLE integ_documents (id INT PRIMARY KEY, title VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_document_details (document_id INT PRIMARY KEY, summary VARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_users (id INT PRIMARY KEY, email VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_user_profiles (id INT PRIMARY KEY, display_name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_gallery_products (id INT PRIMARY KEY, sku VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_product_images (product_id INT NOT NULL, image_url VARCHAR(256) NOT NULL, image_caption VARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_departments (id INT PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_employees (id INT PRIMARY KEY, department_id INT NOT NULL, employee_code VARCHAR(32) NOT NULL, full_name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_parents (id INT PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_children (id INT PRIMARY KEY, parent_id INT NOT NULL, label VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_orders (id INT PRIMARY KEY, code VARCHAR(64) NOT NULL, is_deleted BOOLEAN NOT NULL DEFAULT FALSE)";
        yield return "CREATE TABLE integ_graph_order_lines (id INT PRIMARY KEY, order_id INT NOT NULL, sku VARCHAR(64) NOT NULL, is_deleted BOOLEAN NOT NULL DEFAULT FALSE)";
        yield return "CREATE TABLE integ_proc_orders (id INT PRIMARY KEY, code VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_proc_lines (id INT PRIMARY KEY, order_id INT NOT NULL, sku VARCHAR(64) NOT NULL)";
    }

    private static IEnumerable<string> MySqlAdvancedDdl()
    {
        yield return "CREATE TABLE integ_composite_items (order_id INT NOT NULL, product_id INT NOT NULL, quantity INT NOT NULL, PRIMARY KEY (order_id, product_id))";
        yield return "CREATE TABLE integ_documents (id INT PRIMARY KEY, title VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_document_details (document_id INT PRIMARY KEY, summary VARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_users (id INT PRIMARY KEY, email VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_user_profiles (id INT PRIMARY KEY, display_name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_gallery_products (id INT PRIMARY KEY, sku VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_product_images (product_id INT NOT NULL, image_url VARCHAR(256) NOT NULL, image_caption VARCHAR(256) NOT NULL)";
        yield return "CREATE TABLE integ_departments (id INT PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_employees (id INT PRIMARY KEY, department_id INT NOT NULL, employee_code VARCHAR(32) NOT NULL, full_name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_parents (id INT PRIMARY KEY, name VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_children (id INT PRIMARY KEY, parent_id INT NOT NULL, label VARCHAR(128) NOT NULL)";
        yield return "CREATE TABLE integ_graph_orders (id INT PRIMARY KEY, code VARCHAR(64) NOT NULL, is_deleted TINYINT(1) NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_graph_order_lines (id INT PRIMARY KEY, order_id INT NOT NULL, sku VARCHAR(64) NOT NULL, is_deleted TINYINT(1) NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_proc_orders (id INT PRIMARY KEY, code VARCHAR(64) NOT NULL)";
        yield return "CREATE TABLE integ_proc_lines (id INT PRIMARY KEY, order_id INT NOT NULL, sku VARCHAR(64) NOT NULL)";
    }

    private static IEnumerable<string> SqliteAdvancedDdl()
    {
        yield return "CREATE TABLE integ_composite_items (order_id INTEGER NOT NULL, product_id INTEGER NOT NULL, quantity INTEGER NOT NULL, PRIMARY KEY (order_id, product_id))";
        yield return "CREATE TABLE integ_documents (id INTEGER PRIMARY KEY, title TEXT NOT NULL)";
        yield return "CREATE TABLE integ_document_details (document_id INTEGER PRIMARY KEY, summary TEXT NOT NULL)";
        yield return "CREATE TABLE integ_users (id INTEGER PRIMARY KEY, email TEXT NOT NULL)";
        yield return "CREATE TABLE integ_user_profiles (id INTEGER PRIMARY KEY, display_name TEXT NOT NULL)";
        yield return "CREATE TABLE integ_gallery_products (id INTEGER PRIMARY KEY, sku TEXT NOT NULL)";
        yield return "CREATE TABLE integ_product_images (product_id INTEGER NOT NULL, image_url TEXT NOT NULL, image_caption TEXT NOT NULL)";
        yield return "CREATE TABLE integ_departments (id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
        yield return "CREATE TABLE integ_employees (id INTEGER PRIMARY KEY, department_id INTEGER NOT NULL, employee_code TEXT NOT NULL, full_name TEXT NOT NULL)";
        yield return "CREATE TABLE integ_graph_parents (id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
        yield return "CREATE TABLE integ_graph_children (id INTEGER PRIMARY KEY, parent_id INTEGER NOT NULL, label TEXT NOT NULL)";
        yield return "CREATE TABLE integ_graph_orders (id INTEGER PRIMARY KEY, code TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0)";
        yield return "CREATE TABLE integ_graph_order_lines (id INTEGER PRIMARY KEY, order_id INTEGER NOT NULL, sku TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0)";
    }
}
