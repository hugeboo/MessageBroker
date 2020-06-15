using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MessageBroker.Model
{
    public sealed class IncomingMessage
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Sender { get; set; }
        [Required]
        public string Recipient { get; set; }
        [Required]
        public DateTime SendDateTime { get; set; }
        public string Data { get; set; }
        [Required]
        public string DataType { get; set; }
        [Required]
        public string Text { get; set; }
    }
}
