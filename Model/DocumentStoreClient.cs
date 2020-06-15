using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MessageBroker.Model
{
    public interface IDocumentStoreClient
    {
        Task Post(string id, string data);
        Task<string> Get(string id);
    }

    public sealed class DocumentStoreClient : IDocumentStoreClient
    {
        private readonly string _documentStoreAddr;
        private readonly IHttpClientFactory _clientFactory;

        public DocumentStoreClient(IConfiguration con, IHttpClientFactory clientFactory)
        {
            _documentStoreAddr = con["MessageBroker:DocumentStoreAddr"];
            _clientFactory = clientFactory;
        }

        public async Task Post(string id, string data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                _documentStoreAddr + "/documents");

            var body = JsonConvert.SerializeObject(new {Id = id, Data = data});
            request.Content = new StringContent(body, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"StatusCode={response.StatusCode}");
            }
        }

        public async Task<string> Get(string id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                _documentStoreAddr + "/documents/" + id);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "HttpClientFactory");

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jresp = JsonConvert.DeserializeAnonymousType(responseString, new {Id = "", Data = ""});
                return jresp.Data;
            }
            else
            {
                throw new HttpRequestException($"Cannot read message data. StatusCode={response.StatusCode}");
            }
        }
    }
}
