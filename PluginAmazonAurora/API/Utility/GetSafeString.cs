namespace PluginAmazonAurora.API.Utility
{
    public static partial class Utility
    {
        public static string GetSafeString(string unsafeString, string escapeChar = "\\", string newValue = "\\\\")
        {
            return unsafeString.Replace(escapeChar, newValue);
        }
        
        public static string GetSafeString(string unsafeString, params (string escapeChar, string newValue)[] replacePairs)
        {
            var result = (string)unsafeString.Clone();
            foreach (var pair in replacePairs)
            {
                result = result.Replace(pair.escapeChar, pair.newValue);
            }

            return result;
        }
    }
}