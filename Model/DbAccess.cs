using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MessageBroker.Model
{
    public interface IDbAccess
    {
        Task InsertMessage(StoreMessage message);
        Task<MessagesMetrics> GetMetrics(string recipient, int id, int count);
        Task<StoreMessage[]> GetMessages(string recipient, int id, int count);
    }

    public sealed class MessagesMetrics
    {
        public int Count { get; set; }
        public int DataLength { get; set; }
    }

    public sealed class DbAccess : IDbAccess
    {
        private readonly string _connectionString;

        public DbAccess(IConfiguration con)
        {
            _connectionString = con["MessageBroker:SqlConnectionString"];
        }

        public async Task InsertMessage(StoreMessage message)
        {
            using (var con = new SqlConnection())
            {
                con.ConnectionString = _connectionString;
                await con.OpenAsync();
                var command = con.CreateCommand();
                command.CommandText =
                    "INSERT INTO Messages (Guid,Sender,Recipient,SendDateTime,StoreDateTime,DataLength,DataType,Text)" +
                    "VALUES (@Guid,@Sender,@Recipient,@SendDateTime,@StoreDateTime,@DataLength,@DataType,@Text)";
                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("@Guid", message.Guid),
                    new SqlParameter("@Sender", message.Sender),
                    new SqlParameter("@Recipient", message.Recipient),
                    new SqlParameter("@SendDateTime", message.SendDateTime),
                    new SqlParameter("@StoreDateTime", message.StoreDateTime),
                    new SqlParameter("@DataLength", message.DataLength),
                    new SqlParameter("@DataType", message.DataType),
                    new SqlParameter("@Text", message.Text)
                });
                var count = await command.ExecuteNonQueryAsync();
                if (count < 1) throw new DataException($"Cannot insert message in database: {command}");
            }
        }

        public async Task<MessagesMetrics> GetMetrics(string recipient, int id, int count)
        {
            var metrics = new MessagesMetrics {Count = 0, DataLength = 0};
            if (count == 0) return metrics;

            using (var con = new SqlConnection())
            {
                con.ConnectionString = _connectionString;
                await con.OpenAsync();
                var command = con.CreateCommand();

                command.CommandText =
                    "SELECT count(*) AS mc, sum(DataLength) AS mdl FROM [dbo].[Messages] WHERE Recipient = @Recipient AND Id > @Id";
                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("@Recipient", recipient),
                    new SqlParameter("@Id", id)
                });

                var reader = await command.ExecuteReaderAsync();
                if (reader.Read())
                {
                    metrics.Count = reader.GetInt32("mc");
                    if (metrics.Count > 0 && !reader.IsDBNull("mdl"))
                    {
                        metrics.DataLength = reader.GetInt32("mdl");
                    }
                }

                reader.Close();
            }

            return metrics;
        }

        public async Task<StoreMessage[]> GetMessages(string recipient, int id, int count)
        {
            var messages = new List<StoreMessage>();
            if (count == 0) return messages.ToArray();

            using (var con = new SqlConnection())
            {
                con.ConnectionString = _connectionString;
                await con.OpenAsync();
                var command = con.CreateCommand();

                command.CommandText =
                    "SELECT TOP (@count) Id,Guid,Sender,Recipient,SendDateTime,StoreDateTime,DataLength,DataType,Text " +
                    "FROM Messages WHERE Recipient = @Recipient AND Id > @Id " +
                    "ORDER BY Id";
                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("@count", count),
                    new SqlParameter("@Recipient", recipient),
                    new SqlParameter("@Id", id)
                });


                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var m = new StoreMessage
                    {
                        Id = reader.GetInt32("Id"),
                        Guid = reader.GetString("Guid"),
                        Sender = reader.GetString("Sender"),
                        Recipient = reader.GetString("Recipient"),
                        SendDateTime = reader.GetDateTime("SendDateTime"),
                        StoreDateTime = reader.GetDateTime("StoreDateTime"),
                        DataType = reader.GetString("DataType"),
                        DataLength = reader.GetInt32("DataLength"),
                        Text = reader.GetString("Text")
                    };
                    messages.Add(m);
                }

                reader.Close();
            }

            return messages.ToArray();
        }
    }
}
