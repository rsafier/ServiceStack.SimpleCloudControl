using ServiceStack.Discovery.Redis;
using ServiceStack.SimpleCloudControl.MQControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.SimpleCloudControl
{

    /// <summary>
    /// Will forward request into SSC PubSub
    /// </summary>
    [Route("/SSC/SimpleMQControlRequest")]
    public class SimpleMQControlRequest : IReturnVoid
    {
        public string TargetHost { get; set; }
        public Guid? TargetNodeId { get; set; }
        public bool? Stop { get; set; }
        public bool? Start { get; set; }
        public bool? Restart { get; set; }
    }

    public class SimpleCloudControlAdminFeature : IPlugin
    {
        public const string RedisServiceDiscoveryMetaKey = "SimpleCloudControlAdmin";

        public void Register(IAppHost appHost)
        {
            var rsdFeature = appHost.GetPlugin<RedisServiceDiscoveryFeature>();
            if (rsdFeature == null)
            {
                throw new Exception("RedisServiceDiscoveryFeature registration is required for SimpleCloudControl to function.");
            }
            rsdFeature.Config.Meta[RedisServiceDiscoveryMetaKey] = HostContext.AppHost.Config.WebHostUrl;
            appHost.RegisterService<SimpleCloudControlAdminService>();
        }
    }

    public class SimpleCloudControlAdminService : Service
    {
        public void Any(SimpleMQControlRequest req)
        {
            var sscFeature = HostContext.AppHost.GetPlugin<SimpleCloudControlFeature>();
            sscFeature.SendMessage(req.ConvertTo<SimpleMQControl>());
        }
    }
}
