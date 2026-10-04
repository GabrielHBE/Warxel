using UnityEngine;
using UnityEngine.Serialization;

public class JetProperties : MonoBehaviour
{

    [Header("Hp")]
    public float hp;
    public float resistance;

    [Header("Movement")]
    public float aceleration;
    public float max_throttle;
    [Tooltip("Aceleracao angular maxima de rolagem (rad/s²) com entrada completa.")]
    public float rotation_value;
    [Tooltip("Movimento do mouse necessario para atingir a aceleracao maxima de rolagem; nao limita a velocidade angular.")]
    public float max_rotation_value;
    [Tooltip("Aceleracao angular maxima de pitch (rad/s²) com entrada completa.")]
    public float pitch_value;
    [Tooltip("Movimento do mouse necessario para atingir a aceleracao maxima de pitch; nao limita a velocidade angular.")]
    public float max_pitch_value;
    [Tooltip("Aceleracao angular de yaw (rad/s²) por unidade de entrada.")]
    public float lean_value;
    [Min(0f)] public float max_lean_speed;
    public float dive_speed_boost = 50f;

    [Header("Thrust Vectoring")]
    public bool canThrustVector;
    [Min(0f), Tooltip("Velocidade minima do Rigidbody para iniciar e manter o vetoramento.")]
    public float thrustVectorMinSpeed = 150f;
    [Min(0f), Tooltip("Velocidade em que o vetoramento atinge sua forca total.")]
    public float thrustVectorFullStrengthSpeed = 350f;
    [Min(0.1f), Tooltip("Duracao maxima de cada manobra de vetoramento, em segundos.")]
    public float thrustVectorMaxDuration = 1.5f;
    [Min(0f), Tooltip("Tempo de espera entre manobras de vetoramento, em segundos.")]
    public float thrustVectorCooldown = 3f;
    [Min(0f), Tooltip("Velocidade angular desejada durante o vetoramento, em radianos por segundo.")]
    public float thrustVectorTurnRate = 3f;
    [Min(0f), Tooltip("Aceleracao angular maxima do vetoramento, em radianos por segundo ao quadrado.")]
    public float thrustVectorAngularAcceleration = 18f;
    [Min(0f), Tooltip("Rapidez com que a rotacao acompanha o comando do piloto.")]
    public float thrustVectorResponse = 10f;
    [Min(0f), Tooltip("Perda proporcional de velocidade por segundo durante o vetoramento.")]
    public float thrustVectorSpeedLoss = 1f;
    [Range(0f, 1f), Tooltip("Fracao da propulsao para frente mantida durante o vetoramento.")]
    public float thrustVectorForwardThrust = 0.1f;

    [Header("Sonic Boom")]
    [Tooltip("Velocidade do Rigidbody em unidades por segundo para disparar o efeito. Zero desativa o Sonic Boom.")]
    public SoundManager.SoundComponents sonicBoomSound;
    public GameObject sonicBoomEffectPrefab;
    [Min(0.1f)] public float sonicBoomEffectDuration = 5f;

    [Header("Sounds")]
    public float minTurbinePitch;
    public float maxTurbinePitch;
    public SoundManager.SoundComponents interiorTurbineSound;
    public SoundManager.SoundComponents  exteriorTurbineSound;
}
