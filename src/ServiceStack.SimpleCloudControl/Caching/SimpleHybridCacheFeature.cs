//TODO: Merge in with existing implementation, as this is lacking other features we already have and need (exclusions, etc
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Metadata;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Discovery.Redis;
using ServiceStack.Text;
using System.Text.RegularExpressions;
using ServiceStack.Logging;
using System.Diagnostics;

namespace ServiceStack.SimpleCloudControl.Caching
{
    [Restrict(VisibilityTo = RequestAttributes.None, AccessTo = RequestAttributes.InProcess)]
    [Exclude(Feature.Metadata)]
    public class SHCClearKeys
    {
        /// <summary>
        /// Source Node Id
        /// </summary>
        public Guid NodeId { get; set; }
        /// <summary>
        /// Keys to remove from local cache
        /// </summary>
        public List<string> Keys { get; set; }
        /// <summary>
        /// Invalidate all local cache
        /// </summary>
        public bool? FlushLocal { get; set; }
    }

    public class SimpleHybridCacheFeature :IPlugin
    {
        public SimpleHybridCacheFeature(Regex blackListKeysRegex = null)
        {
            HybridClient = new SimpleHybridCache(blackListKeysRegex);
        }
        public SimpleHybridCache HybridClient { get; private set; }

        public void Register(IAppHost appHost)
        {
            var feature = HostContext.GetPlugin<RedisServiceDiscoveryFeature>();
            if (feature == null)
                throw new Exception("SimpleCloudControlFeature is required for this SimpleHybridCacheFeature to operate.");
            feature.Config.Meta.Add("HybridCache", string.Empty);
            appHost.Register<ICacheClient>(HybridClient);
            appHost.Register<ICacheClientExtended>(HybridClient);
        }
    }


    public class SimpleHybridCacheServices : Service
    {
        private readonly SimpleHybridCache HybridClient = HostContext.GetPlugin<SimpleHybridCacheFeature>().HybridClient;
        private readonly Guid NodeId = HostContext.GetPlugin<RedisServiceDiscoveryFeature>().NodeId;



        public void Any(SHCClearKeys req)
        {
            req.PrintDump();
            if (req.NodeId != NodeId) //coming from another node deleting keys
            {
                if (req.FlushLocal.GetValueOrDefault())
                {
                    HybridClient.FlushLocal();
                }
                else if (req.Keys.Any())
                {
                    HybridClient.RemoveAll(req.Keys);
                }
            }
        }
    }


    public class SimpleHybridCache : ICacheClientExtended
    {
        public bool BlackListEnabled
        {
            get
            {
                return BlackListKeysRegex != null;
            }
        }
        public Regex BlackListKeysRegex { get; set; }

        private readonly SimpleCloudControlFeature CloudControl = HostContext.AppHost.GetPlugin<SimpleCloudControlFeature>();
        private readonly Guid NodeId = HostContext.GetPlugin<RedisServiceDiscoveryFeature>().NodeId;
        private ICacheClient RedisCache
        {
            get { return HostContext.AppHost.GetCacheClient(null); }
        }

        /// <summary>
        /// Maintains private local cache to not pollute HostContext Local cache
        /// </summary>
        private readonly MemoryCacheClient LocalCache = new MemoryCacheClient();
        private readonly ILog Log = LogManager.GetLogger(typeof(SimpleHybridCache));

        public SimpleHybridCache()
        {
            Log.Debug("SimpleHybridCache Started.");
        }

        public SimpleHybridCache(Regex blackListKeysRegex = null) : base()
        {
            BlackListKeysRegex = blackListKeysRegex;
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            if (BlackListEnabled && !BlackListKeysRegex.IsMatch(key))
                LocalCache.Add(key, value, expiresIn);
            return RedisCache.Add(key, value, expiresIn);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            if (BlackListEnabled && !BlackListKeysRegex.IsMatch(key))
                LocalCache.Add(key, value, expiresAt);
            return RedisCache.Add(key, value, expiresAt);
        }

        public bool Add<T>(string key, T value)
        {
            if (BlackListEnabled && !BlackListKeysRegex.IsMatch(key))
                LocalCache.Add(key, value);
            return RedisCache.Add(key, value);
        }

        public long Decrement(string key, uint amount)
        {
            return RedisCache.Decrement(key, amount);
        }


        public void FlushAll()
        {
            NotifyFlush();
            RedisCache.FlushAll();
        }

        public void FlushLocal()
        {
            NotifyFlush();
        }

        public T Get<T>(string key)
        {
            T data;
            if (BlackListEnabled && !BlackListKeysRegex.IsMatch(key))
            {
                data = LocalCache.Get<T>(key);
                if (data == null)
                {
                    data = RedisCache.Get<T>(key);
                    if (data != null)
                    {
                        //Check TTL, apply if exists
                        var ttl = RedisCache.GetTimeToLive(key);
                        if (ttl.HasValue)
                        {
                            LocalCache.Add(key, data, ttl.Value);
                        }
                        else
                        {
                            LocalCache.Add(key, data);
                        }
                    }
                }
            }
            else
            {
                data = RedisCache.Get<T>(key);
            }
            return data;
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return RedisCache.GetAll<T>(keys);
        }

        public long Increment(string key, uint amount)
        {
            return RedisCache.Increment(key, amount);
        }

        public bool Remove(string key)
        {
            RedisCache.Remove(key);
            NotifyDelete(key);
            return LocalCache.Remove(key);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            NotifyDelete(keys);
            RedisCache.RemoveAll(keys);
            LocalCache.RemoveAll(keys);
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Replace(key, value, expiresIn);
            }
            else
            {
                RedisCache.Replace(key, value, expiresIn);
                return LocalCache.Replace(key, value, expiresIn);
            }
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Replace(key, value, expiresAt);
            }
            else
            {
                RedisCache.Replace(key, value, expiresAt);
                return LocalCache.Replace(key, value, expiresAt);
            }
        }

        public bool Replace<T>(string key, T value)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Replace(key, value);
            }
            else
            {
                RedisCache.Replace(key, value);
                return LocalCache.Replace(key, value);
            }
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Set(key, value, expiresIn);
            }
            else
            {
                RedisCache.Set(key, value, expiresIn);
                return LocalCache.Set(key, value, expiresIn);
            }
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Set(key, value, expiresAt);
            }
            else
            {
                RedisCache.Set(key, value, expiresAt);
                return LocalCache.Set(key, value, expiresAt);
            }
        }

        public bool Set<T>(string key, T value)
        {
            NotifyDelete(key);
            if (BlackListEnabled && BlackListKeysRegex.IsMatch(key))
            {
                return RedisCache.Set(key, value);
            }
            else
            {
                RedisCache.Set(key, value);
                return LocalCache.Set(key, value);
            }
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            NotifyDelete(values.Keys);
            RedisCache.SetAll(values);
            LocalCache.SetAll(values);
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return RedisCache.GetKeysByPattern(pattern);
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return RedisCache.GetTimeToLive(key);
        }

        public void RemoveByPattern(string pattern)
        {
            var keys = GetKeysByPattern(pattern);
            if (keys.Any())
            {
                NotifyDelete(keys);
                LocalCache.RemoveByPattern(pattern);
                RedisCache.RemoveAll(keys);
            }
        }

        public void RemoveByRegex(string regex)
        {
            RemoveByPattern(regex.Replace(".*", "*").Replace(".+", "?"));
        }

        public void Dispose()
        {
            //Ignore
        }

        private void NotifyFlush()
        {
            Debug.Print("SHC NotifyFlush Flush");
            CloudControl.SendMessage(new SHCClearKeys() { FlushLocal = true, NodeId = CloudControl.NodeId });
        }

        private void NotifyDelete(IEnumerable<string> keys)
        {
            Debug.Print("SHC NotifiyDelete Keys:{0}".Fmt(keys.ToJsv()));
            CloudControl.SendMessage(new SHCClearKeys() { FlushLocal = true, NodeId = CloudControl.NodeId, Keys = keys.ToList() });
        }
        private void NotifyDelete(string key)
        {
            Debug.Print("SHC NotifiyDelete Key:{0}".Fmt(key));
            CloudControl.SendMessage(new SHCClearKeys() { FlushLocal = true, NodeId = CloudControl.NodeId, Keys = key.InList() });
        }
    }
}
