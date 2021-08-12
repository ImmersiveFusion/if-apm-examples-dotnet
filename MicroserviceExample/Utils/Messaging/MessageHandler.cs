using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Utils.Messaging
{
    internal class MessageHandler
    {
        private readonly ILogger _logger;
        private readonly IConnectionMultiplexer _mux;

        public MessageHandler(ILogger logger, IConnectionMultiplexer mux)
        {
            _logger = logger;
            _mux = mux;
        }

        public void Handle(string message)
        {
            _logger.LogInformation($"Message received: [{message}]");


            //sql call
            using (var connection =
                new SqlConnection(
                    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=demo;Integrated Security=SSPI;")
            )
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT TOP (1000) [id],[blah] FROM [test]";

                    cmd.ExecuteNonQuery();
                }
            }

            //redis
            var db = _mux.GetDatabase();

                db.SetAdd("test", 1);
        }
    }
}
