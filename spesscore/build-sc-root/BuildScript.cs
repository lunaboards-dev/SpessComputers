using System.Data.SQLite;

class BuildScript
{
    class BuildRule
    {
        public char type;
        public string path;
        public Dictionary<string,string> args = [];
        public Dictionary<string,string> rules = [];
    }

    List<BuildRule> rules = [];
    public List<(string,string)> commands = [];
    delegate void ScriptCommand(string arg);

    string fsrc = ".";
    SqlFs fs;

    static (string,int) NextString(string s, int idx)
    {
        while (char.IsWhiteSpace(s[idx]) && idx < s.Length) idx++;
        int st = idx;
        while (idx < s.Length && !char.IsWhiteSpace(s[idx++])) {};
        if (st >= s.Length) return ("", -1);
        string rtv = s[st..idx];
        return (rtv, idx);
    }

    static (string,string) KVParse(string s)
    {
        int sep = s.IndexOf('=');
        if (sep < 0) return (s, "");
        return (s[..sep], s[(sep+1)..]);
    }

    Dictionary<string,ScriptCommand> CmdExec;
    BuildScript()
    {
        CmdExec = new()
        {
            {"output", Output},
            {"source", Source},
            {"exec", Execute}
        };
    }

    public BuildScript(string path) : this()
    {
        using FileStream strm = File.OpenRead(path);
        using StreamReader rdr = new(strm);
        string? line;
        while ((line = rdr.ReadLine()) != null)
        {
            int idx = line.IndexOf('#');
            if (idx > 0)
            {
                line = line.Substring(idx+1);
            }
            line = line.Trim();
            if (line.Length == 0) continue;
            char rtype = line[0];
            string rpath;
            (rpath, idx) = NextString(line, 1); // might as well reuse the idx variable
            if (rtype == '@')
            {
                // command
                commands.Add((rpath, line.Substring(idx).Trim()));
            } else
            {
                BuildRule rule = new()
                {
                    type = rtype,
                    path = rpath
                };
                while (true)
                {
                    (rpath, idx) = NextString(line, idx);
                    if (idx == -1) break;
                    var kvpair = KVParse(rpath);
                    if (rpath[0] == '$')
                        rule.rules[kvpair.Item1[1..]] = kvpair.Item2;
                    else
                        rule.rules[kvpair.Item1] = kvpair.Item2;
                }
                rules.Add(rule);
            }
        }
    }

    void Run()
    {
        foreach (var cmd in commands)
        {
            if (!CmdExec.TryGetValue(cmd.Item1, out ScriptCommand? del))
            {
                Console.Error.WriteLine($"Unknown command: {cmd.Item1}");
                Environment.Exit(1);
            }
        }
    }

    // commands
    void Source(string path)
    {
        fsrc = path;
    }

    void Output(string path)
    {
        fs = new SqlFs(path);
    }

    void Execute(string cmd)
    {
        if (fs == null)
        {
            Console.Error.WriteLine("Can't execute command on unopened filesystem!");
            Environment.Exit(1);
        }
        new SQLiteCommand(cmd, fs.con).ExecuteNonQuery();
    }
}