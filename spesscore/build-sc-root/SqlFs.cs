using System.Data.SQLite;

class SqlFs
{
    public SQLiteConnection con;
    
    public SqlFs(string path)
    {
        con = new SQLiteConnection($"Data Source={path};");
    }

    public void AddFile(string path)
    {
        
    }
}