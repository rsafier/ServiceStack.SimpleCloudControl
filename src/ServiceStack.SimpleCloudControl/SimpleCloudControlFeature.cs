using System;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.Discovery.Redis;

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

        private readonly ILog Log = LogManager.GetLogger(typeof(SimpleCloudControlFeature));

        protected IRedisPubSubServer PubSubLink { get; set; }

        public void Register(IAppHost appHost)
        {
            var rsdFeature = appHost.GetPlugin<RedisServiceDiscoveryFeature>();
            if (rsdFeature == null)
            {
                throw new Exception("RedisServiceDiscoveryFeature registration is required for SimpleCloudControl to function.");
            }
            rsdFeature.Config.Meta[RedisServiceDiscoveryMetaKey] = RedisPrefix;
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
        }

        private void OnMessage(string channel, string msg)
        {
            if (!msg.IsEmpty() && msg.Contains(":"))
            {
                var data = msg.SplitOnFirst(":");
                HostContext.AppHost.ExecuteService(JsonSerializer.DeserializeFromString(data[1], Type.GetType(data[0]))); //just push along like any other message
            }
        }

        public void Dispose()
        {
            PubSubLink.Dispose();
        }
    }
}
