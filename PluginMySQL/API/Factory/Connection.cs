using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using PluginMySQL.Helper;

namespace PluginMySQL.API.Factory
{
    public class Connection : IConnection
    {
        private readonly MySqlConnection _conn;

        public Connection(Settings settings)
        {
            _conn = new MySqlConnection(settings.GetConnectionString());
        }

        public Connection(Settings settings, string database)
        {
            _conn = new MySqlConnection(settings.GetConnectionString(database));
        }

        public async Task OpenAsync()
        {
            await _conn.OpenAsync();
        }

        public async Task CloseAsync()
        {
            await _conn.CloseAsync();
        }

        public async Task<bool> PingAsync()
        {
            return await _conn.PingAsync();
        }

        public IDbConnection GetConnection()
        {
            return _conn;
        }
    }
}