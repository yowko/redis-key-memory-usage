using StackExchange.Redis;

// endpoint 使用環境變數
var configString =Environment.GetEnvironmentVariable("endpoint");
if (string.IsNullOrWhiteSpace(configString))
{
    throw new ArgumentException();
}
var options = ConfigurationOptions.Parse(configString);

// 如果 redis 有 rename command，可以在這邊做設定
// var commands = new Dictionary<string, string>
// {
//     { "SCSN", "yowko_scan" },
//     { "EVAL", "yowko_EVAL" }
// };
// options.CommandMap = CommandMap.Create(commands);

using ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options);
IDatabase db = redis.GetDatabase(0);

// 取得目標 server 上的 slot 資訊
IServer server = redis.GetServer(options.EndPoints[0]);
var serverSetting = server.ClusterConfiguration.Nodes.First(a => a.Raw.Contains(server.ClusterConfiguration.Origin.ToString()));
var slots = serverSetting.Slots.First();


string luaScript = @"
             local from = tonumber(ARGV[1])
             local to = tonumber(ARGV[2])
             local result = {}

             for slot=from,to do
                 local keysInSlot = redis.call('CLUSTER', 'COUNTKEYSINSLOT', slot) -- 計算指定 slot 的 key 數量
                 local keys = redis.call('CLUSTER', 'GETKEYSINSLOT', slot, keysInSlot) -- 取得指定 slot 的所有 key
                 for _, key in ipairs(keys) do
                     local memory = redis.call('MEMORY', 'USAGE', key)
                     table.insert(result, { key, memory })
                 end
             end
             return result
         ";

RedisResult result = db.ScriptEvaluate(luaScript, new RedisKey[] { }, new RedisValue[] { slots.From, slots.To },CommandFlags.DemandReplica);
foreach (var usage in ((RedisResult[])result).SelectMany(a => a.ToDictionary()).ToDictionary())
{
    Console.WriteLine($"Key: {usage.Key} Memory: {HumanSize((long)usage.Value)}");
}

static string HumanSize(long size)
{
    string[] hum = { "Gb", "Mb", "Kb" };
    long[] sizes = { 1024L * 1024 * 1024, 1024 * 1024, 1024 };
        
    for (int i = 0; i < sizes.Length; i++)
    {
        if (size >= sizes[i])
        {
            return string.Format("{0:0.00} {1}", (double)size / sizes[i], hum[i]);
        }
    }

    return $"{size.ToString()} b";
}