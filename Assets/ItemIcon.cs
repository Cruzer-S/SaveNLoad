using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

[CanSaveNLoad(pathName: "PlayerManager", bindingFlags: BindingFlags.Public | BindingFlags.Instance)]
public class Item
{
    public string name;
    public int count;
}

public class ItemIcon : MonoBehaviour
{
    [SerializeField] private TMP_Text count;

    public void Set(Item item)
    {
        count.text = item.count.ToString();
        GetComponent<Image>().sprite = null;
    }
}
