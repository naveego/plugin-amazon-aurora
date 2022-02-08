using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PluginAmazonAurora.DataContracts;

namespace PluginAmazonAurora.API.Write
{
    public static partial class Write
    {
        public static string GetSchemaJson(List<WriteStoredProcedure> storedProcedures)
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"StoredProcedure", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Stored Procedure"},
                        {"description", "Stored Procedure to call"},
                        {"enum", storedProcedures.Select(s => s.GetId())}
                    }},
                    {"GoldenRecordIdParam", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Golden Record Id Parameter"},
                        {"description", "(Optional) Name of the parameter to map the Golden Record Id to as an input."},
                    }},
                }},
                {"required", new []
                {
                    "StoredProcedure"
                }}
            };

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}