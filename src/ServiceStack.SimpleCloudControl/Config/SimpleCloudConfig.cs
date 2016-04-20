//TODO: Grab existing implementation as it had more then what I mocked up here.
//
using ServiceStack.Configuration;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.SimpleCloudControl.Config
{
    /// <summary>
    /// Redis backed AppSettings (to be used as fall thru after local config)
    /// </summary>
    public class SimpleCloudConfig : IAppSettings, ISettingsWriter, ISettings
    {
        public string Tier { get; private set; }
        public string RedisPrefix { get; private set; }
        private readonly string HostName = Environment.MachineName;
        private readonly string ServiceName = HostContext.ServiceName;
        private readonly string GetAllKeysFormat = "{0}:config:*";
        private readonly string BasicKeyFormat = "{0}:config:{1}"; //RedisPrefix::key
        private readonly string HostKeyFormat = "{0}:config:host:{1}:{2}"; //RedisPrefix:::host:key
        private readonly string ServiceKeyFormat = "{0}:config:node:{1}:{2}"; //RedisPrefix:::servicename:key
        private readonly string HostServiceKeyFormat = "{0}:config:{1}:{2}:{3}"; //RedisPrefix::host:servicename:key
        private readonly string TierBasicKeyFormat = "{0}:config:{1}:{2}"; //RedisPrefix::tier:key
        private readonly string TierHostKeyFormat = "{0}:config:host:{1}:{2}:{3}"; //RedisPrefix:::tier:host:key
        private readonly string TierServiceKeyFormat = "{0}:config:node:{1}:{2}:{3}"; //RedisPrefix:::tier:servicename:key
        private readonly string TierMachineServiceKeyFormat = "{0}:config:{1}:{2}:{3}:{4}"; //RedisPrefix::tier:host:servicename:key
        private string[] GetKeyNames(string key)
        {
            if (Tier.IsEmpty())
            {
                return new string[]
                {
                    HostServiceKeyFormat.Fmt(RedisPrefix,HostName,ServiceName,key),
                    ServiceKeyFormat.Fmt(RedisPrefix,ServiceName,key),
                    HostKeyFormat.Fmt(RedisPrefix,HostName,key),
                    BasicKeyFormat.Fmt(RedisPrefix,key),
                };
            }
            else
            {
                return new string[] //ordered most to least refined
                {
                    TierMachineServiceKeyFormat.Fmt(RedisPrefix,Tier,HostName,ServiceName,key),
                    TierServiceKeyFormat.Fmt(RedisPrefix,Tier,ServiceName,key),
                    TierHostKeyFormat.Fmt(RedisPrefix,Tier,HostName,key),
                    TierBasicKeyFormat.Fmt(RedisPrefix,Tier,key),
                    HostServiceKeyFormat.Fmt(RedisPrefix,HostName,ServiceName,key),
                    ServiceKeyFormat.Fmt(RedisPrefix,ServiceName,key),
                    HostKeyFormat.Fmt(RedisPrefix,HostName,key),
                    BasicKeyFormat.Fmt(RedisPrefix,key),
                };
            }

        }
        public SimpleCloudConfig()
        {
            RedisPrefix = "scc";
        }

        public SimpleCloudConfig(string tier = null, string redisPrefix = "scc") : base()
        {
            Tier = tier;
            RedisPrefix = redisPrefix;
        }
        public bool Exists(string key)
        {
            var keys = GetKeyNames(key);
            using (var r = (RedisClient)HostContext.AppHost.GetRedisClient())
            {
                return r.Exists(key) != 0; // odd really should be on IRedis r.Custom("EXISTS", keys).Text != "0";
            }
        }

        public T Get<T>(string name)
        {
            T ret = default(T);
            var keys = GetKeyNames(name);
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                foreach (var key in keys)
                {
                    ret = r.Get<T>(key);
                    if (ret != null)
                        return ret;
                }
            }
            return ret;
        }

        public T Get<T>(string name, T defaultValue)
        {
            var result = Get<T>(name);
            if (result == null)
                return defaultValue;
            else
                return result;
        }

        public Dictionary<string, string> GetAll()
        {
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                return r.GetAll<string>(r.GetKeysByPattern(GetAllKeysFormat.Fmt(RedisPrefix))) as Dictionary<string, string>;
            }
        }

        /// <summary>
        /// This will resolve keys to consisent list
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>


        public List<string> GetAllKeys()
        {
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                return r.GetKeysByPattern(GetAllKeysFormat.Fmt(RedisPrefix)).ToList();
            }
        }
         
        public IDictionary<string, string> GetDictionary(string key)
        {
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                var kv = r.GetAll<Dictionary<string, string>>(GetKeyNames(key)).FirstOrDefault();
                if (!kv.Equals(default(KeyValuePair<string, Dictionary<string, string>>)))
                    return kv.Value;
            }
            return null;
        }

        public IList<string> GetList(string key)
        {
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                var kv = r.GetAll<string>(GetKeyNames(key)).FirstOrDefault();
                if (!kv.Equals(default(KeyValuePair<string, string>)))
                    return kv.Value.Split(',');
            }
            return null;
        }

        public string GetString(string name)
        {
            return Get<string>(name);
        }

        //Will set at Tier or root level only.
        public void Set<T>(string key, T value)
        {
            using (var r = HostContext.AppHost.GetRedisClient())
            {
                var redisKey = Tier.IsEmpty() ? key : $"{Tier}.{key}";
                r.Set<T>(redisKey, value);
            }
        }

        public string Get(string key)
        {
            return Get<string>(key);
        }
    }
}
