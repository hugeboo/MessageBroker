using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MessageBroker.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MessageBroker.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDbAccess _dbAccess;
        private readonly IDocumentStoreClient _documentStoreClient;
        private readonly IConfiguration _config;

        public MessagesController(IDbAccess dbAccess, IDocumentStoreClient documentStoreClient, IConfiguration config)
        {
            _dbAccess = dbAccess;
            _documentStoreClient = documentStoreClient;
            _config = config;
        }

        // GET <MessagesController>/Dimon/0?count=10
        [HttpGet("{recipient}/{id}")]
        public async Task<MessagesResponse> Get(string recipient, int id)
        {
            var count = 1;
            if (this.Request.Query.ContainsKey("count") &&
                int.TryParse(this.Request.Query["count"], out int queryCount))
            {
                count = Math.Min(queryCount, int.Parse(_config["MessageBroker:MaxMessageCountPerRequest"]));
            }

            return await GetMessages(recipient, id, count);
        }

        // POST <MessagesController>
        [HttpPost]
        public async Task Post([FromBody] OutgoingMessage outgoingMessage)
        {
            ValidateOutgoingMessage(outgoingMessage);

            var guid = Guid.NewGuid().ToString().ToUpper();

            if (!string.IsNullOrWhiteSpace(outgoingMessage.Data))
            {
                await _documentStoreClient.Post(guid, outgoingMessage.Data);
            }

            var storeMessage = new StoreMessage
            {
                Guid = guid,
                Sender = outgoingMessage.Sender,
                Recipient = outgoingMessage.Recipient,
                SendDateTime = outgoingMessage.SendDateTime,
                StoreDateTime = DateTime.UtcNow,
                DataLength = outgoingMessage.Data?.Length ?? 0,
                DataType = outgoingMessage.DataType ?? "",
                Text = outgoingMessage.Text ?? ""
            };

            await _dbAccess.InsertMessage(storeMessage);
        }

        private void ValidateOutgoingMessage(OutgoingMessage m)
        {
            if (string.IsNullOrWhiteSpace(m.Sender)) throw new HttpRequestException("Sender is unspecified");
            if (string.IsNullOrWhiteSpace(m.Recipient)) throw new HttpRequestException("Recipient is unspecified");
            if (m.SendDateTime < new DateTime(2020, 6, 13)) throw new HttpRequestException("Invalid SendDateTime");
            if (string.IsNullOrWhiteSpace(m.Data) && string.IsNullOrWhiteSpace(m.Text))
                throw new HttpRequestException("Data or Text must be specified");
            if (!string.IsNullOrWhiteSpace(m.Data) && string.IsNullOrWhiteSpace(m.DataType))
                throw new HttpRequestException("DataType is unspecified");

            if (m.Sender.Length > 64) throw new HttpRequestException("Sender Length must be less 64 bytes");
            if (m.Recipient.Length > 64) throw new HttpRequestException("Recipient Length must be less 64 bytes");
            if (m.DataType.Length > 36) throw new HttpRequestException("DataType Length must be less 36 bytes");
            if (m.Text.Length > 256) throw new HttpRequestException("Text Length must be less 256 bytes");

            var maxDataLength = int.Parse(_config["MessageBroker:MaxDataLength"]);
            if (m.Data.Length > maxDataLength)
                throw new HttpRequestException($"Data Length must be less {maxDataLength} bytes");
        }

        private async Task<MessagesResponse> GetMessages(string recipient, int id, int count)
        {
            var totalMetrics = await _dbAccess.GetMetrics(recipient, id, int.MaxValue);
            if (count == 0)
            {
                return new MessagesResponse {MessageCount = 0, MessageTotalCount = totalMetrics.Count};
            }

            var metrics = await _dbAccess.GetMetrics(recipient, id, count);
            if (metrics.Count == 0)
            {
                return new MessagesResponse {MessageCount = 0, MessageTotalCount = totalMetrics.Count};
            }

            var messages = await _dbAccess.GetMessages(recipient, id, count);

            var maxSize = int.Parse(_config["MessageBroker:MaxDataLengthPerRequest"]);
            var realCount = messages.Length;
            if (metrics.DataLength > maxSize)
            {
                var size = 0;
                realCount = 0;
                var index = 0;
                while (size < maxSize)
                {
                    realCount += 1;
                    size += messages[index].DataLength;
                    index += 1;
                }
            }

            var incomingMessages = new IncomingMessage[realCount];
            for (int i = 0; i < realCount; i++)
            {
                var sm = messages[i];
                var im = new IncomingMessage
                {
                    Id = sm.Id,
                    Sender = sm.Sender,
                    Recipient = sm.Recipient,
                    SendDateTime = sm.SendDateTime,
                    DataType = sm.DataType,
                    Text = sm.Text
                };
                if (sm.DataLength > 0)
                {
                    im.Data = await _documentStoreClient.Get(sm.Guid);
                }
                incomingMessages[i] = im;
            }

            var response = new MessagesResponse
            {
                MessageCount = realCount,
                MessageTotalCount = totalMetrics.Count,
                Messages = incomingMessages
            };

            return response;
        }
    }
}
