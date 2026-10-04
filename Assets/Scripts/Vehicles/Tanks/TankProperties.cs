using UnityEngine;
using UnityEngine.Serialization;

public class TankProperties : MonoBehaviour
{
    [Header("Hp")]
    public float hp;
    public float resistance;
    
    [Header("Movement")]
    [Min(0f), Tooltip("Unidades de throttle por segundo ate atingir a velocidade desejada.")]
    public float acceleration;
    [Min(0f), Tooltip("Velocidade maxima de avanco ou marcha a re, em unidades por segundo, sem boost.")]
    public float max_throttle;
    [Min(0f), Tooltip("Torque maximo aplicado a cada roda; nao altera a velocidade alvo.")]
    public float wheelMotorTorque = 10f;
    public float rotation_value;
    public float max_rotation_speed;

    [Header("Engine Audio")]
    public SoundManager.SoundComponents engineSound;
    [Min(0.01f)] public float minEnginePitch = 0.65f;
    [Min(0.01f)] public float maxEnginePitch = 1.4f;
}
