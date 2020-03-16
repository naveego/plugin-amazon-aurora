using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace PluginMySQL.API.Factory
{
    public class Command : ICommand
    {
        private readonly MySqlCommand _cmd;

        public Command()
        {
            _cmd = new MySqlCommand();
        }

        public Command(string commandText)
        {
            _cmd = new MySqlCommand(commandText);
        }

        public Command(string commandText, IConnection conn)
        {
            _cmd = new MySqlCommand(commandText, (MySqlConnection) conn.GetConnection());
        }

        public void SetConnection(IConnection conn)
        {
            _cmd.Connection = (MySqlConnection) conn.GetConnection();
        }

        public void SetCommandText(string commandText)
        {
            _cmd.CommandText = commandText;
        }

        public void AddParameter(string name, object value)
        {
            _cmd.Parameters.AddWithValue(name, value);
        }

        public async Task<IReader> ExecuteReaderAsync()
        {
            return new Reader(await _cmd.ExecuteReaderAsync());
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            return await _cmd.ExecuteNonQueryAsync();
        }
    }
}