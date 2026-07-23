using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConnection", menuName = "Metroidvania/Level Connection")]
public class LevelConnection : ScriptableObject
{
    /// <summary>
    /// Static property to store the active connection identifier between scene transitions.
    /// Preserves state across scene loads.
    /// </summary>
    public static LevelConnection ActiveConnection { get; set; }
}
