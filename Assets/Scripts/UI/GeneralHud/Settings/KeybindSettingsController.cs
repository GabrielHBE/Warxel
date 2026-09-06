using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class KeybindSettingsView
{
    public IReadOnlyDictionary<string, TextMeshProUGUI> Buttons;
    public GameObject ErrorPanel;
    public Button CloseErrorButton;
    public TextMeshProUGUI ErrorText;
}

internal sealed class KeybindSettingsController
{
    private const string PreferencePrefix = "KeyBind_";

    private readonly KeyBinds model;
    private readonly KeybindSettingsView view;
    private readonly ISettingsStore store;
    private readonly Dictionary<string, KeyCode> defaults = new Dictionary<string, KeyCode>();
    private readonly FieldInfo[] keyFields;

    private string currentAction;
    private KeyCode confirmationKey = KeyCode.None;
    private bool isWaitingForKey;

    public KeybindSettingsController(KeyBinds model, KeybindSettingsView view, ISettingsStore store)
    {
        this.model = model;
        this.view = view;
        this.store = store;
        keyFields = model.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
    }

    public void Load()
    {
        CaptureDefaults();

        for (int i = 0; i < keyFields.Length; i++)
        {
            FieldInfo field = keyFields[i];
            if (field.FieldType != typeof(KeyCode))
            {
                continue;
            }

            KeyCode key = (KeyCode)field.GetValue(model);
            string preferenceKey = PreferencePrefix + field.Name;

            if (store.HasKey(preferenceKey) &&
                Enum.TryParse(store.GetString(preferenceKey), out KeyCode storedKey))
            {
                key = storedKey;
                field.SetValue(model, key);
            }

            UpdateButton(field.Name, key);
        }
    }

    public void Tick()
    {
        if (!isWaitingForKey) return;
        

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key) || key == KeyCode.Escape)  continue;
            

            if (key == KeyCode.Mouse0 && IsPointerOverCloseButton())
            {
                Cancel();
                return;
            }

            ProcessCandidate(key);
            return;
        }
    }

    public void Start(string actionName)
    {
        FieldInfo field = GetKeyField(actionName);
        if (field == null)
        {
            Debug.LogWarning($"Keybind action '{actionName}' was not found.");
            return;
        }

        currentAction = actionName;
        confirmationKey = KeyCode.None;
        isWaitingForKey = true;
        HideError();

        if (view.Buttons.TryGetValue(actionName, out TextMeshProUGUI button) && button != null) button.text = "Press any key...";
        
    }

    public void Cancel()
    {
        isWaitingForKey = false;
        confirmationKey = KeyCode.None;
        HideError();

        if (!string.IsNullOrEmpty(currentAction)) UpdateButton(currentAction, GetCurrentKey(currentAction));
        

        currentAction = string.Empty;
    }

    public void ResetToDefaults()
    {
        Cancel();
        CaptureDefaults();

        foreach (KeyValuePair<string, KeyCode> binding in defaults)
        {
            Apply(binding.Key, binding.Value);
            Save(binding.Key, binding.Value);
        }

        store.Save();
    }

    public void SaveAll()
    {
        for (int i = 0; i < keyFields.Length; i++)
        {
            FieldInfo field = keyFields[i];
            if (field.FieldType == typeof(KeyCode)) Save(field.Name, (KeyCode)field.GetValue(model));
            
        }
    }

    private void ProcessCandidate(KeyCode key)
    {
        bool duplicate = IsAlreadyUsed(key, currentAction);
        if (duplicate && confirmationKey != key)
        {
            confirmationKey = key;
            ShowError($"Key {key} is already in use!\nPress it again to confirm");
            return;
        }

        Apply(currentAction, key);
        Save(currentAction, key);
        store.Save();

        isWaitingForKey = false;
        confirmationKey = KeyCode.None;
        currentAction = string.Empty;
        HideError();
    }

    private bool IsAlreadyUsed(KeyCode key, string ignoredAction)
    {
        for (int i = 0; i < keyFields.Length; i++)
        {
            FieldInfo field = keyFields[i];
            if (field.FieldType == typeof(KeyCode) &&field.Name != ignoredAction && (KeyCode)field.GetValue(model) == key) return true;
        }

        return false;
    }

    private void CaptureDefaults()
    {
        if (defaults.Count > 0) return;
        
        for (int i = 0; i < keyFields.Length; i++)
        {
            FieldInfo field = keyFields[i];
            if (field.FieldType == typeof(KeyCode))
            {
                defaults[field.Name] = (KeyCode)field.GetValue(model);
            }
        }
    }

    private void Apply(string actionName, KeyCode key)
    {
        FieldInfo field = GetKeyField(actionName);
        if (field == null) return;
        

        field.SetValue(model, key);
        UpdateButton(actionName, key);
        Debug.Log($"Keybind updated: {actionName} = {key}");
    }

    private KeyCode GetCurrentKey(string actionName)
    {
        FieldInfo field = GetKeyField(actionName);
        return field == null ? KeyCode.None : (KeyCode)field.GetValue(model);
    }

    private FieldInfo GetKeyField(string actionName)
    {
        FieldInfo field = model.GetType().GetField(actionName, BindingFlags.Instance | BindingFlags.Public);
        return field != null && field.FieldType == typeof(KeyCode) ? field : null;
    }

    private void Save(string actionName, KeyCode key) =>
        store.SetString(PreferencePrefix + actionName, key.ToString());

    private void UpdateButton(string actionName, KeyCode key)
    {
        if (view.Buttons.TryGetValue(actionName, out TextMeshProUGUI button) && button != null) button.text = key.ToString();
    }

    private void ShowError(string message)
    {
        if (view.ErrorText != null) view.ErrorText.text = message;
        
        if (view.ErrorPanel != null) view.ErrorPanel.SetActive(true);
    }

    private void HideError()
    {
        if (view.ErrorPanel != null) view.ErrorPanel.SetActive(false);
    }

    private bool IsPointerOverCloseButton()
    {
        if (view.CloseErrorButton == null || EventSystem.current == null) return false;
        

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject == view.CloseErrorButton.gameObject) return true;
        }

        return false;
    }
}