// See https://aka.ms/new-console-template for more information
Console.WriteLine("build-sc-root v1.0");
string fn = Path.GetFileName(args[0]);
string path = Path.GetDirectoryName(args[0]) ?? "";
Environment.CurrentDirectory = path;
new BuildScript(args[0]).Run();
