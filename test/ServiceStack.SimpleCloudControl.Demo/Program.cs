using Funq;
using ServiceStack.Discovery.Redis;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.SimpleCloudControl.Caching;
using ServiceStack.SimpleCloudControl.ExternalService;
using ServiceStack.SimpleCloudControl.MQControl;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.SimpleCloudControl.Demo.ExternalService
{
    public class AppHost : AppHostHttpListenerSmartPoolBase
    {
        public AppHost() : base("TestAPI", typeof(AppHost).Assembly)
        { }
        private string HostAt;
        public AppHost(string hostAt) :this()
        {
            HostAt = hostAt;
        }

        public override void Configure(Container container)
        {
            container.Register<IRedisClientsManager>(new RedisManagerPool("localhost:6379", new RedisPoolConfig { MaxPoolSize = 100, }));
            var mq = new RedisMqServer(container.Resolve<IRedisClientsManager>(), 0);
            mq.RegisterHandler<SubmitFoo>(HostContext.ServiceController.ExecuteMessage);
            mq.Start();
            container.Register<IMessageService>(mq);
            SetConfig(new HostConfig
            {
                WebHostUrl = HostAt.Replace("*", Dns.GetHostName())
            });
            LoadPlugin(new RedisServiceDiscoveryFeature());
            LoadPlugin(new SimpleCloudControlFeature());
            LoadPlugin(new SimpleMQControlFeature());
            LoadPlugin(new SimpleHybridCacheFeature());
            LoadPlugin(new SimpleCloudControlAdminFeature());
            JsConfig.DateHandler = DateHandler.ISO8601DateTime;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;
            var app = new AppHost(args[0]).Init().Start(args[0]);
            Task.Delay(1000000000).Wait();
        }
    }

    public class Sample1 : Service
    {
        public FooResponse Any(SubmitFoo req)
        {
            if (!Request.RequestAttributes.HasFlag(RequestAttributes.MessageQueue))
            {
                PublishMessage(req);
                return new FooResponse { Queued = true};
            }
            return new FooResponse() { Name = Gateway.Send(new SubmitBarExternal() { Name = req.Name }) };
        } 

        public string Any(SubmitFooExternal req)
        {
            return $"{req.Name} then calls back its own service";
        }

    }

    public class SubmitFooExternal : IReturn<string>
    {
        public string Name { get; set; }
    }

    public class SubmitFoo : IReturn<FooResponse>
    {
        public string Name { get; set; }
    }

    public class FooResponse
    {
        public bool Queued { get; set; }
        public string Name { get; set; }
    }

    public class SubmitFooQueue : IReturnVoid
    {
        public string Name { get; set; }
    }
}
