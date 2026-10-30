using Microsoft.Data.SqlClient;

namespace WebQLTB.Models;

public static class SqlErrorMessage
{
    // Stored procedures use user-defined SQL numbers for business-rule messages.
    // Driver/engine errors (for example SQL Server 823) should not expose paths or internals.
    public static string ForUser(SqlException exception, string fallback) =>
        exception.Number >= 50000 ? exception.Message : fallback;
}
