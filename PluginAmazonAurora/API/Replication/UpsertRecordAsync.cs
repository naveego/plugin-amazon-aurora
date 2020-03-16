using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginAmazonAurora.API.Factory;
using PluginAmazonAurora.DataContracts;
using PluginAmazonAurora.Helper;

namespace PluginAmazonAurora.API.Replication
{
    public static partial class Replication
    {
        public static async Task UpsertRecordAsync(IConnectionFactory connFactory,
            ReplicationTable table,
            Dictionary<string, object> recordMap)
        {
            var conn = connFactory.GetConnection();
            await conn.OpenAsync();

            try
            {
                // try to insert
                var querySb =
                    new StringBuilder(
                        $"INSERT INTO {Utility.Utility.GetSafeName(table.SchemaName, '`')}.{Utility.Utility.GetSafeName(table.TableName, '`')}(");
                foreach (var column in table.Columns)
                {
                    querySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName, '`')},");
                }

                querySb.Length--;
                querySb.Append(") VALUES (");

                foreach (var column in table.Columns)
                {
                    if (recordMap.ContainsKey(column.ColumnName))
                    {
                        var rawValue = recordMap[column.ColumnName];
                        if (column.Serialize)
                        {
                            rawValue = JsonConvert.SerializeObject(rawValue);
                        }
                        querySb.Append($"'{rawValue}',");
                    }
                    else
                    {
                        querySb.Append($"{null},");
                    }
                }

                querySb.Length--;
                querySb.Append(");");

                var query = querySb.ToString();

                Logger.Debug($"Insert record query: {query}");

                var cmd = connFactory.GetCommand(query, conn);
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                try
                {
                    // update if it failed
                    var querySb =
                        new StringBuilder(
                            $"UPDATE {Utility.Utility.GetSafeName(table.SchemaName, '`')}.{Utility.Utility.GetSafeName(table.TableName, '`')} SET ");
                    foreach (var column in table.Columns)
                    {
                        if (!column.PrimaryKey)
                        {
                            var rawValue = recordMap[column.ColumnName];
                            if (column.Serialize)
                            {
                                rawValue = JsonConvert.SerializeObject(rawValue);
                            }
                            querySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName, '`')}='{rawValue}',");
                        }
                    }

                    querySb.Length--;
                    
                    var primaryKey = table.Columns.Find(c => c.PrimaryKey);
                    var primaryValue = recordMap[primaryKey.ColumnName];
                    if (primaryKey.Serialize)
                    {
                        primaryValue = JsonConvert.SerializeObject(primaryValue);
                    }
                    
                    querySb.Append($" WHERE {primaryKey.ColumnName} = '{primaryValue}'");

                    var query = querySb.ToString();
                    
                    var cmd = connFactory.GetCommand(query, conn);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception exception)
                {
                    Logger.Error($"Error Insert: {e.Message}");
                    Logger.Error($"Error Update: {exception.Message}");
                    throw;
                }
            }

            await conn.CloseAsync();
        }
    }
}