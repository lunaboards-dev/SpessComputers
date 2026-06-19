using System.Net;
using spesscore.VM;

namespace spesscore;

class SpessCore
{
    List<Computer> Computers = [];
    List<Object> PendingCalls = [];
    HttpListener http = new();
}