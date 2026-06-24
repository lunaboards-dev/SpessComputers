
using System.Data.SQLite;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM.Peripheral;

class ManagedDisk : AbstractPeripheral
{
    SQLiteConnection? db;
    int capacity = 0;
    bool can_pragma = false;

    public override Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => throw new NotImplementedException();

    public ManagedDisk() : base("disk")
    {
        // don't actually do anything until we're assigned an ID.
    }

    void ExecuteNonQuery(string command)
    {
        using (var createdbs = new SQLiteCommand(command, db))
        {
            createdbs.ExecuteNonQuery();
        }
    }

    void Authorize(object sender, AuthorizerEventArgs args)
    {
        // i think this disables writing?
        if (args.ActionCode == SQLiteAuthorizerActionCode.Pragma && args.Argument2 != null && !can_pragma)
        {
            args.ReturnCode = SQLiteAuthorizerReturnCode.Deny;
        }
    }

    public override void SetID(string id)
    {
        if (db != null)
        {
            // close and delete
            db.Close();
            File.Delete(ID+".db");
        }
        db = new SQLiteConnection($"Data Source={id}.db;");
        db.Authorize += Authorize;
        can_pragma = true;
        ExecuteNonQuery($"PRAGMA page_size=512; PRAGMA max_page_count={capacity*2};");
        can_pragma = false;
        if (!Config.NoCreateDefaultTables)
            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS FileMetadata (
                Inode INTEGER PRIMARY KEY AUTOINCREMENT,
                Store STRING,
                Filename STRING NOT NULL,
                User INTEGER NOT NULL,
                Group INTEGER NOT NULL,
                Permissions INTEGER NOT NULL,
                Size INTEGER NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS FileData (
                Inode INTEGER NOT NULL,
                Block INTEGER NOT NULL,
                Data BLOB NOT NULL
            );");
    }

    /* int PushResultTuple(Lua L, SQLiteDataReader reader)
    {
        int cols = reader.FieldCount;
        for (int i=0;i<cols;++i)
        {
            switch(reader.GetFieldAffinity(i))
            {
                case TypeAffinity.Uninitialized:
                case TypeAffinity.Null:
                    L.PushNil();
                    break;
                case TypeAffinity.Int64:
                    L.PushInteger(reader.GetInt64(i));
                    break;
                case TypeAffinity.Double:
                    L.PushNumber(reader.GetDouble(i));
                    break;
                /* case TypeAffinity.Text:
                    L.PushString(reader.GetString(i));
                    break; * /
                case TypeAffinity.Blob:
                    var blob = reader.GetBlob(i, true);
                    int size = blob.GetCount();
                    byte[] buffer = new byte[size];
                    blob.Read(buffer, size, 0);
                    L.PushBuffer(buffer);
                    break;
                default:
                    L.PushString(reader.GetString(i));
                    break;
            }
        }
        reader.Close();
        return cols;
    }

    SQLiteCommand GenStatement(Lua L, int query, string statement)
    {
        // no table? then fuck off
        if (L.IsNil(query))
        {
            SQLiteCommand cmd = new(statement + ";", db);
            return cmd;
        }
        L.CheckType(query, LuaType.Table);
        List<string> keys = [];
        // this is effectively `for k,v in pairs(tbl) do ... end`
        L.PushNil();
        while (L.Next(query))
        {
            if (L.IsString(-2))
            {
                string key = L.ToString(-2);
                switch (L.Type(-1))
                {
                    case LuaType.String:
                        //keys.Add($"{key}='@{key}'");
                        //break;
                    case LuaType.Number:
                    case LuaType.Boolean:
                        keys.Add($"{key}=@{key}");
                        break;
                    default:
                        L.Error($"Cannot serialzie type {L.Type(-1)}!");
                        break;
                }
            } else
            {
                L.Error($"Invalid key type {L.Type(-2)}");
            }

            L.Pop(1);
        }
        L.Pop(1); // we don't need that key anymore

        string _rtv = statement + " WHERE " + string.Join(" AND ", keys) + " LIMIT 1;";
        SQLiteCommand rtv = new(_rtv, db);

        // wanna see me do it again?
        foreach (string key in keys)
        {
            L.PushString(key);
            L.GetTable(query);
            switch (L.Type(-1))
            {
                case LuaType.String:
                    rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.String) // yeah this looks horirble
                    {
                        Value = L.ToString(-1)
                    });
                    break;
                case LuaType.Number:
                    if (L.IsInteger(-1))
                    {
                        rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Int64)
                        {
                            Value = L.ToInteger(-1)
                        });
                    } else
                    {
                        rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Double)
                        {
                            Value = L.ToNumber(-1)
                        });
                    }
                    break;
                case LuaType.Boolean:
                    rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Boolean)
                    {
                        Value = L.ToBoolean(-1)
                    });
                    break;
            }
            L.Pop(1);
        }
        L.Pop(1);
        return rtv;
    }

    int Select(Lua L)
    {
        string table = L.CheckString(2);
        var query = GenStatement(L, 3, $"SELECT * FROM '@DBTBL");
        query.Parameters.Add(new SQLiteParameter("@DBTBL", System.Data.DbType.String)
        {
            Value = table
        });
        var reader = query.ExecuteReader();
        return PushResultTuple(L, reader);
    }

    int Insert(Lua L)
    {
        return 0;
    }

    int Update(Lua L)
    {
        return 0;
    }

    int Delete(Lua L)
    {
        return 0;
    }

    int CreateTable(Lua L)
    {
        return 0;
    }

    int DeleteTable(Lua L)
    {
        return 0;
    }

    int Capacity(Lua L)
    {
        return 0;
    }

    int Usage(Lua L)
    {
        return 0;
    }

    int Query(Lua L)
    {
        return 0;
    } */

    public override void Destroy()
    {
        throw new NotImplementedException();
    }
}