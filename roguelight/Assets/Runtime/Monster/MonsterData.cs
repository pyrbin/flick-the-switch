using JSAM;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Upgrade", menuName = "Scriptable Objects/Monster Data")]
public class MonsterData : ScriptableObject
{
    [SerializeField]
    public float HealthScaling = 1f;
    [SerializeField]
    public float GoalScaling = 1f;
}
