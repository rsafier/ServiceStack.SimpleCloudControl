////TODO: Merge in with existing implementation, as this is lacking other features we already have and need (exclusions, etc
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using ServiceStack.Caching;
//using ServiceStack.Metadata;
//using ServiceStack;
//using ServiceStack.DataAnnotations;
//using ServiceStack.Discovery.Redis;
//using ServiceStack.Text;

//namespace ServiceStack.SimpleCloudControl.Caching
//{
//    [Restrict(VisibilityTo = RequestAttributes.None, AccessTo = RequestAttributes.InProcess)]
//    [Exclude(Feature.Metadata)]
//    public class SHCClearKeys
//    {
//        public Guid NodeId { get; set; }
//        public List<string> Keys { get; set; }
//        public bool? FlushAll { get; set; }
//    }

//    public class SimpleHybridCacheFeature : IPlugin
//    {
//        public SimpleHybridCache HybridClient = new SimpleHybridCache();

//        public void Register(IAppHost appHost)
//        {
//            var feature = HostContext.GetPlugin<RedisServiceDiscoveryFeature>();
//            if (feature == null)
//                throw new Exception("SimpleCloudControlFeature is required for this SimpleHybridCacheFeature to operate.");
//            feature.Config.Meta.Add("HybridCache",string.Empty);
//            appHost.Register<ICacheClient>(HybridClient);
//            appHost.Register<ICacheClientExtended>(HybridClient);
//        }
//    }


//    public class SimpleHybridCacheServices : Service
//    {
//        private readonly SimpleHybridCache HybridClient = HostContext.GetPlugin<SimpleHybridCacheFeature>().HybridClient;
//        private readonly Guid NodeId = HostContext.GetPlugin<RedisServiceDiscoveryFeature>().NodeId;



//        public void Any(SHCClearKeys req)
//        {
//            req.PrintDump();
//            if (req.NodeId != NodeId) //coming from another node deleting keys
//            {
//                if (req.FlushAll.GetValueOrDefault())
//                {
//                    HybridClient.FlushAll();
//                }
//                else if (req.Keys.Any())
//                {
//                    HybridClient.RemoveAll(req.Keys);
//                }
//            }
//        }
//    }


//    public class SimpleHybridCache : ICacheClientExtended
//    {
//        private readonly SimpleCloudControlFeature CloudControl = HostContext.AppHost.GetPlugin<SimpleCloudControlFeature>();
//        private readonly Guid NodeId = HostContext.GetPlugin<RedisServiceDiscoveryFeature>().NodeId;
//        private readonly ICacheClient RemoteCache = HostContext.AppHost.GetCacheClient();
//        /// <summary>
//        /// Maintains private local cache to not pollute HostContext Local cache
//        /// </summary>
//        private readonly MemoryCacheClient LocalCache = new MemoryCacheClient();

//        public bool Add<T>(string key, T value)
//        {
//            CloudControl.SendMessage(new SHCClearKeys() { NodeId = NodeId, Keys = new List<string>() { key } });
//            LocalCache.Add(key, value);
//            return RemoteCache.Add(key, value);
//        }

//        public bool Add<T>(string key, T value, TimeSpan expiresIn)
//        {
//            CloudControl.SendMessage(new SHCClearKeys() { NodeId = NodeId, Keys = new List<string> { key } });
//            LocalCache.Add(key, value, expiresIn);
//            return RemoteCache.Add(key, value, expiresIn);
//        }

//        public bool Add<T>(string key, T value, DateTime expiresAt)
//        {
//            CloudControl.SendMessage(new SHCClearKeys() { NodeId = NodeId, Keys = new List<string> { key } });
//            LocalCache.Add(key, value, expiresAt);
//            return RemoteCache.Add(key, value, expiresAt);
//        }


//        public void Dispose()
//        {
//        }

//        public void FlushAll()
//        {
//            LocalCache.FlushAll();
//        }

//        public T Get<T>(string key)
//        {
//            var value = LocalCache.Get<T>(key);
//            if (value == null)
//            {
//                value = RemoteCache.Get<T>(key);
//            }
//            return value;
//        }

//        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
//        {
//            //TODO: work on optimize, this is safe for now
//            return RemoteCache.GetAll<T>(keys);
//        }

//        public IEnumerable<string> GetKeysByPattern(string pattern)
//        {
//            //TODO: work on optimize, this is safe for now
//            return RemoteCache.GetKeysByPattern(pattern);
//        }

//        public TimeSpan? GetTimeToLive(string key)
//        {
//            return RemoteCache.GetTimeToLive(key);
//        }

//        public long Increment(string key, uint amount)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Remove(string key)
//        {
//            CloudControl.SendMessage(new SHCClearKeys() { NodeId = NodeId, Keys = new List<string> { key } });
//            LocalCache.Remove(key);
//            return RemoteCache.Remove(key);
//        }

//        public void RemoveAll(IEnumerable<string> keys)
//        {
//            CloudControl.SendMessage(new SHCClearKeys() { NodeId = NodeId, Keys = new List<string>(keys) });
//            LocalCache.RemoveAll(keys);
//            RemoteCache.RemoveAll(keys);
//        }

//        public bool Replace<T>(string key, T value)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Replace<T>(string key, T value, DateTime expiresAt)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Set<T>(string key, T value)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Set<T>(string key, T value, TimeSpan expiresIn)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Set<T>(string key, T value, DateTime expiresAt)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetAll<T>(IDictionary<string, T> values)
//        {
//            throw new NotImplementedException();
//        }

//        public long Decrement(string key, uint amount)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
