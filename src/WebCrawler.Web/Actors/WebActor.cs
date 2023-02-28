using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.Event;
using System;
using WebCrawler.Shared.Commands.V1;

namespace WebCrawler.Web.Actors
{
    public class WebActor : ReceiveActor
    {
        private IActorRef _singletonProxy;
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public WebActor(IActorRef singletonProxy) 
        {
            _singletonProxy = singletonProxy;

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Self, new SelfMessage(), Self);

            Receive<SelfMessage>(Handle);
        }

        private bool Handle(SelfMessage message)
        {
            var msg = Guid.NewGuid().ToString();
            _logger.Info($"Send message to custom singleton ! ({msg})");

            _singletonProxy.Tell(new CrossMessage($"Hello from web app !! ({msg})"));
            return true;
        }

        public class SelfMessage { }
    }
}
