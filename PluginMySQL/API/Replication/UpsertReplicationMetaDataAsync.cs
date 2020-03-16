using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginMySQL.API.Factory;
using PluginMySQL.API.Utility;
using PluginMySQL.DataContracts;
using PluginMySQL.Helper;

namespace PluginMySQL.API.Replication
{
    public static partial class Replication
    {
        private static readonly string InsertMetaDataQuery = $@"INSERT INTO {{0}}.{{1}} 
(
{Constants.ReplicationMetaDataJobId}
, {Constants.ReplicationMetaDataRequest}
, {Constants.ReplicationMetaDataReplicatedShapeId}
, {Constants.ReplicationMetaDataReplicatedShapeName}
, {Constants.ReplicationMetaDataTimestamp})
VALUES (
'{{2}}'
, '{{3}}'
, '{{4}}'
, '{{5}}'
, '{{6}}'
)";
        
        private static readonly string UpdateMetaDataQuery = $@"UPDATE {{0}}.{{1}}
SET 
{Constants.ReplicationMetaDataRequest} = '{{2}}'
, {Constants.ReplicationMetaDataReplicatedShapeId} = '{{3}}'
, {Constants.ReplicationMetaDataReplicatedShapeName} = '{{4}}'
, {Constants.ReplicationMetaDataTimestamp} = '{{5}}'
WHERE {Constants.ReplicationMetaDataJobId} = '{{6}}'";
        
        public static async Task UpsertReplicationMetaDataAsync(IConnectionFactory connFactory, ReplicationTable table, ReplicationMetaData metaData)
        {
            var conn = connFactory.GetConnection();
            await conn.OpenAsync();
            
            try
            {
                // try to insert
                var cmd = connFactory.GetCommand(
                    string.Format(InsertMetaDataQuery, 
                        Utility.Utility.GetSafeName(table.SchemaName, '`'),
                        Utility.Utility.GetSafeName(table.TableName, '`'), 
                        metaData.Request.DataVersions.JobId,
                        JsonConvert.SerializeObject(metaData.Request).Replace("\\", "\\\\"),
                        metaData.ReplicatedShapeId,
                        metaData.ReplicatedShapeName,
                        metaData.Timestamp
                        ),
                    conn);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                try
                {
                    // update if it failed
                    var cmd = connFactory.GetCommand(
                        string.Format(UpdateMetaDataQuery, 
                            Utility.Utility.GetSafeName(table.SchemaName, '`'),
                            Utility.Utility.GetSafeName(table.TableName, '`'),
                            JsonConvert.SerializeObject(metaData.Request).Replace("\\", "\\\\"),
                            metaData.ReplicatedShapeId,
                            metaData.ReplicatedShapeName,
                            metaData.Timestamp,
                            metaData.Request.DataVersions.JobId
                        ),
                        conn);
                
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