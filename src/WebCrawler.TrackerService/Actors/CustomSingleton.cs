using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCrawler.Shared.Commands.V1;

namespace WebCrawler.TrackerService.Actors
{
    public class CustomSingleton : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public CustomSingleton() 
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Self, new SelfMessage(), Self);

            Receive<CrossMessage>(Handle);
            Receive<SelfMessage>(Handle);
        }

        private bool Handle(SelfMessage message)
        {
            _logger.Info("I am the custom singleton !");
            return true;
        }

        private bool Handle(CrossMessage message)
        {
            _logger.Info($"CustomSingleton - Message received : {message.Message}");
            return true;
        }
    }

    public class SelfMessage { }
}
