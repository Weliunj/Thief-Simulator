using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(fileName = "PlayerManager", menuName = "ScriptableObjects/PlayerManager", order = 1)]
public class PlayerManager : ScriptableObject
{
    public float MoveSpeed = 2f;
    public float SprintSpeed = 7f;
    public int Maxweight = 100;
    public int currweight = 0;

    [HideInInspector]
    public float _MoveSpeed = 2.0f;
    [HideInInspector]
    public float _SprintSpeed = 7.0f;
 
}
