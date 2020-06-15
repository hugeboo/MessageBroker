using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageBroker.Model
{
    public sealed class StoreMessage
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public DateTime SendDateTime { get; set; }
        public DateTime StoreDateTime { get; set; }
        public int DataLength { get; set; }
        public string DataType { get; set; }
        public string Text { get; set; }
    }
}
