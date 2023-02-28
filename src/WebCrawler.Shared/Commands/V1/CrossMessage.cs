using System;
using System.Collections.Generic;
using System.Text;

namespace WebCrawler.Shared.Commands.V1
{
    public class CrossMessage
    {
        public CrossMessage(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
