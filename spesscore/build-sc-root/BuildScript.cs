using System.Data.SQLite;

class BuildScript
{
    class BuildRule
    {
        public char type;
        public string path;
        public Dictionary<string,string> args = [];
        public Dictionary<string,string> rules = [];

        public override string ToString()
        {
            string rule_str = "";
            foreach (var pair in rules)
            {
                rule_str += $" ${pair.Key}={pair.Value}";
            }
            string arg_str = "";
            foreach (var pair in args)
            {
                arg_str += $" {pair.Key}={pair.Value}";
            }
            return type + path + rule_str + arg_str;
        }
    }

    List<BuildRule> rules = [];
    public List<(string,string)> commands = [];
    delegate void ScriptCommand(string arg);

    string fsrc = ".";
    SqlFs fs;

    static (string,int) NextString(string s, int idx)
    {
        while (idx < s.Length && char.IsWhiteSpace(s[idx])) idx++;
        int st = idx;
        while (idx < s.Length && !char.IsWhiteSpace(s[idx++])) {};
        if (st >= s.Length) return ("", -1);
        string rtv = s[st..(idx)].Trim();
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
            {"root", Source},
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
            int idx = line.LastIndexOf('#');
            string pre = line;
            if (idx >= 0)
            {
                line = line.Substring(0, idx);
            }
            line = line.Trim();
            if (line.Length == 0) continue;
            string rpath;
            (rpath, idx) = NextString(line, 0); // might as well reuse the idx variable
            char rtype = rpath[0];
            if (rtype == '@')
            {
                // command
                commands.Add((rpath.Substring(1), line.Substring(idx).Trim()));
            } else
            {
                BuildRule rule = new()
                {
                    type = rtype,
                    path = rpath.Substring(1)
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

    public void Run()
    {
        foreach (var cmd in commands)
        {
            if (!CmdExec.TryGetValue(cmd.Item1, out ScriptCommand? del))
            {
                Console.Error.WriteLine($"Unknown command: '{cmd.Item1}'");
                Environment.Exit(1);
            } else
            {
                Console.WriteLine($"CMD: {cmd.Item1} - {cmd.Item2}");
                del(cmd.Item2);
            }
        }
        foreach (var rule in rules)
        {
            Console.WriteLine($"RULE: {rule}");
        }
        Recurse();
    }

    void ActAndApply(string realpath)
    {
        
    }

    void Recurse()
    {
        var tr = fs.con.BeginTransaction();
        foreach (var ent in Directory.EnumerateFileSystemEntries(fsrc, "*", SearchOption.AllDirectories))
        {
            var attr = File.GetAttributes(ent);
            if ((attr & FileAttributes.Directory) > 0)
            {
                fs.AddDirectory(ent);
            } else
            {
                fs.AddFile(ent);
            }
        }
        tr.Commit();
    }

    // commands
    void Source(string path)
    {
        fsrc = path;
        if (fs != null)
            fs.root = path;
    }

    void Output(string path)
    {
        fs = new SqlFs(path, fsrc);
    }

    void Execute(string cmd)
    {
        if (fs == null)
        {
            Console.Error.WriteLine("Can't execute command on unopened filesystem!");
            Environment.Exit(1);
        }
        fs.ExecuteNonQuery(cmd);
    }
}