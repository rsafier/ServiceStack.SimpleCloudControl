using System;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.Discovery.Redis;
using System.Diagnostics;

namespace ServiceStack.SimpleCloudControl
{
    /// <summary>
    /// Provides a simple wrapper around standard PubSub channel that will allow SCC plug-ins to communicate without expense of a bunch of seperate PubSubs
    /// Requires RedisServiceDiscoveryFeature to be registered being to this plugin, all SCC plug-ins should be loaded after.
    /// </summary>
    public class SimpleCloudControlFeature : IPlugin, IDisposable
    {
        public const string RedisServiceDiscoveryMetaKey = "SimpleCloudControl";
        public string RedisPrefix { get; set; } = "scc";
        public RedisServiceDiscoveryFeature RSDFeature { get; private set; }
        public Guid NodeId
        {
            get { return RSDFeature.NodeId; }
        }
        private readonly ILog Log = LogManager.GetLogger(typeof(SimpleCloudControlFeature));

        protected IRedisPubSubServer PubSubLink { get; set; }

        public void Register(IAppHost appHost)
        {
            RSDFeature = appHost.GetPlugin<RedisServiceDiscoveryFeature>();
            if (RSDFeature == null)
            {
                throw new Exception("RedisServiceDiscoveryFeature registration is required for SimpleCloudControl to function.");
            }
            RSDFeature.Config.Meta[RedisServiceDiscoveryMetaKey] = RedisPrefix;
            PubSubLink = new RedisPubSubServer(appHost.TryResolve<IRedisClientsManager>(), RedisPrefix);
            PubSubLink.OnMessage = OnMessage;
            PubSubLink.OnError = OnError;
            PubSubLink.Start();
        }

        private void OnError(Exception e)
        {
            Log.Error("SimpleCloudControl PubSub Exception", e);
        }

        public void SendMessage(object dto)
        {
            if (dto == null)
                return;
            HostContext.AppHost.GetRedisClient().PublishMessage(RedisPrefix, "{0}:{1}".Fmt(dto.GetType().FullName, dto.ToJson()));
            Debug.Print("SCC SendMessage {0}".Fmt(dto.GetType().FullName));

        }

        private void OnMessage(string channel, string msg)
        {
            if (!msg.IsEmpty() && msg.Contains(":"))
            {
                var data = msg.SplitOnFirst(":");
                var type = Type.GetType(data[0]);
                Debug.Print("SCC OnMessage {0}".Fmt(type.FullName));
                HostContext.AppHost.ExecuteService(JsonSerializer.DeserializeFromString(data[1], type)); //just push along like any other message
            }
        }

        public void Dispose()
        {
            PubSubLink.Dispose();
        }
    }
}
