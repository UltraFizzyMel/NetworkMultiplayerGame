using System.Collections.Generic;

public static class PlayerRegistry
{
    public static List<Player> Players = new List<Player>();

    public static void Register(Player p)
    {
        if (!Players.Contains(p))
            Players.Add(p);
    }

    public static void Unregister(Player p)
    {
        if (Players.Contains(p))
            Players.Remove(p);
    }
}
