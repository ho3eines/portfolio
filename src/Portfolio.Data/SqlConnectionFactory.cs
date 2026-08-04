using Microsoft.Data.SqlClient;
using System.Data;

namespace Portfolio.Data;

public class SqlConnectionFactory
{
    private readonly string _connStr;
    public SqlConnectionFactory(string connStr) => _connStr = connStr;
    public IDbConnection Create() => new SqlConnection(_connStr);
}
