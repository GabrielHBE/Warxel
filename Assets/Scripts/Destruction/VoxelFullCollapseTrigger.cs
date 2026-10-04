using FishNet.Object;
using UnityEngine;

public class VoxelFullCollapseTrigger : VoxelPartialCollapse
{
    [HideInInspector] public bool isTrigged;

    protected override void ApplyCollapseState()
    {
        base.ApplyCollapseState();
        isTrigged = IsDestroyed;
    }
}
