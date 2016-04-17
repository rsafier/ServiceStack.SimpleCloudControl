////TODO: Grab existing implementation as it had more then what I mocked up here.
////
//using ServiceStack.Configuration;
//using ServiceStack.Redis;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ServiceStack.SimpleCloudControl.Config
//{
//    /// <summary>
//    /// Redis backed AppSettings (to be used as fall thru after local config)
//    /// </summary>
//    public class SimpleCloudConfig : IAppSettings
//    {
//        public string Tier { get; private set; }
//        private readonly string MachineName = Environment.MachineName;
//        private readonly string ServiceName = HostContext.ServiceName;
//        private readonly string GetAllKeysFormat = "{0}:config:*";
//        private readonly string BasicKeyFormat = "{0}:config:{1}"; //prefix::key
//        private readonly string MachineKeyFormat = "{0}:config:host:{1}:{2}"; //prefix:::host:key
//        private readonly string ServiceKeyFormat = "{0}:config:node:{1}:{2}"; //prefix:::servicename:key
//        private readonly string MachineServiceKeyFormat = "{0}:config:{1}:{2}:{3}"; //prefix::host:servicename:key
//        private readonly string TierBasicKeyFormat = "{0}:config:{1}:{2}"; //prefix::tier:key
//        private readonly string TierMachineKeyFormat = "{0}:config:host:{1}:{2}:{3}"; //prefix:::tier:host:key
//        private readonly string TierServiceKeyFormat = "{0}:config:node:{1}:{2}:{3}"; //prefix:::tier:servicename:key
//        private readonly string TierMachineServiceKeyFormat = "{0}:config:{1}:{2}:{3}:{4}"; //prefix::tier:host:servicename:key
//        private SimpleCloudControlFeature CloudControl;
//        private string[] GetKeyNames(string key)
//        {
//            if (Tier.IsEmpty())
//            {
//                return new string[]
//                {
//                    MachineServiceKeyFormat.Fmt(CloudControl.Prefix,MachineName,ServiceName,key),
//                    ServiceKeyFormat.Fmt(CloudControl.Prefix,ServiceName,key),
//                    MachineKeyFormat.Fmt(CloudControl.Prefix,MachineName,key),
//                    BasicKeyFormat.Fmt(CloudControl.Prefix,key),
//                };
//            }
//            else
//            {
//                return new string[] //ordered most to least refined
//                {
//                    TierMachineServiceKeyFormat.Fmt(CloudControl.Prefix,Tier,MachineName,ServiceName,key),
//                    TierServiceKeyFormat.Fmt(CloudControl.Prefix,Tier,ServiceName,key),
//                    TierMachineKeyFormat.Fmt(CloudControl.Prefix,Tier,MachineName,key),
//                    TierBasicKeyFormat.Fmt(CloudControl.Prefix,Tier,key),
//                    MachineServiceKeyFormat.Fmt(CloudControl.Prefix,MachineName,ServiceName,key),
//                    ServiceKeyFormat.Fmt(CloudControl.Prefix,ServiceName,key),
//                    MachineKeyFormat.Fmt(CloudControl.Prefix,MachineName,key),
//                    BasicKeyFormat.Fmt(CloudControl.Prefix,key),
//                };
//            }

//        }
//        public SimpleCloudConfig()
//        {
//            CloudControl = HostContext.GetPlugin<SimpleCloudControlFeature>();
//        }

//        public SimpleCloudConfig(string tier = null) : base()
//        {
//            Tier = tier;
//        }
//        public bool Exists(string key)
//        {
//            var keys = GetKeyNames(key);
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                return r.Custom("EXISTS", keys).Text != "0";
//            }
//        }

//        public T Get<T>(string name)
//        {
//            T ret = default(T);
//            var keys = GetKeyNames(name);
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                foreach(var key in keys)
//                {
//                    ret = r.Get<T>(key);
//                    if (ret != null)
//                        return ret;
//                }
//            }
//            return ret;
//        }

//        public T Get<T>(string name, T defaultValue)
//        {
//            var result = Get<T>(name);
//            if (result == null)
//                return defaultValue;
//            else
//                return result;
//        }

//        public Dictionary<string, string> GetAll()
//        {
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                return r.GetAll<string>(GetAllKeys()) as Dictionary<string, string>;
//            }
//        }

//        /// <summary>
//        /// This will resolve keys to consisent list
//        /// </summary>
//        /// <param name="dictionary"></param>
//        /// <returns></returns>
        

//        public List<string> GetAllKeys()
//        {
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                return FilterAllKeys(r.GetKeysByPattern(GetAllKeysFormat.Fmt(CloudControl.Prefix)));
//            }
//        }

//        private List<string> FilterAllKeys(IEnumerable<string> enumerable)
//        {
//            return enumerable.ToList();
//        }

//        public IDictionary<string, string> GetDictionary(string key)
//        {
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                var kv = r.GetAll<Dictionary<string, string>>(GetKeyNames(key)).FirstOrDefault();
//                if (!kv.Equals(default(KeyValuePair<string, Dictionary<string, string>>)))
//                    return kv.Value;
//            }
//            return null;
//        }

//        public IList<string> GetList(string key)
//        {
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                var kv = r.GetAll<string>(GetKeyNames(key)).FirstOrDefault();
//                if (!kv.Equals(default(KeyValuePair<string, string>)))
//                    return kv.Value.Split(',');
//            }
//            return null;
//        }

//        public string GetString(string name)
//        {
//            using (var r = HostContext.AppHost.GetRedisClient())
//            {
//                var kv = r.GetAll<string>(GetKeyNames(name)).FirstOrDefault();
//                if (!kv.Equals(default(KeyValuePair<string,string>)))
//                    return kv.Value;
//            }
//            return null;
//        }

//        public void Set<T>(string key, T value)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
