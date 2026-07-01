using System.Data;
using Dapper;

namespace DapperX.IntegrationTests.Shared;

public static class IntegrationProcedureBootstrap
{
    public static async Task CreateProceduresAsync(IDbConnection connection, string provider)
    {
        foreach (var sql in GetProcedureScripts(provider))
            await connection.ExecuteAsync(sql);
    }

    private static IEnumerable<string> GetProcedureScripts(string provider) => provider switch
    {
        "PostgreSql" => PostgreSqlProcedures(),
        "MySql" => MySqlProcedures(),
        _ => SqlServerProcedures(),
    };

    private static IEnumerable<string> SqlServerProcedures()
    {
        yield return """
            CREATE OR ALTER PROCEDURE sp_integ_list_proc_orders @customerId INT AS
            SELECT id, code FROM integ_proc_orders
            """;
        yield return """
            CREATE OR ALTER PROCEDURE sp_integ_process_proc_order
              @orderId INT,
              @total DECIMAL(18,2) OUTPUT,
              @resultCode INT OUTPUT,
              @message NVARCHAR(4000) OUTPUT
            AS
            BEGIN
              SET @resultCode = 0;
              SET @message = N'ok';
              SET @total = @total + 1;
            END
            """;
        yield return """
            CREATE OR ALTER PROCEDURE sp_integ_proc_order_report @orderId INT AS
            BEGIN
              SELECT id, code FROM integ_proc_orders WHERE id = @orderId;
              SELECT id, sku FROM integ_proc_lines WHERE order_id = @orderId;
            END
            """;
    }

    private static IEnumerable<string> MySqlProcedures()
    {
        yield return "DROP PROCEDURE IF EXISTS sp_integ_list_proc_orders";
        yield return "DROP PROCEDURE IF EXISTS sp_integ_process_proc_order";
        yield return "DROP PROCEDURE IF EXISTS sp_integ_proc_order_report";
        yield return """
            CREATE PROCEDURE sp_integ_list_proc_orders(IN customerId INT)
            BEGIN
              SELECT id, code FROM integ_proc_orders;
            END
            """;
        yield return """
            CREATE PROCEDURE sp_integ_process_proc_order(
              IN orderId INT,
              INOUT total DECIMAL(18,2),
              OUT resultCode INT,
              OUT message VARCHAR(4000))
            BEGIN
              SET resultCode = 0;
              SET message = 'ok';
              SET total = total + 1;
            END
            """;
        yield return """
            CREATE PROCEDURE sp_integ_proc_order_report(IN orderId INT)
            BEGIN
              SELECT id, code FROM integ_proc_orders WHERE id = orderId;
              SELECT id, sku FROM integ_proc_lines WHERE order_id = orderId;
            END
            """;
    }

    private static IEnumerable<string> PostgreSqlProcedures()
    {
        yield return """
            CREATE OR REPLACE FUNCTION sp_integ_list_proc_orders(p_customerid integer)
            RETURNS TABLE(id integer, code character varying)
            LANGUAGE sql
            AS $$
              SELECT o.id, o.code FROM integ_proc_orders o;
            $$
            """;
        yield return """
            CREATE OR REPLACE PROCEDURE sp_integ_process_proc_order(
              IN p_orderid integer,
              INOUT p_total numeric,
              OUT p_resultcode integer,
              OUT p_message text)
            LANGUAGE plpgsql
            AS $$
            BEGIN
              p_resultcode := 0;
              p_message := 'ok';
              p_total := p_total + 1;
            END;
            $$
            """;
    }
}
