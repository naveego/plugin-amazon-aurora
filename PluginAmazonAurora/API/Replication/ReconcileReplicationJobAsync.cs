using System;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginAmazonAurora.API.Factory;
using PluginAmazonAurora.DataContracts;
using PluginAmazonAurora.Helper;
using Constants = PluginAmazonAurora.API.Utility.Constants;

namespace PluginAmazonAurora.API.Replication
{
    public static partial class Replication
    {
        private const string SchemaNameChange = "Schema name changed";
        private const string GoldenNameChange = "Golden record name changed";
        private const string VersionNameChange = "Version name changed";
        private const string JobDataVersionChange = "Job data version changed";
        private const string ShapeDataVersionChange = "Shape data version changed";
        
        public static async Task ReconcileReplicationJobAsync(IConnectionFactory connFactory, PrepareWriteRequest request)
        {
            // get request settings 
            var replicationSettings =
                JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Replication.SettingsJson);
            var safeSchemaName = replicationSettings.SchemaName;
            var safeGoldenTableName =
                replicationSettings.GoldenTableName;
            var safeVersionTableName =
                replicationSettings.VersionTableName;

            var metaDataTable = new ReplicationTable
            {
                SchemaName = safeSchemaName,
                TableName = Constants.ReplicationMetaDataTableName,
                Columns = Constants.ReplicationMetaDataColumns
            };

            var goldenTable = GetGoldenReplicationTable(request.Schema, safeSchemaName, safeGoldenTableName);
            var versionTable = GetVersionReplicationTable(request.Schema, safeSchemaName, safeVersionTableName);

            Logger.Info(
                $"SchemaName: {safeSchemaName} Golden Table: {safeGoldenTableName} Version Table: {safeVersionTableName} job: {request.DataVersions.JobId}");

            // get previous metadata
            Logger.Info($"Getting previous metadata job: {request.DataVersions.JobId}");
            var previousMetaData = await GetPreviousReplicationMetaDataAsync(connFactory, request.DataVersions.JobId, metaDataTable);
            Logger.Info($"Got previous metadata job: {request.DataVersions.JobId}");

            // create current metadata
            Logger.Info($"Generating current metadata job: {request.DataVersions.JobId}");
            var metaData = new ReplicationMetaData
            {
                ReplicatedShapeId = request.Schema.Id,
                ReplicatedShapeName = request.Schema.Name,
                Timestamp = DateTime.Now,
                Request = request
            };
            Logger.Info($"Generated current metadata job: {request.DataVersions.JobId}");

            // check if changes are needed
            if (previousMetaData == null)
            {
                Logger.Info($"No Previous metadata creating buckets job: {request.DataVersions.JobId}");
                await EnsureTableAsync(connFactory, goldenTable);
                await EnsureTableAsync(connFactory, versionTable);
                Logger.Info($"Created buckets job: {request.DataVersions.JobId}");
            }
            else
            {
                var dropGoldenReason = "";
                var dropVersionReason = "";
                var previousReplicationSettings =
                    JsonConvert.DeserializeObject<ConfigureReplicationFormData>(previousMetaData.Request.Replication
                        .SettingsJson);
                
                var previousGoldenTable = ConvertSchemaToReplicationTable(previousMetaData.Request.Schema, previousReplicationSettings.SchemaName, previousReplicationSettings.GoldenTableName);
                goldenTable.Columns.Add(new ReplicationColumn
                {
                    ColumnName = Constants.ReplicationRecordId,
                    DataType = "varchar(255)",
                    PrimaryKey = true
                });

                var previousVersionTable = ConvertSchemaToReplicationTable(previousMetaData.Request.Schema, previousReplicationSettings.SchemaName, previousReplicationSettings.VersionTableName);
                versionTable.Columns.Add(new ReplicationColumn
                {
                    ColumnName = Constants.ReplicationRecordId,
                    DataType = "varchar(255)",
                    PrimaryKey = true
                });
                versionTable.Columns.Add(new ReplicationColumn
                {
                    ColumnName = Constants.ReplicationVersionRecordId,
                    DataType = "varchar(255)",
                    PrimaryKey = true
                });
                
                // check if schema changed
                if (previousReplicationSettings.SchemaName != replicationSettings.SchemaName)
                {
                    dropGoldenReason = SchemaNameChange;
                    dropVersionReason = SchemaNameChange;
                }

                // check if golden bucket name changed
                if (previousReplicationSettings.GoldenTableName != replicationSettings.GoldenTableName)
                {
                    dropGoldenReason = GoldenNameChange;
                }

                // check if version bucket name changed
                if (previousReplicationSettings.VersionTableName != replicationSettings.VersionTableName)
                {
                    dropVersionReason = VersionNameChange;
                }

                // check if job data version changed
                if (metaData.Request.DataVersions.JobDataVersion > previousMetaData.Request.DataVersions.JobDataVersion)
                {
                    dropGoldenReason = JobDataVersionChange;
                    dropVersionReason = JobDataVersionChange;
                }

                // check if shape data version changed
                if (metaData.Request.DataVersions.ShapeDataVersion >
                    previousMetaData.Request.DataVersions.ShapeDataVersion)
                {
                    dropGoldenReason = ShapeDataVersionChange;
                    dropVersionReason = ShapeDataVersionChange;
                }

                // drop previous golden bucket
                if (dropGoldenReason != "")
                {
                    var safePreviousSchemaName = Utility.Utility.GetSafeName(previousReplicationSettings.SchemaName, '`');
                    var safePreviousGoldenBucketName =
                        Utility.Utility.GetSafeName(previousReplicationSettings.GoldenTableName, '`');

                    await DropTableAsync(connFactory, previousGoldenTable);

                    await EnsureTableAsync(connFactory, goldenTable);
                }

                // drop previous version bucket
                if (dropVersionReason != "")
                {
                    var safePreviousSchemaName = Utility.Utility.GetSafeName(previousReplicationSettings.SchemaName, '`');
                    var safePreviousGoldenBucketName =
                        Utility.Utility.GetSafeName(previousReplicationSettings.VersionTableName, '`');

                    await DropTableAsync(connFactory, previousVersionTable);

                    await EnsureTableAsync(connFactory, versionTable);
                }
            }

            // save new metadata
            Logger.Info($"Updating metadata job: {request.DataVersions.JobId}");
            await UpsertReplicationMetaDataAsync(connFactory, metaDataTable, metaData);
            Logger.Info($"Updated metadata job: {request.DataVersions.JobId}");
        }
    }
}