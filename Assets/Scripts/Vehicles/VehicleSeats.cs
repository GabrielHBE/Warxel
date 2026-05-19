using System;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[Serializable]
public class VehicleSeats
{
    [Header("Seat Configuration")]
    public SeatType seatType;
    public Camera playerCamera;
    public GameObject seatHUD;

    [Header("Seat Armory")]
    public GameObject[] vehicleArmory;
    public IVehicleArmory currentArmory;

    [Header("Player Hand IK Targets")]
    public Transform vehicleLeftHandTarget;
    public Transform vehicleRightHandTarget;

    [Header("Runtime References (Auto-assigned)")]
    public bool isOccupied;
    public PlayerProperties playerProperties;
    public PlayerController playerController;
    public Rigidbody playerRigidbody;
    public GameObject playerGameObject;
    public PlayerAnimation playerAnimation;


    [Header("Transform References")]
    public Transform playerSeat;

    [NonSerialized] public NetworkConnection authorizedConnection;

    public void EnterSeat(PlayerProperties playerProperties, PlayerController playerController, Transform playerSeat, Rigidbody playerRigidbody, GameObject playerGameObject)
    {
        if (vehicleArmory != null && vehicleArmory.Length > 0)
        {

            currentArmory = vehicleArmory[0].GetComponent<IVehicleArmory>();
            currentArmory.ActivateArmory();
        }

        this.playerProperties = playerProperties;
        this.playerController = playerController;
        this.playerRigidbody = playerRigidbody;
        this.playerGameObject = playerGameObject;
        this.playerSeat = playerSeat;
        playerCamera = playerController.playerCamera;
        this.playerProperties.is_in_vehicle = true;
        playerAnimation = playerController.playerAnimation;

        if (playerAnimation != null) playerAnimation.SetVehicleIKTargets(vehicleLeftHandTarget, vehicleRightHandTarget);

        this.playerRigidbody.isKinematic = true;
        this.playerRigidbody.interpolation = RigidbodyInterpolation.None;
        this.playerProperties.is_reloading = false;
        this.playerProperties.is_in_vehicle = true;

        if (seatType != SeatType.Passenger)
        {
            playerAnimation.DeactivateCurrentWeapon();
            playerController.first_person_player_components.SetActive(false);
            playerController.soldierHudManager.UpdateItemsVisibility(false);
            playerController.HideOwnerItems(false);
        }
        else
        {
            playerAnimation.ActivateCurrentWeapon();
            playerController.first_person_player_components.SetActive(true);
            playerController.soldierHudManager.UpdateItemsVisibility(true);
            playerController.HideOwnerItems(true);
        }

        if (seatHUD != null)
        {
            if (seatHUD != null) seatHUD.GetComponent<UIElementsColor>().SetColor(Color.limeGreen, 2);
            seatHUD.SetActive(true);
        }

        isOccupied = true;

    }

    public void ExitSeat()
    {
        //Player Animation Exit State
        playerAnimation.ActivateCurrentWeapon();
        if (playerAnimation != null)
        {
           playerAnimation.SetVehicleIKTargets(null, null);
        }

        //Player Controls Exit State
        playerController.first_person_player_components.SetActive(true);
        playerController.soldierHudManager.UpdateItemsVisibility(true);
        playerController.HideOwnerItems(true);
        playerProperties.is_in_vehicle = false;

        //Player GameObject Exit State
        if (!playerGameObject.activeSelf) playerGameObject.SetActive(true);
        playerGameObject.transform.SetParent(null); // Detach the player from the vehicle seat

        //Player Rigidbody Exit State
        playerRigidbody.isKinematic = false;
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        //Camera Exit State
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            playerController.playerCamera.enabled = true; // Enable the player's personal camera when they exit the vehicle seat
            playerController.playerCamera.GetComponent<AudioListener>().enabled = true; // Enable the player's personal audio listener when they exit the vehicle seat
            playerCamera = null;
        }

        ClearReferences();
    }

    public void ClearReferences()
    {
        DeactivateAllArmory();
        if (playerAnimation != null)
        {
            playerAnimation.SetVehicleIKTargets(null, null);
        }

        if (seatHUD != null) seatHUD.SetActive(false);

        playerRigidbody = null;
        playerGameObject = null;
        playerProperties = null;
        playerController = null;
        playerCamera = null;
        isOccupied = false;
    }

    private void DeactivateAllArmory()
    {
        if (vehicleArmory == null) return;

        foreach (GameObject armoryObj in vehicleArmory)
        {
            if (armoryObj != null)
            {
                IVehicleArmory armory = armoryObj.GetComponent<IVehicleArmory>();
                if (armory != null)
                {
                    armory.DeactivateArmory();
                }
            }
        }
    }

    // Verifica se a conexão tem autoridade sobre as armas deste assento
    public bool HasAuthority(NetworkConnection conn)
    {
        return authorizedConnection != null && authorizedConnection == conn;
    }

    // Define a autoridade das armas para uma conexão específica
    public void SetAuthority(NetworkConnection conn)
    {
        authorizedConnection = conn;

        if (vehicleArmory == null) return;

        foreach (GameObject armoryObj in vehicleArmory)
        {
            if (armoryObj != null)
            {
                NetworkObject nObj = armoryObj.GetComponent<NetworkObject>();
                if (nObj != null)
                {   
                
                    if (conn != null)
                    {
                        Debug.Log("Setando autoridade para: " + nObj.gameObject.name);
                        nObj.GiveOwnership(conn);
                    }
                    else
                    {
                        Debug.Log("Removendo autoridade para: " + nObj.gameObject.name);
                        nObj.RemoveOwnership();
                    }
                        
                }
            }
        }
    }


    public enum SeatType
    {
        Pilot,
        Passenger,
        Gunner
    }
}