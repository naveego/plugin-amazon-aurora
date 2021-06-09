using PluginAmazonAurora.Helper;
using System.Data;

namespace PluginAmazonAurora.API.Factory
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