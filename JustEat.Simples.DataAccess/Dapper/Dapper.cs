using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using NLog;

namespace JustEat.Simples.DataAccess.Dapper
{
    public class Dapper : IDapper
    {
        private int CommandTimeout { get; set; }
        private readonly ISqlMonitoringService _monitoringService;
        private readonly IDatabaseConfiguration _dbConfig;
        private readonly static Logger Logger = LogManager.GetCurrentClassLogger();

        public Dapper(ISqlMonitoringService monitoringService, IDatabaseConfiguration databaseConfiguration)
        {
            _monitoringService = monitoringService;
            _dbConfig = databaseConfiguration;
        }

        protected Dapper()
        {
            CommandTimeout = 1;
        }

        protected virtual IDbConnection CreateConnection()
        {
            var conn = new SqlConnection(_dbConfig.GetConnectionString());
            conn.Open();
            return conn;
        }

        public IEnumerable<T> Query<T>(string sql, dynamic parameters)
        {
            try
            {
                using (var conn = CreateConnection())
                {
                    return conn.Query<T>(sql, parameters as object, commandTimeout: CommandTimeout);
                }
            }
            catch (Exception ex)
            {
                _monitoringService.SqlException();
                Logger.ErrorException(string.Format("Query Exception: {0} , {1}", sql, parameters.ToString()), ex);
                throw;
            }
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> QueryMultiple<T1, T2>(string sql, dynamic parameters)
        {
            List<dynamic> rtn1;
            List<dynamic> rtn2;
            try
            {
            using (var conn = CreateConnection())
            {
                var rdr = conn.QueryMultiple(sql, parameters as object, commandTimeout: CommandTimeout);
                rtn1 = rdr.Read().ToList();
                rtn2 = rdr.Read().ToList();
            }
            }
            catch (Exception ex)
            {
                _monitoringService.SqlException();
                Logger.ErrorException(string.Format("Query Exception: {0} , {1}", sql, parameters.ToString()), ex);
                throw;
            }

            return Tuple.Create((IEnumerable<T1>)rtn1.AsEnumerable(), (IEnumerable<T2>)rtn2.AsEnumerable());
        }

        private int ExecuteInner(string sql, dynamic parameters)
        {
            using (var conn = CreateConnection())
            {
                return conn.Execute(sql, parameters as object);
            }
        }

        public int Execute(string sql, dynamic parameters)
        {
            return DeadlockRetryExecute(sql, parameters, 0);
        }

        public int DeadlockRetryExecute(string sql, dynamic parameters, int retryTimes)
        {
            if (retryTimes < 0)
            {
                throw new ArgumentException("Value must be greater than zero.", "retryTimes");
            }

            try
            {
                return ExecuteInner(sql, parameters);
            }
            catch (SqlException sex)
            {
                var isDeadlockException = sex.Number == 1205;
                
                if(!isDeadlockException)
                {
                    _monitoringService.SqlException();
                    Logger.ErrorException(string.Format("Execute Exception: {0} , {1}", sql, parameters.ToString()), sex);
                    throw;
                }

                _monitoringService.SqlDeadlockException();

                if (retryTimes > 0)
                {
                    Logger.WarnException("SQL deadlock occured. Retrying.", sex);
                }
                else
                {
                    Logger.ErrorException("SQL deadlock occured. Not Retrying.", sex);
                    throw;
                }
            }

            return DeadlockRetryExecute(sql, parameters, retryTimes - 1);
        }
    }
}