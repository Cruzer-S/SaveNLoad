using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

[CanSaveNLoad(pathName: "PlayerManager", bindingFlags: BindingFlags.NonPublic | BindingFlags.Instance)]
public class PlayerManager
{
    private Dictionary<string, Player> players;

    private PlayerManager()
    {
        players = new Dictionary<string, Player>();
    }

    public static PlayerManager Instantiate()
    {
        PlayerManager manager = SaveNLoad.Load<PlayerManager>("PlayerManager");

        if (manager == null)
            manager = new PlayerManager();

        if (manager.players == null)
            manager.players = new Dictionary<string, Player>();

        return manager;
    }

    public ICollection<Player> GetPlayers() => players.Values;
    public int GetNumberOfPlayer() => players.Count;

    public bool AddPlayer(Player player)
    {
        if (players.ContainsKey(player.name))
            return false;

        players.Add(player.name, player);

        SaveNLoad.Save(this, "PlayerManager");

        return true;
    }

    public bool RemovePlayer(string name)
    {
        if (!players.ContainsKey(name))
            return false;

        players.Remove(name);

        SaveNLoad.Save(this, "PlayerManager");

        return true;
    }

    public Player GetPlayer(string name)
    {
        if (!players.ContainsKey(name))
            return null;

        return players[name];
    }
}

[CanSaveNLoad(pathName: "PlayerManager", bindingFlags: BindingFlags.Public | BindingFlags.Instance)]
public class Player
{
    public string name;
    public int level;

    public float health;
    public float mana;

    public List<Item> items;

    private static string[] itemPreset = {
        "Health Potion", "Long Sword", "Dart",
        "Mail Armor", "Enchantment Scroll", "Golden Key"
    };

    public static Player CreateTempPlayer(string name)
    {
        Player player = new Player();

        player.name = name;

        player.level = Random.Range(1, 100);

        player.health = Random.Range(10, 100);
        player.mana = Random.Range(10, 100);

        player.items = new List<Item>();

        int numberOfItem = Random.Range(0, itemPreset.Length + 1);

        List<string> itemList = new List<string>(itemPreset);
        for (int i = 0; i < numberOfItem; i++)
        {
            int index = Random.Range(0, itemList.Count);

            Item item = new Item();

            item.name = itemList[index];
            item.count = Random.Range(1, 16 + 1);

            player.items.Add(item);

            itemList.RemoveAt(index);
        }

        return player;
    }
}