using System.Data.SQLite;

class SqlFs
{
    public SQLiteConnection con;
    public string root;

    public Dictionary<string,int> mapping = [];
    
    public SqlFs(string path, string root)
    {
        this.root = root;
        File.Delete(path);
        con = new SQLiteConnection($"Data Source={path};");
        con.Open();
        Init();
    }

    public void ExecuteNonQuery(string cmd)
    {
        new SQLiteCommand(cmd, con).ExecuteNonQuery();
    }

    public void Init()
    {
        /* ExecuteNonQuery("PRAGMA journal_mode = WAL;");
        ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
        ExecuteNonQuery("PRAGMA temp_store = MEMORY;"); */
        ExecuteNonQuery("PRAGMA foreign_keys = ON;");
        ExecuteNonQuery("PRAGMA page_size = 512;");
        ExecuteNonQuery("""
        create table fmeta (
            inode integer primary key,
            name text not null,
            parent integer,
            owner integer not null,
            ogroup integer not null,
            mtime integer not null,
            flags integer not null,
            size integer not null,
            target text,
            foreign key(parent) references fmeta(inode) on delete cascade
        ) strict;
        """);
        ExecuteNonQuery("""
        create table fdata (
            inode integer not null,
            blkid integer not null,
            data blob not null,
            foreign key(inode) references fmeta(inode) on delete cascade
        ) strict;
        """);
    }

    public int NewEntry(string name, int parent, int owner, int group, int mtime, int flags, int size, string? target)
    {
        var query = new SQLiteCommand("""
        INSERT INTO fmeta (name, parent, owner, ogroup, mtime, flags, size, target) VALUES (
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?
        ); 
        """, con);
        query.Parameters.Add(new SQLiteParameter { Value = name });
        if (parent >= 0)
            query.Parameters.Add(new SQLiteParameter { Value = parent });
        else
            query.Parameters.Add(new SQLiteParameter { Value = null });
        query.Parameters.Add(new SQLiteParameter { Value = owner });
        query.Parameters.Add(new SQLiteParameter { Value = group });
        query.Parameters.Add(new SQLiteParameter { Value = mtime });
        query.Parameters.Add(new SQLiteParameter { Value = flags });
        query.Parameters.Add(new SQLiteParameter { Value = size });
        query.Parameters.Add(new SQLiteParameter { Value = target });
        query.ExecuteNonQuery();
        return (int)(con.LastInsertRowId & 0x7FFF_FFFF);
    }

    public int LookupFile(string? path)
    {
        //Console.WriteLine(path);
        if (path != null && mapping.TryGetValue(path, out int val))
            return val;
        else
            return -1;
    }

    public void AddFile(string name)
    {
        string rel = Path.GetRelativePath(root, name);
        int fz = (int)(new FileInfo(name).Length & 0x7FFF_FFFF);
        int inode = NewEntry(Path.GetFileName(name), LookupFile(Path.GetDirectoryName(rel)), 0, 0, 0, 0x2000, fz, null);
        var query = new SQLiteCommand("INSERT INTO fdata (inode, blkid, data) VALUES (@inode, @blkid, @data);", con);
        query.Parameters.Add("@inode", System.Data.DbType.Int32);
        query.Parameters.Add("@blkid", System.Data.DbType.Int32);
        query.Parameters.Add("@data", System.Data.DbType.Binary);
        query.Prepare();
        query.Parameters["@inode"].Value = inode;
        using var fs = File.OpenRead(name);
        int id = 0;
        byte[] buffer = new byte[512];
        while (fz > 0)
        {
            fz -= fs.Read(buffer, 0, 512);
            query.Parameters["@blkid"].Value = id++;
            query.Parameters["@data"].Value = buffer;
            query.ExecuteNonQuery();
        }
    }

    public void AddDirectory(string name)
    {
        string rel = Path.GetRelativePath(root, name);
        int id = NewEntry(Path.GetFileName(name), LookupFile(Path.GetDirectoryName(rel)), 0, 0, 0, 0x1000, 0, null);
        mapping[rel] = id;
    }

    void WriteFile(int inode, string path)
    {
        int fz = (int)(new FileInfo(path).Length & 0x7FFF_FFFF);
        var query = new SQLiteCommand("INSERT INTO fdata (inode, blkid, data) VALUES (@inode, @blkid, @data);", con);
        query.Parameters.Add("@inode", System.Data.DbType.Int32);
        query.Parameters.Add("@blkid", System.Data.DbType.Int32);
        query.Parameters.Add("@data", System.Data.DbType.Binary);
        query.Prepare();
        query.Parameters["@inode"].Value = inode;
        using var fs = File.OpenRead(path);
        int id = 0;
        byte[] buffer = new byte[512];
        while (fz > 0)
        {
            fz -= fs.Read(buffer, 0, 512);
            query.Parameters["@blkid"].Value = id++;
            query.Parameters["@data"].Value = buffer;
            query.ExecuteNonQuery();
        }
    }

    public void AddEntry(SCStat st)
    {
        Console.WriteLine(st.Path);
        int parent = LookupFile(Path.GetDirectoryName(st.Path));
        string name = Path.GetFileName(st.Path);
        int fz = 0;
        if (st.Type == SCStat.File && !st.Virtual)
        {
            fz = (int)(new FileInfo(st.RealPath).Length & 0x7FFF_FFFF);
        }
        //Console.WriteLine($"{st.Path}: {st.Type} - {st.Perms} ({st.Perms | (short)(st.Type << 12)})");
        int ind = NewEntry(name, parent, st.Owner, st.Group, 0, st.Perms | (short)(st.Type << 12), fz, (st.Type == SCStat.Link) ? st.Target : null);
        if (st.Type == SCStat.File && !st.Virtual)
        {
            WriteFile(ind, st.RealPath);
        } else if (st.Type == SCStat.Dir)
        {
            mapping[st.Path] = ind;
        }
    }
}