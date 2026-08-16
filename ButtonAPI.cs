using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuasimorphHelloWorld
{
    /// <summary>
    /// Simple wrapper for button data stored with the button.
    /// </summary>
    public class ButtonUserData
    {
        public System.Action Callback { get; set; }
        public string Tooltip { get; set; }
        public object Mode { get; set; }
        public string ExtraData { get; set; }
    }

    /// <summary>
    /// Factory method to create a button with callback and positioning data.
    /// Modelled after QuickGearButton but simplified for general use.
    /// 
    /// Usage example:
    /// CreateButton(parent, "myButton", "Click Me", new Vector2(100f, 50f), 80f, 24f, () => { /* callback */ });
    /// </summary>
    /// <param name="parent">The parent GameObject to attach the button under</param>
    /// <param name="buttonId">Unique identifier for this button (used in naming)</param>
    /// <param name="text">The text/caption to display on the button</param>
    /// <param name="positionOffset">Vector2 offset from base position (null = use parent's center or default location)</param>
    /// <param name="width">Button width in pixels</param>
    /// <param name="height">Button height in pixels</param>
    /// <param name="callback">Action to execute when button is clicked</param>
    /// <param name="tooltip">Optional tooltip text displayed on hover</param>
    /// <param name="buttonMode">Optional mode for special update behavior (can be null)</param>
    /// <param name="extraData">Optional data associated with the button (e.g., mercenary profile ID)</param>
    /// <returns>The created RectTransform for further configuration</returns>
    public static void CreateButton(
        IGameObject parent,
        string buttonId,
        string text,
        Vector2? positionOffset = null,
        float width = 40f,
        float height = 16f,
        System.Action callback = null,
        string tooltip = null,
        object buttonMode = null,
        string extraData = null)
    {
        if (parent == null)
        {
            Debug.LogWarning("[QuasimorphHelloWorld] Parent GameObject is null, cannot create button.");
            return;
        }

        // Create the button GameObject under the parent
        GameObject buttonGO = new GameObject($"Button_{buttonId}");
        buttonGO.transform.SetParent(parent.transform);
        
        // Set up RectTransform for positioning and sizing
        RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
        rectTransform.anchoredPosition3D = Vector3.zero;
        rectTransform.sizeDelta = new Vector2(width, height);
        
        // Apply position offset if provided
        if (positionOffset.HasValue)
        {
            rectTransform.anchoredPosition3D = new Vector3(positionOffset.Value.x, positionOffset.Value.y, 0f);
        }
        
        Debug.Log($"[QuasimorphHelloWorld] Created button: {buttonId} at position {(positionOffset?.ToString() ?? "default")}");
        
        // Store callback and metadata on the GameObject for later retrieval
        var buttonData = new ButtonUserData
        {
            Callback = callback,
            Tooltip = tooltip,
            Mode = buttonMode,
            ExtraData = extraData
        };
        
        // Attach ButtonUserData to the button object (using Traverse pattern from QuickGear)
        // This allows the button to access its stored data during updates or callbacks
        Traverse traverseButton = Traverse.Create(buttonGO);
        var buttonDataField = traverseButton.Field<ButtonUserData>("_buttonData");
        if (buttonDataField != null)
        {
            buttonDataField.Value = buttonData;
        }
        else
        {
            Debug.Log($"[QuasimorphHelloWorld] Could not find _buttonData field on button: {buttonId}");
        }
        
        return rectTransform;
    }

    /// <summary>
    /// Alternative CreateButton overload for VisualPanel-based buttons (requires MGSC.Traverse).
    /// </summary>
    public static void CreateVisualPanelButton(
        IGameObject parent,
        string buttonId,
        string text,
        Vector2? positionOffset = null,
        float width = 40f,
        float height = 16f,
        System.Action callback = null,
        string tooltip = null,
        object buttonMode = null,
        string extraData = null)
    {
        // This would create a VisualPanel instead of a standard GameObject + RectTransform
        // Using the same pattern as QuickGearButton's CreateQuickGearButton method
        
        if (parent == null)
        {
            Debug.LogWarning("[QuasimorphHelloWorld] Parent GameObject is null, cannot create VisualPanel button.");
            return;
        }

        GameObject buttonGO = new GameObject($"VisualPanel_Button_{buttonId}");
        buttonGO.transform.SetParent(parent.transform);
        
        // In production, this would use Traverse to build the actual VisualPanel structure:
        // var visualPanel = Traverse.Create(buttonGO).Field<CommonButton>("visualPanel").Value;
        
        return;
    }
}