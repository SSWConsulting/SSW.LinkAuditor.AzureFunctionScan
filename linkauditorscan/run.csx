#r "SSW.LinkAuditor.BusinessImplementation.dll"
#r "SSW.LinkAuditor.BusinessImplementation.Resources.dll"
#r "SSW.LinkAuditor.Common.dll"
#r "SSW.LinkAuditor.BusinessInterfaces.dll"
#r "SSW.LinkAuditor.Domain.dll"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "SSW.CodeAuditor.RuleEngine.dll"
#r "log4net.dll"
#r "Dapper.dll"
#r "System.Data"
#r "SSW.LinkAuditor.Engine.RepositoryInterfaces.dll"
using SSW.LinkAuditor.BusinessImplementation;
using SSW.LinkAuditor.Domain;
using SSW.LinkAuditor.Common;
using Newtonsoft.Json;
using System.Threading.Tasks;


        public class AzureFunctionLogger : ILogger
        {
            private TraceWriter _logger;
            private string _executionId;

            public AzureFunctionLogger(string executionId, TraceWriter logger)
            {
                _logger = logger;
                _executionId = executionId;
            }

            public void Info(string log)
            {
                _logger.Info("[" + _executionId + "] " + log);
            }

            public void Trace(Exception ex, TracingLevels level, string messageTemplate, params object[] properties)
            {
                _logger.Info("[" + _executionId + "] " + string.Format(messageTemplate, properties));
                if (ex != null)
                {
                    _logger.Info(ex.StackTrace);
                }
            }
        }

        public static async Task Run(string myQueueItem, ExecutionContext exCtx, TraceWriter log)
        {
            var scanSqlConn = System.Configuration.ConfigurationManager.AppSettings["ScansSqlConnString"];
            var userSqlConn = System.Configuration.ConfigurationManager.AppSettings["UserSqlConnString"];
            var storageConn = System.Configuration.ConfigurationManager.AppSettings["LinkAuditorStorage"];

            var azureScanner = new AzureFunctionScan(
                                        new AzureFunctionConfigManager("scanrequestqueue", "scannedurls",
                                        userSqlConn, scanSqlConn, storageConn),
                                        new Persistence(scanSqlConn),
                                        new StringResourceManager(),
                                        exCtx.InvocationId,
                                        new AzureFunctionLogger(exCtx.InvocationId.ToString(), log)
                                    );
            await azureScanner.ProcessScan(JsonConvert.DeserializeObject<PageScanRequest>(myQueueItem));
        }