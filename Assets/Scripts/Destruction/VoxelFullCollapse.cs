using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class VoxelFullCollapse : VoxelObj
{
    [SerializeField] private Animator animator;
    [Tooltip("Empty discovers VoxelFullCollapseTrigger components in this building.")]
    [SerializeField] private VoxelDestruction[] collapseTriggers;
    [Tooltip("Empty discovers destructible pieces in this building (excluding fragments).")]
    [SerializeField] private VoxelDestruction[] collapseTargets;
    [Tooltip("Zero requires every trigger; otherwise collapse after this many are destroyed.")]
    [Min(0)] [SerializeField] private int requiredDestroyedTriggers;
    [Tooltip("Animator state to restore immediately for clients joining after the collapse.")]
    [SerializeField] private string collapsedAnimatorState = "";

    private readonly SyncVar<bool> collapsed = new SyncVar<bool>();
    private bool initialized;
    private bool visualCollapsed;
    private int initialAnimatorState;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        base.Start();
        if (initialized) return;
        initialized = true;
        if (collapseTriggers == null || collapseTriggers.Length == 0)
            collapseTriggers = GetComponentsInChildren<VoxelFullCollapseTrigger>(true);
        var uniqueTriggers = new HashSet<VoxelDestruction>();
        foreach (VoxelDestruction trigger in collapseTriggers)
            if (trigger != null) uniqueTriggers.Add(trigger);
        collapseTriggers = new List<VoxelDestruction>(uniqueTriggers).ToArray();
        if (collapseTargets == null || collapseTargets.Length == 0)
            collapseTargets = GetComponentsInChildren<VoxelDestruction>(true);
        if (animator != null) initialAnimatorState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        collapsed.OnChange += OnCollapsedChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (VoxelDestruction trigger in collapseTriggers)
        {
            if (trigger == null) continue;
            // A serialized duplicate must not register multiple callbacks.
            trigger.DestroyedOnServer -= OnTriggerDestroyed;
            trigger.DestroyedOnServer += OnTriggerDestroyed;
        }
        EvaluateTriggers();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyAnimation(collapsed.Value, true);
    }

    public override void OnStopServer()
    {
        foreach (VoxelDestruction trigger in collapseTriggers)
            if (trigger != null) trigger.DestroyedOnServer -= OnTriggerDestroyed;
        base.OnStopServer();
    }

    private void OnDestroy() => collapsed.OnChange -= OnCollapsedChanged;
    private void OnTriggerDestroyed(VoxelDestruction trigger) => EvaluateTriggers();

    private void EvaluateTriggers()
    {
        if (!IsServerInitialized || collapsed.Value || collapseTriggers.Length == 0) return;
        int destroyedCount = 0;
        foreach (VoxelDestruction trigger in collapseTriggers)
            if (trigger != null && trigger.IsDestroyed) destroyedCount++;
        int required = requiredDestroyedTriggers <= 0 ? collapseTriggers.Length :
            Mathf.Clamp(requiredDestroyedTriggers, 1, collapseTriggers.Length);
        if (destroyedCount < required) return;

        // Set state before enqueueing targets to prevent cyclic events.
        collapsed.Value = true;
        ApplyAnimation(true, false);
        foreach (VoxelDestruction target in collapseTargets)
            if (!(target is VoxelFragmentedObj)) VoxelDestructionScheduler.Enqueue(target);
    }

    private void OnCollapsedChanged(bool previous, bool next, bool asServer)
    {
        if (!asServer && IsServerInitialized) return;
        ApplyAnimation(next, false);
    }

    private void ApplyAnimation(bool value, bool initial)
    {
        if (animator == null) return;
        if (value && initial && !string.IsNullOrEmpty(collapsedAnimatorState))
            animator.Play(collapsedAnimatorState, 0, 1f);
        else if (value && !visualCollapsed)
            animator.SetTrigger("FullCollapse");
        else if (!value && visualCollapsed)
        {
            animator.ResetTrigger("FullCollapse");
            if (initialAnimatorState != 0) animator.Play(initialAnimatorState, 0, 0f);
        }
        visualCollapsed = value;
    }

    [Server]
    public void ResetFullCollapse()
    {
        collapsed.Value = false;
        ApplyAnimation(false, false);
        foreach (VoxelDestruction target in collapseTargets) ResetPiece(target);
        foreach (VoxelDestruction trigger in collapseTriggers) ResetPiece(trigger);
    }

    private static void ResetPiece(VoxelDestruction piece)
    {
        if (piece is VoxelPartialCollapse partial) partial.ResetCollapse();
        else if (piece is VoxelFragmentDestruction fragmented) fragmented.ResetFragments();
    }
}
