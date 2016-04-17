using Funq;
using ServiceStack.Discovery.Redis;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.SimpleCloudControl.Demo.ExternalService;
using ServiceStack.SimpleCloudControl.MQControl;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.SimpleCloudControl.ExternalService
{
    public class AppHost : AppHostHttpListenerSmartPoolBase
    {
        public AppHost() : base("TestAPI2", typeof(AppHost).Assembly)
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
      //      mq.RegisterHandler<Test2>(HostContext.ServiceController.ExecuteMessage);
            mq.Start();
            container.Register<IMessageService>(mq);
            SetConfig(new HostConfig
            {
                WebHostUrl = HostAt.Replace("*", Dns.GetHostName())
            });
            LoadPlugin(new RedisServiceDiscoveryFeature());
            LoadPlugin(new SimpleCloudControlFeature());
            LoadPlugin(new SimpleMQControlFeature());
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
            while(true)
            {
                HostContext.AppHost.ExecuteService(new CallBaseDemoService());
                Task.Delay(1000).Wait();
            }
        }
    } 
    public class ExternalService : Service
    {
        public string Any(SubmitBarExternal req)
        {
            return Gateway.Send(new SubmitFooExternal { Name = req.Name + "from Remote!" });
        } 

        public void Any(CallBaseDemoService req)
        {
            try
            {
                Gateway.Send(new SubmitFoo() { Name = DateTime.Now.Ticks.ToString() }).PrintDump();
            }
            catch(RedisServiceDiscoveryGatewayException e)
            { e.PrintDump(); }
        }
    }

    public class CallBaseDemoService
    {
    }

    public class SubmitBarExternal : IReturn<string>
    {
        public string Name { get; set; }
    }
}
