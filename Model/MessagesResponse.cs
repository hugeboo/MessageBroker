using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MessageBroker.Model
{
    public sealed class MessagesResponse
    {
        [Required]
        public int MessageCount { get; set; }
        [Required]
        public int MessageTotalCount { get; set; }
        [Required]
        public IncomingMessage[] Messages { get; set; }
    }
}
