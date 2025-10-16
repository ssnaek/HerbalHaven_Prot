using UnityEngine;
using UnityEngine.UI;

public class PlayerSpeedUI : MonoBehaviour
{
    public PlayerController player;   // reference to your player
    public Text speedText;            // UI Text component

    void Update()
    {
        if (player != null && speedText != null)
        {
            speedText.text = "Speed: " + player.CurrentSpeed.ToString("F2") + " u/s";
        }
    }
}
