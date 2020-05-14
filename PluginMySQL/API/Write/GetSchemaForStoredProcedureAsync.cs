using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginMySQL.API.Factory;
using PluginMySQL.DataContracts;

namespace PluginMySQL.API.Write
{
    public static partial class Write
    {
        private static string ParamName = "PARAMETER_NAME";
        private static string DataType = "DATA_TYPE";

        private static string GetStoredProcedureParamsQuery = @"
SELECT PARAMETER_NAME, DATA_TYPE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.PARAMETERS
WHERE SPECIFIC_SCHEMA = '{0}'
AND SPECIFIC_NAME = '{1}'
ORDER BY ORDINAL_POSITION ASC";

        public static async Task<Schema> GetSchemaForStoredProcedureAsync(IConnectionFactory connFactory,
            WriteStoredProcedure storedProcedure)
        {
            var schema = new Schema
            {
                Id = storedProcedure.GetId(),
                Name = storedProcedure.GetId(),
                Description = "",
                DataFlowDirection = Schema.Types.DataFlowDirection.Write,
                Query = storedProcedure.GetId()
            };

            var conn = connFactory.GetConnection();
            await conn.OpenAsync();

            var cmd = connFactory.GetCommand(
                string.Format(GetStoredProcedureParamsQuery, storedProcedure.SchemaName, storedProcedure.SpecificName),
                conn);
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var property = new Property
                {
                    Id = reader.GetValueById(ParamName).ToString(),
                    Name = reader.GetValueById(ParamName).ToString(),
                    Description = "",
                    Type = Discover.Discover.GetType(reader.GetValueById(DataType).ToString()),
                    TypeAtSource = reader.GetValueById(DataType).ToString()
                };

                schema.Properties.Add(property);
            }

            await conn.CloseAsync();

            return schema;
        }
    }
}