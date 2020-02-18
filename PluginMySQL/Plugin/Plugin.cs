using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using MySql.Data.MySqlClient;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginMySQL.API.Discover;
using PluginMySQL.API.Factory;
using PluginMySQL.Helper;

namespace PluginMySQL.Plugin
{
    public class Plugin : Publisher.PublisherBase
    {
        private readonly ServerStatus _server;
        private TaskCompletionSource<bool> _tcs;
        private IConnectionFactory _connectionFactory;

        public Plugin(IConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new ConnectionFactory();
            _server = new ServerStatus
            {
                Connected = false,
                WriteConfigured = false
            };
        }

        /// <summary>
        /// Establishes a connection with MySQL.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>A message indicating connection success</returns>
        public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            Logger.SetLogPrefix("connect");
            // validate settings passed in
            try
            {
                _server.Settings = JsonConvert.DeserializeObject<Settings>(request.SettingsJson);
                _server.Settings.Validate();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                };
            }

            // initialize connection factory
            try
            {
                _connectionFactory.Initialize(_server.Settings);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

            // test cluster factory
            try
            {
                var conn =  _connectionFactory.GetConnection();
                await conn.OpenAsync();

                if (!await conn.PingAsync())
                {
                    return new ConnectResponse
                    {
                        OauthStateJson = request.OauthStateJson,
                        ConnectionError = "Unable to ping target database.",
                        OauthError = "",
                        SettingsError = ""
                    };
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);

                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = e.Message,
                    OauthError = "",
                    SettingsError = ""
                };
            }

            return new ConnectResponse
            {
                OauthStateJson = request.OauthStateJson,
                ConnectionError = "",
                OauthError = "",
                SettingsError = ""
            };
        }

        public override async Task ConnectSession(ConnectRequest request,
            IServerStreamWriter<ConnectResponse> responseStream, ServerCallContext context)
        {
            Logger.SetLogPrefix("connect_session");
            Logger.Info("Connecting session...");

            // create task to wait for disconnect to be called
            _tcs?.SetResult(true);
            _tcs = new TaskCompletionSource<bool>();

            // call connect method
            var response = await Connect(request, context);

            await responseStream.WriteAsync(response);

            Logger.Info("Session connected.");

            // wait for disconnect to be called
            await _tcs.Task;
        }


        /// <summary>
        /// Discovers schemas located in the users Zoho CRM instance
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix("discover");
            Logger.Info("Discovering Schemas...");

            var sampleSize = checked((int) request.SampleSize);

            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();

            // only return requested schemas if refresh mode selected
            if (request.Mode == DiscoverSchemasRequest.Types.Mode.All)
            {
                // get all schemas
                try
                {
                    var schemas = Discover.GetAllSchemas(_connectionFactory);

                    discoverSchemasResponse.Schemas.AddRange(await schemas.ToListAsync());

                    Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");

                    return discoverSchemasResponse;
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    throw;
                }
            }

            try
            {
                var refreshSchemas = request.ToRefresh;

                Logger.Info($"Refresh schemas attempted: {refreshSchemas.Count}");

                var schemas = Discover.GetRefreshSchemas(_connectionFactory, refreshSchemas);

                discoverSchemasResponse.Schemas.AddRange(await schemas.ToListAsync());

                // return all schemas 
                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
                return discoverSchemasResponse;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Publishes a stream of data for a given schema
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ReadStream(ReadRequest request, IServerStreamWriter<Record> responseStream,
            ServerCallContext context)
        {
            Logger.SetLogPrefix(request.DataVersions.JobId);
            //Read.GetAllRecords()
        }

        /// <summary>
        /// Configures replication writebacks to MySQL
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureReplicationResponse> ConfigureReplication(ConfigureReplicationRequest request,
            ServerCallContext context)
        {
            // Logger.Info("Configuring write...");
            //
            // var schemaJson = Replication.GetSchemaJson();
            // var uiJson = Replication.GetUIJson();
            //
            // try
            // {
            //     var errors = new List<string>();
            //     // if (! string.IsNullOrWhiteSpace(request.Form.DataJson))
            //     // {
            //     //     // check for config errors
            //     //     var replicationFormData = JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Form.DataJson);
            //     //
            //     //     errors = Replication.ValidateReplicationFormData(replicationFormData);
            //     // }
            //     //
            //     return Task.FromResult(new ConfigureReplicationResponse
            //     {
            //         Form = new ConfigurationFormResponse
            //         {
            //             DataJson = request.Form.DataJson,
            //             Errors = {errors},
            //             SchemaJson = schemaJson,
            //             UiJson = uiJson,
            //             StateJson = request.Form.StateJson
            //         }
            //     });
            // }
            // catch (Exception e)
            // {
            //     Logger.Error(e.Message);
            //     return Task.FromResult(new ConfigureReplicationResponse
            //     {
            //         Form = new ConfigurationFormResponse
            //         {
            //             DataJson = request.Form.DataJson,
            //             Errors = {e.Message},
            //             SchemaJson = schemaJson,
            //             UiJson = uiJson,
            //             StateJson = request.Form.StateJson
            //         }
            //     });
            // }
            
            return Task.FromResult(new ConfigureReplicationResponse());
        }

        /// <summary>
        /// Prepares writeback settings to write to MySQL
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<PrepareWriteResponse> PrepareWrite(PrepareWriteRequest request, ServerCallContext context)
        {
            // Logger.SetLogPrefix(request.DataVersions.JobId);
            // Logger.Info("Preparing write...");
            // _server.WriteConfigured = false;
            //
            // _server.WriteSettings = new WriteSettings
            // {
            //     CommitSLA = request.CommitSlaSeconds,
            //     Schema = request.Schema,
            //     Replication = request.Replication,
            //     DataVersions = request.DataVersions,
            // };
            //
            // if (_server.WriteSettings.IsReplication())
            // {
            //     // reconcile job
            //     Logger.Info($"Starting to reconcile Replication Job {request.DataVersions.JobId}");
            //     await Replication.ReconcileReplicationJob(_connectionFactory, request);
            //     Logger.Info($"Finished reconciling Replication Job {request.DataVersions.JobId}");
            // }
            //
            // _server.WriteConfigured = true;
            //
            // Logger.Debug(JsonConvert.SerializeObject(_server.WriteSettings, Formatting.Indented));
            // Logger.Info("Write prepared.");
            return new PrepareWriteResponse();
        }

        /// <summary>
        /// Writes records to MySQL
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task WriteStream(IAsyncStreamReader<Record> requestStream,
            IServerStreamWriter<RecordAck> responseStream, ServerCallContext context)
        {
            // try
            // {
            //     Logger.Info("Writing records to MySQL...");
            //
            //     var schema = _server.WriteSettings.Schema;
            //     var inCount = 0;
            //     var config =
            //         JsonConvert.DeserializeObject<ConfigureReplicationFormData>(_server.WriteSettings.Replication
            //             .SettingsJson);
            //
            //     // get next record to publish while connected and configured
            //     while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
            //            _server.WriteConfigured)
            //     {
            //         var record = requestStream.Current;
            //         inCount++;
            //
            //         Logger.Debug($"Got record: {record.DataJson}");
            //
            //         if (_server.WriteSettings.IsReplication())
            //         {
            //             
            //             // send record to source system
            //             // timeout if it takes longer than the sla
            //             Task.Run(async () => await Replication.WriteRecord(_connectionFactory, schema, record, config, responseStream), context.CancellationToken);
            //         }
            //         else
            //         {
            //             throw new Exception("Only replication writebacks are supported");
            //         }
            //     }
            //
            //     Logger.Info($"Wrote {inCount} records to MySQL.");
            // }
            // catch (Exception e)
            // {
            //     Logger.Error(e.Message);
            //     throw;
            // }
        }

        /// <summary>
        /// Handles disconnect requests from the agent
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
        {
            // clear connection
            _server.Connected = false;
            _server.Settings = null;

            // alert connection session to close
            if (_tcs != null)
            {
                _tcs.SetResult(true);
                _tcs = null;
            }

            Logger.Info("Disconnected");
            return Task.FromResult(new DisconnectResponse());
        }
    }
}