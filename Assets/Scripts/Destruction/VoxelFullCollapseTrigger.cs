using FishNet.Object;
using UnityEngine;
public class VoxelFullCollapseTrigger : VoxelPartialCollapse
{
    [HideInInspector] public bool isTrigged;

    [Server]
    public override void Destroy()
    {
        base.Destroy();
        isTrigged = true;
    }
}