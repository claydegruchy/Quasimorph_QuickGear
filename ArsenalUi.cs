using System;
using System.Collections.Generic;
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
            private static string _selectedLoadSourceProfileId;
            private static Transform _loadSourceDropdownParent;

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

                    EnsureSelectedSourceValidity(mercenary);

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
                            48f,
                            16f,
                            () =>
                            {
                                try
                                {
                                    Debug.Log(
                                        "[QuickGear] Load Saved Equipment clicked: loading saved equipment."
                                    );
                                    QuickGearService.LoadSavedEquipment(
                                        mercenary,
                                        ResolveSelectedLoadSourceProfileId(mercenary)
                                    );
                                    CloseLoadSourceDropdown();
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
                            baseLocal + new Vector2(100f, 0f),
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
                            baseLocal + new Vector2(146f, 0f),
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
                captionRect.offsetMax =
                    spec.ObjectName == "LoadSavedEquipmentButton"
                        ? new Vector2(-12f, -4f)
                        : new Vector2(-4f, -4f);

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

                if (spec.ObjectName == "LoadSavedEquipmentButton")
                {
                    EnsureLoadSourceArrow(buttonObj.transform, parent);
                }
            }

            private static void UpdateExistingQuickGearButtons(
                Transform parent,
                Mercenary mercenary
            )
            {
                EnsureSelectedSourceValidity(mercenary);

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
                        QuickGearService.LoadSavedEquipment(
                            mercenary,
                            ResolveSelectedLoadSourceProfileId(mercenary)
                        );
                        CloseLoadSourceDropdown();
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

                var loadButtonObj = parent.Find("LoadSavedEquipmentButton");
                if (loadButtonObj != null)
                {
                    EnsureLoadSourceArrow(loadButtonObj, parent);
                }
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

            private static void EnsureSelectedSourceValidity(Mercenary currentMerc)
            {
                if (currentMerc == null)
                {
                    _selectedLoadSourceProfileId = null;
                    return;
                }

                if (string.IsNullOrWhiteSpace(_selectedLoadSourceProfileId))
                {
                    return;
                }

                if (!QuickGearService.HasSavedEquipment(_selectedLoadSourceProfileId))
                {
                    _selectedLoadSourceProfileId = null;
                }
            }

            private static string ResolveSelectedLoadSourceProfileId(Mercenary currentMerc)
            {
                if (currentMerc == null)
                {
                    return null;
                }

                EnsureSelectedSourceValidity(currentMerc);
                return string.IsNullOrWhiteSpace(_selectedLoadSourceProfileId)
                    ? currentMerc.ProfileId
                    : _selectedLoadSourceProfileId;
            }

            private static void EnsureLoadSourceArrow(Transform loadButton, Transform parent)
            {
                if (loadButton == null)
                {
                    return;
                }

                var arrowObj = loadButton.Find("LoadSourceArrowButton") as RectTransform;
                if (arrowObj == null)
                {
                    var obj = new GameObject(
                        "LoadSourceArrowButton",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(Button)
                    );
                    obj.layer = loadButton.gameObject.layer;
                    obj.transform.SetParent(loadButton, false);
                    arrowObj = obj.GetComponent<RectTransform>();

                    var arrowTextObj = new GameObject(
                        "Caption",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Text)
                    );
                    arrowTextObj.layer = obj.layer;
                    arrowTextObj.transform.SetParent(obj.transform, false);

                    var arrowTextRect = arrowTextObj.GetComponent<RectTransform>();
                    arrowTextRect.anchorMin = Vector2.zero;
                    arrowTextRect.anchorMax = Vector2.one;
                    arrowTextRect.offsetMin = Vector2.zero;
                    arrowTextRect.offsetMax = Vector2.zero;

                    var arrowText = arrowTextObj.GetComponent<Text>();
                    arrowText.text = "v";
                    arrowText.alignment = TextAnchor.MiddleCenter;
                    arrowText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    arrowText.color = Color.white;
                    arrowText.fontSize = 6;
                    arrowText.raycastTarget = false;
                }

                arrowObj.anchorMin = new Vector2(1f, 0f);
                arrowObj.anchorMax = new Vector2(1f, 1f);
                arrowObj.pivot = new Vector2(1f, 0.5f);
                arrowObj.anchoredPosition = Vector2.zero;
                arrowObj.sizeDelta = new Vector2(10f, 0f);

                var image = arrowObj.GetComponent<Image>();
                image.color = new Color(40f / 255f, 49f / 255f, 50f / 255f, 0.95f);
                image.raycastTarget = true;

                var button = arrowObj.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    var selectedMerc = QuickGearService.GetSelectedMerc();
                    ToggleLoadSourceDropdown(parent, loadButton.GetComponent<RectTransform>(), selectedMerc);
                });

                var tooltip = arrowObj.GetComponent<QuickGearTooltip>();
                if (tooltip == null)
                {
                    tooltip = arrowObj.gameObject.AddComponent<QuickGearTooltip>();
                }

                tooltip.TooltipText = QuickGearLocalization.Get(
                    QuickGearLocalization.Keys.LoadEquipmentSourceTooltip
                );
            }

            private static void ToggleLoadSourceDropdown(
                Transform parent,
                RectTransform loadButtonRect,
                Mercenary currentMerc
            )
            {
                if (parent == null || loadButtonRect == null)
                {
                    return;
                }

                if (_loadSourceDropdownParent != null && _loadSourceDropdownParent != parent)
                {
                    CloseLoadSourceDropdown();
                }

                var existing = parent.Find("LoadSourceDropdownPanel");
                if (existing != null)
                {
                    CloseLoadSourceDropdown();
                    return;
                }

                BuildLoadSourceDropdown(parent, loadButtonRect, currentMerc);
            }

            private static void BuildLoadSourceDropdown(
                Transform parent,
                RectTransform loadButtonRect,
                Mercenary currentMerc
            )
            {
                List<LoadSourceOption> options = BuildLoadSourceOptions();
                if (options.Count == 0)
                {
                    Debug.Log("[QuickGear] No saved equipment sources available for dropdown.");
                    return;
                }

                EnsureSelectedSourceValidity(currentMerc);

                var panel = new GameObject(
                    "LoadSourceDropdownPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(Image),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(Outline)
                );
                panel.layer = LayerMask.NameToLayer("UI");
                panel.transform.SetParent(parent, false);
                _loadSourceDropdownParent = parent;

                var panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.anchoredPosition =
                    loadButtonRect.anchoredPosition + new Vector2(loadButtonRect.sizeDelta.x * 0.5f, -10f);
                panelRect.sizeDelta = new Vector2(154f, 4f);
                panel.transform.SetAsLastSibling();

                var panelCanvas = panel.GetComponent<Canvas>();
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 5000;

                var panelImage = panel.GetComponent<Image>();
                panelImage.color = new Color(22f / 255f, 29f / 255f, 30f / 255f, 0.98f);
                panelImage.raycastTarget = true;

                var panelOutline = panel.GetComponent<Outline>();
                panelOutline.effectColor = new Color(79f / 255f, 114f / 255f, 102f / 255f, 0.95f);
                panelOutline.effectDistance = new Vector2(1f, -1f);

                var layout = panel.GetComponent<VerticalLayoutGroup>();
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.spacing = 1f;
                layout.padding = new RectOffset(2, 2, 2, 2);

                var fitter = panel.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                foreach (var option in options)
                {
                    bool isSelected = string.Equals(
                        ResolveSelectedLoadSourceProfileId(currentMerc),
                        option.ProfileId,
                        StringComparison.Ordinal
                    );

                    AddLoadSourceOptionRow(
                        panel.transform,
                        option,
                        isSelected,
                        () =>
                        {
                            _selectedLoadSourceProfileId = option.ProfileId;
                            Debug.Log(
                                $"[QuickGear] Load source selected: {option.DisplayName} ({option.ProfileId})"
                            );
                            CloseLoadSourceDropdown();
                        }
                    );
                }
            }

            private static void AddLoadSourceOptionRow(
                Transform parent,
                LoadSourceOption option,
                bool isSelected,
                Action onClick
            )
            {
                var row = new GameObject(
                    "Option_" + NormalizeProfileId(option.ProfileId),
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement)
                );
                row.layer = LayerMask.NameToLayer("UI");
                row.transform.SetParent(parent, false);

                var rowRect = row.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0f, 16f);

                var layoutElement = row.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = 16f;
                layoutElement.minHeight = 16f;
                layoutElement.flexibleHeight = 0f;

                var rowImage = row.GetComponent<Image>();
                rowImage.color = isSelected
                    ? new Color(71f / 255f, 101f / 255f, 91f / 255f, 1f)
                    : new Color(36f / 255f, 46f / 255f, 47f / 255f, 1f);
                rowImage.raycastTarget = true;

                var rowButton = row.GetComponent<Button>();
                rowButton.targetGraphic = rowImage;
                rowButton.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    rowButton.onClick.AddListener(() => onClick());
                }

                var textObj = new GameObject(
                    "Caption",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text)
                );
                textObj.layer = row.layer;
                textObj.transform.SetParent(row.transform, false);

                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4f, 2f);
                textRect.offsetMax = new Vector2(-4f, -2f);

                var text = textObj.GetComponent<Text>();
                text.text = isSelected ? "> " + option.DisplayName : option.DisplayName;
                text.alignment = TextAnchor.MiddleLeft;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.color = Color.white;
                text.fontSize = 6;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
            }

            private static List<LoadSourceOption> BuildLoadSourceOptions()
            {
                List<Mercenary> mercs = QuickGearService.GetMercenariesWithSavedEquipment();
                var options = mercs
                    .Where(merc => merc != null)
                    .Select(merc =>
                    {
                        string mercName = QuickGearService.GetMercenaryDropdownName(merc.ProfileId);
                        string className = QuickGearService.GetMercenaryClassDisplayName(merc);

                        string label = string.IsNullOrWhiteSpace(className)
                            ? mercName
                            : mercName + " - " + className;

                        return new LoadSourceOption
                        {
                            ProfileId = merc.ProfileId,
                            DisplayName = label
                        };
                    }
                    )
                    .ToList();

                var nameCounts = options
                    .GroupBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

                foreach (var option in options)
                {
                    if (nameCounts.TryGetValue(option.DisplayName, out int count) && count > 1)
                    {
                        int index = options
                            .TakeWhile(existing => !ReferenceEquals(existing, option))
                            .Count(existing => string.Equals(existing.DisplayName, option.DisplayName, StringComparison.OrdinalIgnoreCase));
                        option.DisplayName = option.DisplayName + " (" + (index + 1).ToString() + ")";
                    }

                    Debug.Log(
                        "[QuickGear] Load source option label: "
                            + option.ProfileId
                            + " -> "
                            + option.DisplayName
                    );
                }

                return options;
            }

            private static void CloseLoadSourceDropdown()
            {
                if (_loadSourceDropdownParent == null)
                {
                    return;
                }

                var dropdown = _loadSourceDropdownParent.Find("LoadSourceDropdownPanel");
                if (dropdown != null)
                {
                    UnityEngine.Object.Destroy(dropdown.gameObject);
                }

                _loadSourceDropdownParent = null;
            }

            private sealed class LoadSourceOption
            {
                public string ProfileId { get; set; }
                public string DisplayName { get; set; }
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
