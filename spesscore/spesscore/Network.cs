namespace spesscore;

class Network(int i)
{
    int id = i;
    public int ID => id;
    public Dictionary<string, NetworkCard> Cards = [];
    public List<NetworkCard> Snoopers = [];

    public void Broadcast(string src, short port, byte[] data)
    {
        foreach (var card in Cards)
        {
            
        }
    }

    public void Send(string dst, string src, short port, byte[] data)
    {
        if (Cards.TryGetValue(dst, out NetworkCard? card))
        {
            
        }
        foreach (var snoop in Snoopers)
        {
            
        }
    }

    void SendTo(NetworkCard card, string src, short port, byte[] data)
    {
        
    }
}