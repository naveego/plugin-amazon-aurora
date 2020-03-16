using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginMySQL.API.Factory;
using PluginMySQL.DataContracts;

namespace PluginMySQL.API.Replication
{
    public static partial class Replication
    {
        private static readonly string DropTableQuery = @"DROP TABLE IF EXISTS @schema.@table";
        
        public static async Task DropTableAsync(IConnectionFactory connFactory, ReplicationTable table)
        {
            var conn = connFactory.GetConnection();
            await conn.OpenAsync();

            var cmd = connFactory.GetCommand(DropTableQuery, conn);
            cmd.AddParameter("@schema", table.SchemaName);
            cmd.AddParameter("@table", table.TableName);

            await cmd.ExecuteNonQueryAsync();
            
            await conn.CloseAsync();
        }
    }
}