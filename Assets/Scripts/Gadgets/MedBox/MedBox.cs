using System.Collections.Generic;
using UnityEngine;

public class MedBox : Gadget
{
    [Header("Heal properties")]
    [SerializeField] private float heal_rate;
    [SerializeField] private float heal_distance;

    [Header("Settings")]
    [SerializeField] private Vector3 original_pos;
    [SerializeField] private Quaternion original_rot;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float doubleClickTime = 0.3f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject circleVisualPrefab; // Opcional: prefab com LineRenderer
    [SerializeField] private Material circleMaterial;
    [SerializeField] private Color circleColor = Color.green;
    [SerializeField] private float circleWidth = 0.1f;


    public GameObject owner;
    private LineRenderer circleLineRenderer;
    private GameObject circleVisual;
    private PlayerController playerController;
    private KeyBinds keyBinds;
    private float lastAimKeyPressTime;
    private int aimKeyPressCount;
    private bool med_box_thrown;

    private SwitchWeapon switchWeapon;


    void Awake()
    {

        rb.isKinematic = true;
        keyBinds = GameObject.FindGameObjectWithTag("Settings").GetComponent<KeyBinds>();
        playerController = GetComponentInParent<PlayerController>();
        switchWeapon = gameObject.GetComponentInParent<SwitchWeapon>();
        owner = playerController.gameObject;
    }

    void Update()
    {
        if (is_active)
        {
            gadgetComponents.left_hand.transform.position = transform.position;
            gadgetComponents.right_hand.transform.position = transform.position;

            if (!med_box_thrown)
            {

                if (Input.GetKey(keyBinds.WEAPON_shootKey))
                {
                    SelfHeal();
                }

                // Verifica duplo clique para a tecla de mirar
                if (Input.GetKeyDown(keyBinds.WEAPON_aimKey))
                {
                    CheckForDoubleClick();
                }

                // Mantém a funcionalidade normal de curar outros enquanto segura a tecla
                if (Input.GetKey(keyBinds.WEAPON_aimKey) && aimKeyPressCount < 2)
                {
                    HealOthers();
                }
            }
        }


        if(med_box_thrown)
        {
            if (Input.GetKey(keyBinds.PLAYER_interactKey))
            {
                Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 10) && hit.transform.gameObject == owner)
                {
                    PickUp();
                }
            }

            UpdateHealingCirclePosition();
            Collider[] overlappedColliders = Physics.OverlapSphere(transform.position, heal_distance);

            foreach (Collider overlappedCollider in overlappedColliders)
            {
                // Verifica se o collider sobreposto tem PlayerProperties
                PlayerController props = overlappedCollider.GetComponent<PlayerController>();

                if (props != null)
                {
                    // Aplica regeneração
                    props.Regenerate(heal_rate * Time.deltaTime);
                    Debug.Log($"Aplicando regeneração em {overlappedCollider.gameObject.name}");
                }
            }
        }


    }

    private void PickUp()
    {
        Destroy(circleVisual);
        rb.isKinematic = true;
        transform.SetParent(switchWeapon.gadgets_parent);
        transform.localPosition = original_pos;
        transform.localRotation = original_rot;
        med_box_thrown = false;
    }

    private void CreateHealingCircle()
    {
        // Cria um objeto para o círculo
        circleVisual = new GameObject("HealingCircle");
        circleVisual.transform.position = Vector3.zero;

        // Adiciona e configura o LineRenderer
        circleLineRenderer = circleVisual.AddComponent<LineRenderer>();
        circleLineRenderer.positionCount = 33; // 32 segmentos + 1 para fechar o círculo
        circleLineRenderer.loop = true;
        circleLineRenderer.startWidth = circleWidth;
        circleLineRenderer.endWidth = circleWidth;
        circleLineRenderer.material = circleMaterial ?? new Material(Shader.Find("Sprites/Default"));
        circleLineRenderer.startColor = circleColor;
        circleLineRenderer.endColor = circleColor;

        // Faz o círculo sempre olhar para cima
        circleVisual.transform.rotation = Quaternion.Euler(90, 0, 0); // Rotaciona para ficar horizontal
    }

    private void UpdateHealingCirclePosition()
    {
        if (circleVisual == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            // Posiciona o círculo no chão
            circleVisual.transform.position = hit.point + Vector3.up * 0.05f;

            // Atualiza os pontos do círculo
            Vector3[] points = new Vector3[33];
            for (int i = 0; i <= 32; i++)
            {
                float angle = (i / 32f) * Mathf.PI * 2;
                // Os pontos são relativos ao objeto, então usamos Vector3.right/forward
                points[i] = new Vector3(Mathf.Sin(angle) * heal_distance, 0, Mathf.Cos(angle) * heal_distance);
            }
            circleLineRenderer.SetPositions(points);

            // Ativa o círculo
            circleVisual.SetActive(true);
        }
        else
        {
            // Se não encontrar chão, desativa o círculo
            circleVisual.SetActive(false);
        }
    }

    private void CheckForDoubleClick()
    {
        float timeSinceLastPress = Time.time - lastAimKeyPressTime;

        if (timeSinceLastPress <= doubleClickTime)
        {
            // Duplo clique detectado
            aimKeyPressCount++;

            if (aimKeyPressCount == 2)
            {
                ThrowMedBox();
                aimKeyPressCount = 0; // Reseta o contador após executar
            }
        }
        else
        {
            // Primeiro clique ou clique após muito tempo
            aimKeyPressCount = 1;
        }

        lastAimKeyPressTime = Time.time;
    }

    private void SelfHeal()
    {
        playerController.Regenerate(Time.deltaTime * heal_rate);
    }

    private void HealOthers()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Corrigindo a máscara de layer - LayerMask.NameToLayer retorna um índice, não uma máscara
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1)
        {
            Debug.LogError("Layer 'Player' não encontrada!");
            return;
        }

        int layerMask = 1 << playerLayer;

        if (Physics.Raycast(ray, out hit, heal_distance, layerMask))
        {
            PlayerController pc = hit.transform.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.Regenerate(Time.deltaTime * heal_rate);
            }
        }
    }

    private void ThrowMedBox()
    {
        CreateHealingCircle();
        med_box_thrown = true;
        rb.isKinematic = false;

        transform.SetParent(null);

        rb.AddForce(playerController.transform.forward * 20 * rb.mass, ForceMode.Impulse);

    }

}