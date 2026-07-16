using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class FlagCapture : NetworkBehaviour
{
    [Header("UI Elements")]
    public Sprite UI_Image;
    public Transform InWorldUIPosition;

    [Header("Configurações da Bandeira")]
    [SerializeField] private LayerMask captureLayers;
    [SerializeField] private float baseCaptureSpeed = 10f;
    [SerializeField] private float maxProgress = 100f;
    [SerializeField] private float assaultClassMultiplier = 1.5f; // Adicionado para facilitar ajustes no Inspector

    [Header("Progresso (Sincronizado)")]
    public readonly SyncVar<float> teamAProgress = new SyncVar<float>();
    public readonly SyncVar<float> teamBProgress = new SyncVar<float>();
    private readonly SyncVar<FactionManager.Faction> currentOwner = new SyncVar<FactionManager.Faction>(FactionManager.Faction.Neutral);

    private List<Collider> entitiesInZone = new List<Collider>();

    private void Update()
    {
        if (!IsServerInitialized) return;
        ProcessCaptureMechanic();
    }

    [Server]
    private void ProcessCaptureMechanic()
    {
        // Limpa entidades destruídas/desconectadas
        entitiesInZone.RemoveAll(col => col == null);

        // Mudamos de 'int' para 'float' para podermos usar casas decimais nos pesos de captura
        float teamACapturePower = 0f;
        float teamBCapturePower = 0f;

        // Conta a "força" de captura na área
        foreach (var col in entitiesInZone)
        {
            var entityFaction = col.GetComponent<EntityFaction>(); 
            if (entityFaction != null)
            {
                float captureWeight = 1f; // Peso padrão (para veículos e outras classes)

                // Verifica se a layer é exatamente a layer "Player"
                if (col.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    // Tenta acessar o PlayerController para checar a classe
                    PlayerProperties playerProperties = col.GetComponent<PlayerProperties>();
                    
                    // Se tiver o script e a classe for Assault, aumenta o peso
                    if (playerProperties != null && playerProperties.selectedClass.Value == ClassManager.Class.Assault)
                    {
                        captureWeight = assaultClassMultiplier; // Aplica o peso 1.5x
                    }
                }

                // Adiciona o peso calculado para a facção correspondente
                if (entityFaction.GetFaction() == FactionManager.Faction.FactionA)
                {
                    teamACapturePower += captureWeight;
                } 
                else if (entityFaction.GetFaction() == FactionManager.Faction.FactionB)
                {
                    teamBCapturePower += captureWeight;
                } 
            }
        }

        // Calcula a vantagem (agora em float)
        float netDifference = teamACapturePower - teamBCapturePower;

        // Como estamos lidando com floats, usamos uma margem muito pequena para verificar empate
        if (Mathf.Abs(netDifference) < 0.01f) return; 

        // A velocidade base é multiplicada pela diferença de "Força de Captura"
        float delta = Mathf.Abs(netDifference) * baseCaptureSpeed * Time.deltaTime;

        // Facção A tem vantagem
        if (netDifference > 0f) 
        {
            if (teamBProgress.Value > 0)
            {
                teamBProgress.Value = Mathf.Max(0, teamBProgress.Value - delta); 
            }
            else
            {
                teamAProgress.Value = Mathf.Min(maxProgress, teamAProgress.Value + delta);
                
                if (teamAProgress.Value >= maxProgress && currentOwner.Value != FactionManager.Faction.FactionA)
                {
                    OnFlagCapture(FactionManager.Faction.FactionA);
                }
            }
        }
        // Facção B tem vantagem
        else 
        {
            if (teamAProgress.Value > 0)
            {
                teamAProgress.Value = Mathf.Max(0, teamAProgress.Value - delta);
            }
            else
            {
                teamBProgress.Value = Mathf.Min(maxProgress, teamBProgress.Value + delta);
                
                if (teamBProgress.Value >= maxProgress && currentOwner.Value != FactionManager.Faction.FactionB)
                {
                    OnFlagCapture(FactionManager.Faction.FactionB);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized) return;

        // Verifica a LayerMask
        if ((captureLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            if (!entitiesInZone.Contains(other))
            {
                entitiesInZone.Add(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServerInitialized) return;

        if (entitiesInZone.Contains(other))
        {
            entitiesInZone.Remove(other);
        }
    }

    private void OnFlagCapture(FactionManager.Faction newOwner)
    {
        currentOwner.Value = newOwner;
        Debug.Log($"Bandeira capturada pela facção: {newOwner}");
    }

    public FactionManager.Faction GetFactionInControl()
    {
        return currentOwner.Value;
    }
}