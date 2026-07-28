
using System.Data.SQLite;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM.Peripheral.Disk;

class ManagedDisk : AbstractPeripheral
{
    SQLiteConnection? db;
    int capacity = 0;
    bool can_pragma = false;
    bool read_only = false;
    //bool paused = false;
    public bool IsValid => Computer != null;

    public override Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => methods;

    Dictionary<string, IPeripheral.PeripheralCallback> methods;

    ManagedDisk() : base("disk")
    {
        methods = new()
        {
            {"query", Query}
        };
    }

    public ManagedDisk(uint refid) : this()
    {
        // don't actually do anything until we're assigned an ID.
    }

    public ManagedDisk(uint refid, string static_path) : this()
    {
        // do things
        read_only = true;
        db = new SQLiteConnection($"Data Source={static_path};Mode=ReadOnly;");
        db.Authorize += Authorize;
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

    void Interrupt(ref ProgressEventArgs args)
    {
        args.ReturnCode = SQLiteProgressReturnCode.Interrupt;
        if (Computer != null) {
            Computer.PushSignal(new ()
            {
                Name = "io_fail",
                Sender = ID,
                Valid = true,
                Values = new("timeout")
            });
            Computer.ExitIOWait();
        }
    }

    void Progress(object sender, ProgressEventArgs args)
    {
        args.ReturnCode = SQLiteProgressReturnCode.Continue;

    }

    public override void SetID(string id)
    {
        if (read_only) return;
        if (db != null)
        {
            // close and delete
            db.Close();
            File.Delete(ID+".db");
        }
        db = new SQLiteConnection($"Data Source={id}.db;");
        db.Authorize += Authorize;
        db.Progress += Progress;
        db.ProgressOps = 100;
        can_pragma = true;
        ExecuteNonQuery($"PRAGMA page_size=512; PRAGMA max_page_count={capacity*2};");
        can_pragma = false;
        //if (!Config.NoCreateDefaultTables);
            /* ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS FileMetadata (
                Inode INTEGER PRIMARY KEY AUTOINCREMENT,
                Parent INTEGER,
                Filename STRING NOT NULL,
                User INTEGER NOT NULL,
                Group INTEGER NOT NULL,
                Permissions INTEGER NOT NULL,
                Size INTEGER NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS FileData (
                Inode INTEGER NOT NULL,
                Block INTEGER NOT NULL,
                Data BLOB NOT NULL,

                FOREIGN KEY (Inode) REFERENCES FileMetadata(Inode)
            );"); */
    }

    SQLiteCommand GenStatement(lua_State L, int query, string statement)
    {
        // no table? then fuck off
        if (lua_isnil(L, query))
        {
            SQLiteCommand cmd = new(statement + ";", db);
            return cmd;
        }
        luaL_checktype(L, query, LUA_TTABLE);
        List<string> keys = [];
        // this is effectively `for k,v in pairs(tbl) do ... end`
        lua_pushnil(L);
        while (lua_next(L, query))
        {
            if (lua_isstring(L, -2))
            {
                string key = lua_tostring(L, -2);
                switch (lua_type(L, -1))
                {
                    case LUA_TSTRING:
                        //keys.Add($"{key}='@{key}'");
                        //break;
                    case LUA_TNUMBER:
                    case LUA_TBOOLEAN:
                        keys.Add($"{key}=@{key}");
                        break;
                    default:
                        luaL_error(L, $"Cannot serialzie type {luaL_typename(L, -1)}!");
                        break;
                }
            } else
            {
                luaL_error(L, $"Invalid key type {luaL_typename(L, -2)}");
            }

            lua_pop(L, 1);
        }
        lua_pop(L, 1); // we don't need that key anymore

        string _rtv = statement + " WHERE " + string.Join(" AND ", keys) + " LIMIT 1;";
        SQLiteCommand rtv = new(_rtv, db);

        // wanna see me do it again?
        foreach (string key in keys)
        {
            lua_pushstring(L, key);
            lua_gettable(L, query);
            switch (lua_type(L, -1))
            {
                case LUA_TSTRING:
                    rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.String) // yeah this looks horirble
                    {
                        Value = lua_tostring(L, -1)
                    });
                    break;
                case LUA_TNUMBER:
                    if (lua_isinteger(L, -1))
                    {
                        rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Int64)
                        {
                            Value = lua_tointeger(L, -1)
                        });
                    } else
                    {
                        rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Double)
                        {
                            Value = lua_tonumber(L, -1)
                        });
                    }
                    break;
                case LUA_TBOOLEAN:
                    rtv.Parameters.Add(new SQLiteParameter("@"+key, System.Data.DbType.Boolean)
                    {
                        Value = lua_toboolean(L, -1)
                    });
                    break;
            }
            lua_pop(L, 1);
        }
        lua_pop(L, 1);
        return rtv;
    }

    async Task ExecuteQuery(SQLiteCommand cmd)
    {
        var reader = await cmd.ExecuteReaderAsync();
        res = (SQLiteDataReader) reader;
        Computer.ExitIOWait();
    }

    int Select(lua_State L)
    {
        string? table = luaL_checkstring(L, 2);
        if (table == null) return 0;
        var query = GenStatement(L, 3, $"SELECT * FROM '@DBTBL");
        query.Parameters.Add(new SQLiteParameter("@DBTBL", System.Data.DbType.String)
        {
            Value = table
        });
        var reader = query.ExecuteReader();
        return QueryReader.Push(L, reader);
    }

    int Insert(lua_State L)
    {
        return 0;
    }

    int Update(lua_State L)
    {
        return 0;
    }

    int Delete(lua_State L)
    {
        return 0;
    }

    int CreateTable(lua_State L)
    {
        return 0;
    }

    int DeleteTable(lua_State L)
    {
        return 0;
    }

    int Capacity(lua_State L)
    {
        return 0;
    }

    int Usage(lua_State L)
    {
        return 0;
    }

    SQLiteDataReader? res;
    int Query(lua_State L)
    {
        string cstr = luaL_checkstring(L, 2);
        var cmd = new SQLiteCommand(cstr, db);
        int expected_args = cstr.Where((c) => c == '?').Count();
        for (int i=3;i<lua_gettop(L); ++i)
        {
            SQLiteParameter param = new();
            switch (lua_type(L, i))
            {
                case LUA_TSTRING:
                    param.Value = lua_tobytebuffer(L, i);
                    break;
                case LUA_TBOOLEAN:
                    param.Value = lua_toboolean(L, i);
                    break;
                case LUA_TNUMBER:
                    if (lua_isinteger(L, i))
                        param.Value = lua_tointeger(L, i);
                    else
                        param.Value = lua_tonumber(L, i);
                    break;
                default:
                    param.Value = null;
                    break;
            }
            cmd.Parameters.Add(param);
        }
        // pad with NULL.
        int missing = expected_args-cmd.Parameters.Count;
        for (int i=0; i<missing;++i)
        {
            cmd.Parameters.Add(new SQLiteParameter()
            {
                Value = null
            });
        }
        _ = ExecuteQuery(cmd);
        return Computer.EnterIOWait((L) =>
        {
            QueryReader.Push(L, res); // this will never be null
            res = null;
            return 1;
        });
    }

    public override void Destroy()
    {
        throw new NotImplementedException();
    }

    // generic fs methods
    int Open(lua_State S)
    {
        return 0;
    }

    
}