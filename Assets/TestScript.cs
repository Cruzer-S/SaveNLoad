using UnityEngine;

using TMPro;

public class TestScript : MonoBehaviour
{
    [SerializeField] private PlayerInformationPanel panel;

    private PlayerManager playerManager;

    public void CreatePlayer(TMP_InputField inputField)
    {
        Player player = Player.CreateTempPlayer(inputField.text);

        playerManager.AddPlayer(player);

        panel.Set(player);
    }

    public void DestroyPlayer(TMP_InputField inputField)
    {
        panel.Clear();

        if ( !playerManager.RemovePlayer(inputField.text) )
            panel.ShowErrorMessage("Failed to destroy player!");
    }

    public void LoadPlayer(TMP_InputField inputField)
    {
        Player player = playerManager.GetPlayer(inputField.text);

        panel.Clear();
        if (player == null) {
            panel.ShowErrorMessage("Failed to load player!");
            return ;
        }

        panel.Set(player);
    }

    void Start()
    {
        playerManager = PlayerManager.Instantiate();
    }
}