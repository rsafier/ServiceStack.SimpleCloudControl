using ServiceStack.Discovery.Redis;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using System;
using System.Collections.Generic;

namespace ServiceStack.SimpleCloudControl.MQControl
{
    public class MQControlStatus
    {
        public bool IsRunning { get; set; }
        public IMessageHandlerStats MessageHandlerStats { get; set; }
        public List<string> WorkerThreadsStatus { get; set; } //Only works with Redis
        public string Status { get; set; }
        public string StatusDescription { get; set; }
    }

    [Restrict(VisibilityTo = RequestAttributes.None, AccessTo = RequestAttributes.InProcess)]    
    public class SimpleMQControl : IReturnVoid
    {
        public string TargetHost { get; set; }
        public Guid? TargetNodeId { get; set; }
        public bool? Stop { get; set; }
        public bool? Start { get; set; }
        public bool? Restart { get; set; }
    }



    public class SimpleMQControlFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            var feature = HostContext.GetPlugin<RedisServiceDiscoveryFeature>();
            if (feature == null)
                throw new Exception("RedisServiceDiscoveryFeature is required for this SimpleMQControlFeature to operate.");
            UpdateMQControlStatus(feature);
            var mqServer = appHost.TryResolve<IMessageService>();
            if (mqServer == null)
                throw new Exception("IMessageService is required for this SimpleMQControlFeature to operate.");
            appHost.RegisterService(typeof(SimpleMQControlServices));
            feature.OnNodeRefreshActions.Add(RefreshMQData);
        }

        private void UpdateMQControlStatus(RedisServiceDiscoveryFeature feature)
        {
            var mqServer = HostContext.AppHost.Resolve<IMessageService>();
            var status = new MQControlStatus() { Status = mqServer.GetStatus() };
            if (status.Status == "Starting" || status.Status == "Started")
            {
                status.IsRunning = true;
                status.MessageHandlerStats = mqServer.GetStats();
                status.StatusDescription = mqServer.GetStatsDescription();
                status.WorkerThreadsStatus = (mqServer as RedisMqServer)?.WorkerThreadsStatus(); //Kinda funky, might want to get IMessageService expanded, really would be nice to have fully typed
            }
            feature.Config.Meta["MQControl"] = status.ToJson();
        }

        /// <summary>
        /// TODO:figure out what is actually useful to put here
        /// </summary>
        private void RefreshMQData()
        {
            UpdateMQControlStatus(HostContext.GetPlugin<RedisServiceDiscoveryFeature>());
        }
    }



    public class SimpleMQControlServices : Service
    {
        public void Any(SimpleMQControl req)
        {
            var feature = HostContext.AppHost.GetPlugin<RedisServiceDiscoveryFeature>();
            if (req.TargetNodeId.HasValue)
            {
                if (req.TargetNodeId.Value != feature.NodeId)
                    return;
            }
            else if (!req.TargetHost.EqualsIgnoreCase(feature.HostName))
                return;

            var msgService = HostContext.Resolve<IMessageService>();
            if (req.Stop.GetValueOrDefault())
            {
                msgService.Stop();
            }
            else if (req.Start.GetValueOrDefault())
            {
                msgService.Start();
            }
            else if (req.Restart.GetValueOrDefault())
            {
                msgService.Stop();
                msgService.Start();
            }
        }

    }
}
