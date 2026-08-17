using System;
using System.Linq;
using MGSC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HarmonyLib;

namespace QuasimorphHelloWorld
{
    public static class ArsenalUi
    {
        [HarmonyPatch(typeof(ArsenalScreen), "Configure")]
        public static class ArsenalScreen_Configure_Patch
        {
            public static void Postfix(ArsenalScreen __instance, Mercenary mercenary)
            {
                try
                {
                    if (mercenary == null)
                        return;

                    Debug.Log(
                        "[QuickGear] ArsenalScreen.Configure called for: " + mercenary.ProfileId
                    );

                    GameObject inventoryWindow = Traverse
                        .Create(__instance)
                        .Field<GameObject>("_inventoryWindow")
                        .Value;

                    Transform parent =
                        (inventoryWindow != null)
                            ? inventoryWindow.transform
                            : __instance.transform;

                    var existingButton = parent.Find("QuickRestockButton");
                    if (existingButton != null)
                    {
                        Debug.Log(
                            "[QuickGear] QuickGear buttons already exist; updating for new merc."
                        );
                        UpdateExistingQuickGearButtons(parent, mercenary);
                        return;
                    }

                    Debug.Log(
                        "[QuickGear] Creating QuickRestockButtons in ArsenalScreen under "
                            + parent.name
                    );

                    Vector2 baseLocal = new Vector2(196.3f, 122.8f);
                    var buttonSpecs = new[]
                    {
                        new ButtonSpec(
                            "QuickRestockButton",
                            QuickGearLocalization.Keys.QuickRestockButton,
                            baseLocal + new Vector2(0f, 0f),
                            40f,
                            16f,
                            () =>
                            {
                                try
                                {
                                    QuickGearService.EquipQuickGear(mercenary);
                                }
                                catch (Exception e)
                                {
                                    Debug.Log(
                                        "[QuickGear] Error running Quick Restock: " + e.Message
                                    );
                                }
                            },
                            QuickGearLocalization.Keys.QuickRestockTooltip
                        ),
                        new ButtonSpec(
                            "LoadSavedEquipmentButton",
                            QuickGearLocalization.Keys.LoadEquipmentButton,
                            baseLocal + new Vector2(46f, 0f),
                            40f,
                            16f,
                            () =>
                            {
                                try
                                {
                                    Debug.Log(
                                        "[QuickGear] Load Saved Equipment clicked: loading saved equipment."
                                    );
                                    QuickGearService.LoadSavedEquipment(mercenary);
                                }
                                catch (Exception e)
                                {
                                    Debug.Log(
                                        "[QuickGear] Error loading saved equipment: " + e.Message
                                    );
                                }
                            },
                            QuickGearLocalization.Keys.LoadEquipmentTooltip
                        ),
                        new ButtonSpec(
                            "SaveEquipmentButton",
                            QuickGearLocalization.Keys.SaveEquipmentButton,
                            baseLocal + new Vector2(92f, 0f),
                            40f,
                            16f,
                            () =>
                            {
                                try
                                {
                                    Debug.Log(
                                        "[QuickGear] Save Equipment clicked: saving equipment."
                                    );
                                    QuickGearService.SaveEquipment(mercenary);
                                }
                                catch (Exception e)
                                {
                                    Debug.Log("[QuickGear] Error saving equipment: " + e.Message);
                                }
                            },
                            QuickGearLocalization.Keys.SaveEquipmentTooltip
                        ),
                        new ButtonSpec(
                            "SaveInventoryButton",
                            QuickGearLocalization.Keys.UpdateQuickRestockButton,
                            baseLocal + new Vector2(138f, 0f),
                            40f,
                            16f,
                            () =>
                            {
                                try
                                {
                                    Debug.Log(
                                        "[QuickGear] Save Inventory clicked: saving current inventory to quick equip config."
                                    );
                                    QuickGearService.SaveInventoryQuickGear(mercenary);
                                }
                                catch (Exception e)
                                {
                                    Debug.Log(
                                        "[QuickGear] Error saving inventory quick equip: "
                                            + e.Message
                                    );
                                }
                            },
                            QuickGearLocalization.Keys.UpdateQuickRestockTooltip
                        )
                    };

                    foreach (var spec in buttonSpecs)
                    {
                        AddButton(parent, spec);
                    }

                    AddOrUpdateAugsImplantsToggle(parent, GetAugsImplantsTogglePosition(parent));
                }
                catch (Exception e)
                {
                    Debug.Log("[QuickGear] Exception in ArsenalScreen patch: " + e.Message);
                    Debug.Log("[QuickGear] " + e.StackTrace);
                }
            }

            private sealed class ButtonSpec
            {
                public string ObjectName { get; }
                public string LabelKey { get; }
                public Vector2 AnchoredPosition { get; }
                public float Width { get; }
                public float Height { get; }
                public Action OnClick { get; }
                public string TooltipKey { get; }

                public ButtonSpec(
                    string objectName,
                    string labelKey,
                    Vector2 anchoredPosition,
                    float width,
                    float height,
                    Action onClick,
                    string tooltipKey
                )
                {
                    ObjectName = objectName;
                    LabelKey = labelKey;
                    AnchoredPosition = anchoredPosition;
                    Width = width;
                    Height = height;
                    OnClick = onClick;
                    TooltipKey = tooltipKey;
                }
            }

            private static void AddButton(Transform parent, ButtonSpec spec)
            {
                if (parent.Find(spec.ObjectName) != null)
                    return;

                var buttonObj = new GameObject(
                    spec.ObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );
                buttonObj.layer = LayerMask.NameToLayer("UI");
                buttonObj.transform.SetParent(parent, false);

                var rect = buttonObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = spec.AnchoredPosition;
                rect.sizeDelta = new Vector2(spec.Width, spec.Height);
                rect.localScale = Vector3.one;

                var layout = buttonObj.AddComponent<LayoutElement>();
                layout.preferredWidth = spec.Width;
                layout.preferredHeight = spec.Height;
                layout.minWidth = spec.Width;
                layout.minHeight = spec.Height;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;

                var img = buttonObj.GetComponent<Image>();
                img.color = new Color(25f / 255f, 32f / 255f, 33f / 255f, 0.95f);
                img.type = Image.Type.Sliced;
                img.raycastTarget = true;

                var outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = new Color(79f / 255f, 114f / 255f, 102f / 255f, 0.95f);
                outline.effectDistance = new Vector2(1f, -1f);

                var button = buttonObj.GetComponent<Button>();
                button.targetGraphic = img;
                if (spec.OnClick != null)
                {
                    button.onClick.AddListener(() => spec.OnClick());
                }

                var captionObj = new GameObject(
                    "Caption",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text)
                );
                captionObj.transform.SetParent(buttonObj.transform, false);
                captionObj.layer = buttonObj.layer;
                var captionRect = captionObj.GetComponent<RectTransform>();
                captionRect.anchorMin = Vector2.zero;
                captionRect.anchorMax = Vector2.one;
                captionRect.offsetMin = new Vector2(4f, 4f);
                captionRect.offsetMax = new Vector2(-4f, -4f);

                var txt = captionObj.GetComponent<Text>();
                txt.text = QuickGearLocalization.Get(spec.LabelKey);
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.resizeTextForBestFit = false;
                txt.fontSize = 4;
                txt.raycastTarget = false;

                string localizedTooltipText = QuickGearLocalization.Get(spec.TooltipKey);
                if (!string.IsNullOrEmpty(localizedTooltipText))
                {
                    var tooltip = buttonObj.AddComponent<QuickGearTooltip>();
                    tooltip.TooltipText = localizedTooltipText;
                }
            }

            private static void UpdateExistingQuickGearButtons(
                Transform parent,
                Mercenary mercenary
            )
            {
                void RebindButton(string objectName, Action onClick)
                {
                    var buttonObj = parent.Find(objectName);
                    if (buttonObj == null)
                        return;

                    var button = buttonObj.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                        if (onClick != null)
                            button.onClick.AddListener(() => onClick());
                    }
                }

                RebindButton(
                    "QuickRestockButton",
                    () => QuickGearService.EquipQuickGear(mercenary)
                );
                RebindButton(
                    "LoadSavedEquipmentButton",
                    () =>
                    {
                        Debug.Log(
                            "[QuickGear] Load Saved Equipment clicked: loading saved equipment."
                        );
                        QuickGearService.LoadSavedEquipment(mercenary);
                    }
                );
                RebindButton(
                    "SaveEquipmentButton",
                    () =>
                    {
                        Debug.Log("[QuickGear] Save Equipment clicked: saving equipment.");
                        QuickGearService.SaveEquipment(mercenary);
                    }
                );
                RebindButton(
                    "SaveInventoryButton",
                    () =>
                    {
                        Debug.Log(
                            "[QuickGear] Save Inventory clicked: saving current inventory to quick equip config."
                        );
                        QuickGearService.SaveInventoryQuickGear(mercenary);
                    }
                );

                RefreshLocalizedButtonContent(
                    parent,
                    "QuickRestockButton",
                    QuickGearLocalization.Keys.QuickRestockButton,
                    QuickGearLocalization.Keys.QuickRestockTooltip
                );
                RefreshLocalizedButtonContent(
                    parent,
                    "LoadSavedEquipmentButton",
                    QuickGearLocalization.Keys.LoadEquipmentButton,
                    QuickGearLocalization.Keys.LoadEquipmentTooltip
                );
                RefreshLocalizedButtonContent(
                    parent,
                    "SaveEquipmentButton",
                    QuickGearLocalization.Keys.SaveEquipmentButton,
                    QuickGearLocalization.Keys.SaveEquipmentTooltip
                );
                RefreshLocalizedButtonContent(
                    parent,
                    "SaveInventoryButton",
                    QuickGearLocalization.Keys.UpdateQuickRestockButton,
                    QuickGearLocalization.Keys.UpdateQuickRestockTooltip
                );

                AddOrUpdateAugsImplantsToggle(parent, GetAugsImplantsTogglePosition(parent));
            }

            private static void RefreshLocalizedButtonContent(
                Transform parent,
                string objectName,
                string labelKey,
                string tooltipKey
            )
            {
                var buttonObj = parent.Find(objectName);
                if (buttonObj == null)
                    return;

                var caption = buttonObj.Find("Caption");
                if (caption != null)
                {
                    var text = caption.GetComponent<Text>();
                    if (text != null)
                    {
                        text.text = QuickGearLocalization.Get(labelKey);
                    }
                }

                var tooltip = buttonObj.GetComponent<QuickGearTooltip>();
                if (tooltip != null)
                {
                    tooltip.TooltipText = QuickGearLocalization.Get(tooltipKey);
                }
            }

            private static void AddOrUpdateAugsImplantsToggle(
                Transform parent,
                Vector2 anchoredPosition
            )
            {
                var toggleObj = parent.Find("AugsImplantsToggle");
                if (toggleObj == null)
                {
                    var root = new GameObject(
                        "AugsImplantsToggle",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Toggle)
                    );
                    root.layer = LayerMask.NameToLayer("UI");
                    root.transform.SetParent(parent, false);

                    var rootRect = root.GetComponent<RectTransform>();
                    rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                    rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    rootRect.pivot = new Vector2(0.5f, 0.5f);
                    rootRect.anchoredPosition = anchoredPosition;
                    rootRect.sizeDelta = new Vector2(74f, 14f);
                    rootRect.localScale = Vector3.one;

                    var background = new GameObject(
                        "Background",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );
                    background.layer = root.layer;
                    background.transform.SetParent(root.transform, false);
                    var bgRect = background.GetComponent<RectTransform>();
                    const float checkboxX = 0f;
                    const float checkboxSize = 10f;
                    bgRect.anchorMin = new Vector2(0f, 0.5f);
                    bgRect.anchorMax = new Vector2(0f, 0.5f);
                    bgRect.pivot = new Vector2(0f, 0.5f);
                    bgRect.anchoredPosition = new Vector2(checkboxX, 0f);
                    bgRect.sizeDelta = new Vector2(checkboxSize, checkboxSize);
                    var bgImage = background.GetComponent<Image>();
                    bgImage.color = new Color(25f / 255f, 32f / 255f, 33f / 255f, 0.95f);

                    var checkmark = new GameObject(
                        "Checkmark",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );
                    checkmark.layer = root.layer;
                    checkmark.transform.SetParent(background.transform, false);
                    var checkRect = checkmark.GetComponent<RectTransform>();
                    checkRect.anchorMin = Vector2.zero;
                    checkRect.anchorMax = Vector2.one;
                    checkRect.offsetMin = new Vector2(2f, 2f);
                    checkRect.offsetMax = new Vector2(-2f, -2f);
                    var checkImage = checkmark.GetComponent<Image>();
                    checkImage.color = new Color(120f / 255f, 181f / 255f, 120f / 255f, 1f);

                    var label = new GameObject(
                        "Label",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Text)
                    );
                    label.layer = root.layer;
                    label.transform.SetParent(root.transform, false);
                    var labelRect = label.GetComponent<RectTransform>();
                    labelRect.anchorMin = new Vector2(0f, 0f);
                    labelRect.anchorMax = new Vector2(1f, 1f);
                    float labelLeft = checkboxX + checkboxSize + 4f;
                    labelRect.offsetMin = new Vector2(labelLeft, 0f);
                    labelRect.offsetMax = new Vector2(0f, 0f);
                    var labelText = label.GetComponent<Text>();
                    labelText.text = QuickGearLocalization.Get(
                        QuickGearLocalization.Keys.ToggleAugsImplantsLabel
                    );
                    labelText.alignment = TextAnchor.MiddleLeft;
                    labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    labelText.color = Color.white;
                    labelText.fontSize = 5;
                    labelText.raycastTarget = false;

                    var toggle = root.GetComponent<Toggle>();
                    toggle.targetGraphic = bgImage;
                    toggle.graphic = checkImage;

                    var tooltip = root.AddComponent<QuickGearTooltip>();
                    tooltip.TooltipText = QuickGearLocalization.Get(
                        QuickGearLocalization.Keys.ToggleAugsImplantsTooltip
                    );

                    toggleObj = root.transform;
                }

                toggleObj.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
                RefreshAugsImplantsToggle(toggleObj);
            }

            private static Vector2 GetAugsImplantsTogglePosition(Transform parent)
            {
                var rightButton = parent.Find("SaveInventoryButton");
                if (rightButton != null)
                {
                    var rightButtonRect = rightButton.GetComponent<RectTransform>();
                    if (rightButtonRect != null)
                    {
                        float buttonRightEdge =
                            rightButtonRect.anchoredPosition.x
                            + rightButtonRect.sizeDelta.x * 0.5f;
                        float toggleHalfWidth = 74f * 0.5f;
                        const float minGap = 10f;
                        return new Vector2(
                            buttonRightEdge + minGap + toggleHalfWidth,
                            rightButtonRect.anchoredPosition.y
                        );
                    }
                }

                return new Vector2(380.3f, 122.8f);
            }

            private static void RefreshAugsImplantsToggle(Transform toggleObj)
            {
                var toggle = toggleObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.SetIsOnWithoutNotify(ModConfigStore.Config.HandleAugsAndImplants);
                    toggle.onValueChanged.AddListener(OnAugsImplantsToggleChanged);
                }

                var label = toggleObj.Find("Label");
                if (label != null)
                {
                    var labelText = label.GetComponent<Text>();
                    if (labelText != null)
                    {
                        labelText.text = QuickGearLocalization.Get(
                            QuickGearLocalization.Keys.ToggleAugsImplantsLabel
                        );
                    }
                }

                var tooltip = toggleObj.GetComponent<QuickGearTooltip>();
                if (tooltip != null)
                {
                    tooltip.TooltipText = QuickGearLocalization.Get(
                        QuickGearLocalization.Keys.ToggleAugsImplantsTooltip
                    );
                }
            }

            private static void OnAugsImplantsToggleChanged(bool isOn)
            {
                ModConfigStore.Config.HandleAugsAndImplants = isOn;
                ModConfigStore.SaveConfig();
                Debug.Log(
                    $"[QuickGear] HandleAugsAndImplants set to {isOn} for slot {ModConfigStore.CurrentSlot}."
                );
            }

            private static bool TryGetSavedEquipment(
                string profileId,
                out ModConfig.SavedEquipment savedEquip
            )
            {
                if (
                    ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(
                        profileId,
                        out savedEquip
                    )
                )
                    return true;

                string normalized = NormalizeProfileId(profileId);
                if (
                    normalized != profileId
                    && ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(
                        normalized,
                        out savedEquip
                    )
                )
                    return true;

                savedEquip = null;
                return false;
            }

            private static string NormalizeProfileId(string profileId)
            {
                if (string.IsNullOrEmpty(profileId))
                    return profileId;

                return profileId.EndsWith("_custom")
                    ? profileId.Substring(0, profileId.Length - "_custom".Length)
                    : profileId;
            }

            private class QuickGearTooltip
                : MonoBehaviour,
                    IPointerEnterHandler,
                    IPointerExitHandler
            {
                public string TooltipText;
                private bool _createdTooltip;

                public void OnPointerEnter(PointerEventData eventData)
                {
                    if (_createdTooltip || string.IsNullOrEmpty(TooltipText))
                        return;

                    _createdTooltip = true;
                    if (SingletonMonoBehaviour<TooltipFactory>.Instance != null)
                    {
                        SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip(
                            TooltipText
                        );
                    }
                }

                public void OnPointerExit(PointerEventData eventData)
                {
                    if (!_createdTooltip)
                        return;

                    _createdTooltip = false;
                    if (SingletonMonoBehaviour<TooltipFactory>.Instance != null)
                    {
                        SingletonMonoBehaviour<TooltipFactory>.Instance.HideSimpleTextTooltip();
                    }
                }
            }
        }
    }
}
