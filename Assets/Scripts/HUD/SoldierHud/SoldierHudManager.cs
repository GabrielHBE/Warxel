using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SoldierHudManager : MonoBehaviour
{
    //private Settings settings;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private GameObject hud;
    [SerializeField] private Image center_screen_dot;
    [SerializeField] private List<GameObject> itens_to_hide_when_in_vehicle = new List<GameObject>();
    public FireMode fire_mode_hud;
    public SoldierHudMagCounter mag_counter_hud;
    public ScreenBlood screenBlood;
    public SoldierHudHpManager soldierHudHpManager;
    public DeadPlayerHud deadPlayerHud;

    private bool lastVehicleState;
    private bool isInitialized = false;

    void Awake()
    {
        //settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();
        
        // Inicializa o estado anterior
        lastVehicleState = playerProperties.is_in_vehicle;
        
        // Aplica o estado inicial
        UpdateItemsVisibility(!playerProperties.is_in_vehicle);
        
        isInitialized = true;
    }

    void Update()
    {

        // Verifica se o estado do veículo mudou
        if (isInitialized && playerProperties.is_in_vehicle != lastVehicleState)
        {
            // Atualiza a visibilidade dos itens
            UpdateItemsVisibility(!playerProperties.is_in_vehicle);
            
            // Atualiza o estado anterior
            lastVehicleState = playerProperties.is_in_vehicle;
        }

        if (playerProperties.sprinting && !playerProperties.is_in_vehicle)
        {
            center_screen_dot.enabled = true;
        }
        else
        {
            center_screen_dot.enabled = false;
        }
    }

    /// <summary>
    /// Atualiza a visibilidade dos itens que devem ser escondidos no veículo
    /// </summary>
    /// <param name="isInVehicle">Indica se o jogador está dentro de um veículo</param>
    private void UpdateItemsVisibility(bool isInVehicle)
    {
        foreach (GameObject item in itens_to_hide_when_in_vehicle)
        {
            if (item != null)
            {
                item.SetActive(isInVehicle);
            }
        }
    }
}