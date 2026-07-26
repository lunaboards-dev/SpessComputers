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
    List<BuildRule> virt_files = [];
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
                        rule.args[kvpair.Item1] = kvpair.Item2;
                }
                if (rule.type == '+')
                {
                    virt_files.Add(rule);
                } else {
                    rules.Add(rule);
                }
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
                //Console.WriteLine($"CMD: {cmd.Item1} - {cmd.Item2}");
                del(cmd.Item2);
            }
        }
        Recurse();
        foreach (var vfile in virt_files)
        {
            SCStat stat = new()
            {
                Virtual = true,
                Path = vfile.path
            };
            if (!DoesFilterApply(stat, vfile)) continue;
            ApplyRule(stat, vfile); // this always applies first!
            if (!ApplyRules(stat)) continue;
            fs.AddEntry(stat);
        }
    }

    bool PathCompare(string pat, string path)
    {
        string[] p1_parts = pat.Split('/');
        string[] p2_parts = pat.Split('/');
        if (p1_parts.Length > p2_parts.Length) return false;
        if (p1_parts.Length == p2_parts.Length && pat.EndsWith('/')) return false;
        for (int i=0; i<p1_parts.Length; ++i)
        {
            if (p1_parts[i] != p2_parts[i]) return false;
        }
        return true;
    }

    static Dictionary<string,byte> type_map = new()
    {
        {"directory", 0x1},
        {"dir", 0x1},
        {"folder", 0x1},
        {"file", 0x2},
        {"normal", 0x2},
        {"regular", 0x2},
        {"symlink", 0x3},
        {"link", 0x3}
    };

    byte TypeMap(string key)
    {
        if (type_map.TryGetValue(key.ToLower(), out byte val))
        {
            return val;
        } else
        {
            Console.Error.WriteLine("Warning: unknown file type: "+key.ToLower());
            return 0;
        }
    }

    bool DoesFilterApply(SCStat st, BuildRule rule)
    {
        if (!PathCompare(rule.path, st.Path)) return false;
        foreach (var filt in rule.rules)
        {
            switch (filt.Key)
            {
                case "ext":
                    if (Path.GetExtension(st.Path).Substring(2) != filt.Value) return false;
                    break;
                case "type":
                    if (st.Type != TypeMap(filt.Value)) return false;
                    break;
                case "virtual":
                case "virt":
                    if (!st.Virtual) return false;
                    break;
                case "physical":
                case "real":
                    if (st.Virtual) return false;
                    break;
                case "!exists":
                    string path = Path.Join(fsrc, rule.path);
                    if (File.Exists(path) || Directory.Exists(path)) return false;
                    break;
                default:
                    Console.Error.WriteLine("Warning: Unknown filter: "+filt.Key);
                    break;
            }
        }
        return true;
    }

    short RwxParse(string rwx)
    {
        short res = 0;
        for (int i=0; i<rwx.Length; i++)
        {
            if (rwx[rwx.Length-i-1] != '-')
            {
                res |= (short) (1<<i);
            }
        }
        return res;
    }

    void ApplyRule(SCStat st, BuildRule rule)
    {
        foreach (var fx in rule.args)
        {
            switch (fx.Key)
            {
                case "chown":
                    // set owner/group
                    int idx = fx.Value.IndexOf(":");
                    string oid = fx.Value[..idx];
                    string gid = fx.Value[(idx+1)..];
                    //Console.WriteLine($"oid: {oid}, gid: {gid}");
                    st.Owner = short.Parse(oid, System.Globalization.NumberStyles.None);
                    st.Group = short.Parse(gid, System.Globalization.NumberStyles.None);
                    break;
                case "chmod":
                    // set perms
                    st.Perms = RwxParse(fx.Value);
                    break;
                case "type":
                    if (rule.type == '+')
                    {
                        st.Type = TypeMap(fx.Value);
                    } else
                    {
                        Console.Error.WriteLine($"Warning: 'type' argument not valid for rule ({rule})");
                    }
                    break;
                case "target":
                    st.Target = fx.Value;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown effect {fx.Key} ({rule})");
                    break;
            }
        }
    }

    bool ApplyRules(SCStat stat)
    {
        foreach (var rule in rules)
        {
            if (DoesFilterApply(stat, rule))
            {
                if (rule.type == '-') return false;
                else if (rule.type == '~') ApplyRule(stat, rule);
                else Console.Error.WriteLine($"Warning: Unknown rule type '{rule.type}' ({rule})");
            }
        }
        return true;
    }

    void ActAndApply(string ent)
    {
        string rpath = Path.GetRelativePath(fsrc, ent);
        SCStat stat = new()
        {
            RealPath = ent,
            Path = rpath,
            Virtual = false
        };
        var attr = File.GetAttributes(ent);
        if ((attr & FileAttributes.Directory) > 0)
        {
            stat.Type = SCStat.Dir;
        } else
        {
            stat.Type = SCStat.File;
        }
        if (!ApplyRules(stat)) return;
        fs.AddEntry(stat);
    }

    void Recurse()
    {
        var tr = fs.con.BeginTransaction();
        foreach (var ent in Directory.EnumerateFileSystemEntries(fsrc, "*", SearchOption.AllDirectories))
        {
            ActAndApply(ent);
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