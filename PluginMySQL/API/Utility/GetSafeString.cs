namespace PluginMySQL.API.Utility
{
    public static partial class Utility
    {
        public static string GetSafeString(string unsafeString, string escapeChar = "\\")
        {
            return unsafeString.Replace(escapeChar, "\\\\");
        }
    }
}