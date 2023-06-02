using System.Collections.Generic;

using UnityEngine;

[CanSaveNLoad(PathName: "Player")]
public class Player
{
    public string name;
    public int level;

    public float health;
    public float mana;

    public List<Item> items;
}

[CanSaveNLoad(PathName: "Item")]
public class Item
{
    public string name;
    public int count;
}

public class TestScript : MonoBehaviour
{
    [SerializeField] private string playerName;

    public string[] itemPreset;

    private Player player;

    public bool DestroyData;
    
    void Start()
    {
        player = SaveNLoad.Load<Player>(playerName);
        if (player == null)
            player = CreatePlayer(playerName);

        PrintPlayerInfo(player);

        if (DestroyData)
            DestroyPlayer(player);
    }

    private void DestroyPlayer(Player player)
    {
        SaveNLoad.Delete<Player>(player.name);
    }

    private void PrintPlayerInfo(Player player)
    {
        Debug.Log($"Player: {player.name} ({player.level})");
        Debug.Log($"Health/Mana: {player.health} / {player.mana}");

        Debug.Log($"Items: {player.items.Count}");
        foreach (Item item in player.items)
            Debug.Log($"\t{item.name}: {item.count}");
    }

    private Player CreatePlayer(string playerName)
    {
        Player player = new Player();

        player.name = playerName;

        player.level = Random.Range(1, 100);

        player.health = Random.Range(10, 100);
        player.mana = Random.Range(10, 100);

        player.items = new List<Item>();

        int numberOfItem = Random.Range(0, itemPreset.Length + 1);

        List<string> itemList = new List<string>(itemPreset);
        for (int i = 0; i < numberOfItem; i++) {
            int index = Random.Range(0, itemList.Count);

            Item item = new Item();

            item.name = itemList[index];
            item.count = Random.Range(1, 16 + 1);

            player.items.Add(item);

            itemList.RemoveAt(index);
        }

        SaveNLoad.Save(player, playerName);

        return player;
    }
}