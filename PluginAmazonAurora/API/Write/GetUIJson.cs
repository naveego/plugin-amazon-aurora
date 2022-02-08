using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginAmazonAurora.API.Write
{
    public static partial class Write
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "StoredProcedure",
                    "GoldenRecordIdParam"
                }}
            };
            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}