using UnityEngine;

public class Nozzle : Attatchment
{

    [Header("Changes")]
    public float horizontalRecoilChange;
    public float verticalRecoilChange;
    public float firstShootRecoilChange;
    public float muzzleLightningChange;
    public int muzzleVelocityChange;
    public AudioClip[] shootSoundChange;
    public float shootPithChange;
    public float shootVolumeChange;
    public float volumeChanger;
    public float spreadChange;

    private EquippableItemAudio weaponSounds;

    public override void Initialize()
    {
        base.Initialize();
        weaponSounds = GetComponentInParent<EquippableItemAudio>();
        if (weaponSounds != null && shootSoundChange != null && shootSoundChange.Length > 0) weaponSounds.shootSounds = shootSoundChange;
    }
}
