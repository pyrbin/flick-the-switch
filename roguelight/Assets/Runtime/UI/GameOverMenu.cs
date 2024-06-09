using System.Data;
using UnityEngine;

public class GameOverMenu : MonoBehaviour
{
    public TMPro.TMP_Text? Level;
    public TMPro.TMP_Text? Kills;
    public TMPro.TMP_Text? TotGold;

    // Update is called once per frame
    public void SyncData()
    {
        var stats = Game.Instance.Stats;
        Level.text = "level: " + stats.Level;
        Kills.text = "kills: " + stats.TotalKills;
        TotGold.text = "tot. gold: " + stats.TotalGold;
    }
}
