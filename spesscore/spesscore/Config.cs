public static class Config
{
    public static string WorkspacePath;
    public static int ThreadCount;
    public static int InputBufferSize;
    public static int EventBufferSize;
    public static int MaxNetPacketSize;
    public static int WebsocketPort;
    public static string IPCSocketPath;
    public static bool DebugAllowGCHooks;
    public static bool DebugUseNativePatternMatching;
    public static List<int> MemorySizes = [];
    public static List<int> DiskSizes = [];
    public static int MaxActiveSystems;
    public static int MaxSystemMemory;
    public static bool NoCreateDefaultTables;
    public static bool DebugEnableControlWS; // YOU SHOULD NEVER EVER EVER USE THIS IN PRODUCTION
    public static bool AllowCustomEEPROMCode;
    public static int ParentPID;

    static void TrySet(Dictionary<string,string> keys, string key, string fallback, out string value)
    {
        if (!keys.TryGetValue(key, out value)) value = fallback;
    }

    static void TrySet(Dictionary<string,string> keys, string key, int fallback, out int value)
    {
        if (keys.TryGetValue(key, out string tvh)) value = int.Parse(tvh);
        else value = fallback;
    }

    static void TrySet(Dictionary<string,string> keys, string key, List<string> fallback, List<string> value)
    {
        if (keys.TryGetValue(key, out string tvh))
        {
            string[] parts = tvh.Split(",");
            value.AddRange(parts);
        } else
        {
            value.AddRange(fallback);
        }
    }

    static void TrySet(Dictionary<string,string> keys, string key, List<int> fallback, List<int> value)
    {
        if (keys.TryGetValue(key, out string tvh))
        {
            string[] parts = tvh.Split(",");
            foreach (string part in parts)
            {
                value.Add(int.Parse(part));
            }
        } else
        {
            value.AddRange(fallback);
        }
    }

    public static void ParseConfig(List<string> flags, Dictionary<string,string> keys)
    {
        // flags
        DebugAllowGCHooks = flags.Contains("debugallowgchooks");
        DebugUseNativePatternMatching = flags.Contains("debugusenativepatternmatching");
        NoCreateDefaultTables = flags.Contains("nocreatedefaulttables");
        AllowCustomEEPROMCode = flags.Contains("allowcustomeepromcode");
        DebugEnableControlWS = true;//flags.Contains("debugenablecontrolws"); // IF YOU USE THIS IN PROD I WILL KILL YOU
                                                                       // LIKELY WITH A BALLPEEN HAMMER

        // key-value pairs
        TrySet(keys, "workspacepath", "./spesscomputers", out WorkspacePath);
        TrySet(keys, "threadcount", 4, out ThreadCount);
        TrySet(keys, "inputbuffersize", 1024, out InputBufferSize);
        TrySet(keys, "eventbuffersize", 32, out EventBufferSize);
        TrySet(keys, "maxnetpacketsize", 4096, out MaxNetPacketSize);
        TrySet(keys, "websocketport", 42069, out WebsocketPort);
        TrySet(keys, "ipcsocketpath", "./sock", out IPCSocketPath);
        TrySet(keys, "maxactivesystems", 200, out MaxActiveSystems);
        TrySet(keys, "maxsystemmemory", 12288, out MaxSystemMemory);
        TrySet(keys, "memorysizes", [128, 256, 512, 1024, 512, 1024, 2048, 3072], MemorySizes);
        TrySet(keys, "disksizes", [512, 1024, 2048, 4096, 8192], DiskSizes);
        TrySet(keys, "parentpid", -1, out ParentPID);
    }
}