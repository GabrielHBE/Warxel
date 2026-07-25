using FishNet.Object;
using UnityEngine;

public class VoxelFullCollapse : VoxelObj
{
    [SerializeField] private Animator animator;
    private VoxelFullCollapseTrigger[] voxelFullCollapseTriggers;
    private VoxelPartialCollapse[] voxelPartialCollapses;

    private bool _animationTriggered = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        voxelFullCollapseTriggers = GetComponentsInChildren<VoxelFullCollapseTrigger>();
        voxelPartialCollapses = GetComponentsInChildren<VoxelPartialCollapse>();
    }

    void Update()
    {
        if(!IsServerInitialized) return;

        // Verifica se já foi acionado para evitar repetição
        if (_animationTriggered) return;
        
        // Verifica se todos os triggers estão ativados
        if (AllTriggersActivated())
        {
            CollapseAllVoxelPartialCollapse();
            TriggerAnimation();
            _animationTriggered = true;
        }
    }

    private bool AllTriggersActivated()
    {
        // Verifica se a lista está vazia
        if (voxelFullCollapseTriggers == null || voxelFullCollapseTriggers.Length == 0)
            return false;

        // Verifica cada trigger
        foreach (var trigger in voxelFullCollapseTriggers)
        {
            if (trigger == null || !trigger.isTrigged)
                return false;
        }

        return true;
    }

    [ObserversRpc]
    private void TriggerAnimation()
    {
        if (animator != null)
        {
            // Método 1: Trigger
            animator.SetTrigger("FullCollapse");
            
            // Método 2: Bool (alternativa)
            // animator.SetBool("FullCollapsed", true);
        }
        else
        {
            Debug.LogWarning("Animator não atribuído em VoxelFullCollapse!");
        }
    }

    [Server]
    private void CollapseAllVoxelPartialCollapse()
    {
        foreach(VoxelPartialCollapse partial in voxelPartialCollapses)
        {
            partial.Destroy();
        }
    }

    // Método para resetar o estado se necessário (opcional)
    [Server]
    public void ResetFullCollapse()
    {
        _animationTriggered = false;
        
        // Opcional: Resetar todos os triggers também
        foreach (var trigger in voxelFullCollapseTriggers)
        {
            if (trigger != null)
                trigger.ResetCollapse(); // Você precisará adicionar este método em VoxelFullCollapseTrigger
        }
    }
}