using System.Data;
using MySql.Data.MySqlClient;
using PluginMySQL.Helper;

namespace PluginMySQL.API.Factory
{
    public class ConnectionFactory : IConnectionFactory
    {
        private Settings _settings;

        public void Initialize(Settings settings)
        {
            _settings = settings;
        }

        public IConnection GetConnection()
        {
            return new Connection(_settings);
        }

        public IConnection GetConnection(string database)
        {
            return new Connection(_settings, database);
        }

        public ICommand GetCommand(string commandText, IConnection connection)
        {
            return new Command(commandText, connection);
        }
    }
}