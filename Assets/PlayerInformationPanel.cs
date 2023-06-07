using UnityEngine;

using UnityEngine.UI;

using TMPro;

public class PlayerInformationPanel : MonoBehaviour
{
    [SerializeField] private new TMP_Text name;
    [SerializeField] private TMP_Text stats;

    [SerializeField] private LayoutGroup inventory;

    [SerializeField] private GameObject itemIcon;

    private void Awake()
    {
        Clear();
    }

    public void Set(Player player)
    {
        Clear();

        name.text = player.name;
        stats.text = $"Level: {player.level}\n" +
                     $"HP: {player.health}\n" +
                     $"MP: {player.mana}";

        foreach (Item item in player.items)
        {
            GameObject icon = Instantiate(itemIcon, inventory.transform);

            icon.GetComponent<ItemIcon>().Set(item);
        }
    }

    public void ShowErrorMessage(string message) => name.text = message;

    public void Clear()
    {
        name.text = string.Empty;
        stats.text = string.Empty;

        foreach (Transform transform in inventory.transform)
            GameObject.Destroy(transform.gameObject);
    }
}
