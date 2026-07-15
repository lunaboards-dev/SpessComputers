// See https://aka.ms/new-console-template for more information
// simple arg parsing
using spesscore;

List<string> flags = [];
Dictionary<string, string> nargs = [];
Console.WriteLine(string.Join(" ", args));
foreach (string arg in args)
{
    int pos;
    if ((pos = arg.IndexOf('=')) > 0)
    {
        string key = arg[..(pos - 1)];
        string value = arg[(pos + 1)..];
        nargs[key.ToLower()] = value;
    } else
    {
        flags.Add(arg.ToLower());
    }
}

Config.ParseConfig(flags, nargs);

Directory.SetCurrentDirectory(Config.WorkspacePath);
SpessCore sc = new();
SpessCore.inst = sc;

sc.Start();