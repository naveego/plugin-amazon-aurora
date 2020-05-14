using System.Collections.Generic;
using System.Threading.Tasks;
using PluginMySQL.API.Factory;
using PluginMySQL.DataContracts;

namespace PluginMySQL.API.Write
{
    public static partial class Write
    {
        private const string SchemaName = "ROUTINE_SCHEMA";
        private const string RoutineName = "ROUTINE_NAME";
        private const string SpecificName = "SPECIFIC_NAME";

        private static string GetAllStoredProceduresQuery = @"
SELECT ROUTINE_SCHEMA, ROUTINE_NAME, SPECIFIC_NAME
FROM INFORMATION_SCHEMA.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA != 'sys'";
        
        public static async Task<List<WriteStoredProcedure>> GetAllStoredProceduresAsync(IConnectionFactory connFactory)
        {
            var storedProcedures = new List<WriteStoredProcedure>();
            var conn = connFactory.GetConnection();
            await conn.OpenAsync();

            var cmd = connFactory.GetCommand(GetAllStoredProceduresQuery, conn);
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var storedProcedure = new WriteStoredProcedure
                {
                    SchemaName = reader.GetValueById(SchemaName).ToString(),
                    RoutineName = reader.GetValueById(RoutineName).ToString(),
                    SpecificName = reader.GetValueById(SpecificName).ToString()
                };
                
                storedProcedures.Add(storedProcedure);
            }
            
            await conn.CloseAsync();

            return storedProcedures;
        }
    }
}