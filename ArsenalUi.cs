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
                            "Quick Restock",
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
                            "Pulls configured items from cargo to inventory, this equipment list is shared between all mercenary profiles.\n\nIdeal for items that are frequently used and need to be restocked quickly, such as medkits or consumables."
                        ),
                        new ButtonSpec(
                            "LoadSavedEquipmentButton",
                            "Load equipment",
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
                            "Load saved equipment, limbs, and implants for this mercenary."
                        ),
                        new ButtonSpec(
                            "SaveEquipmentButton",
                            "Save equipment",
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
                            "Save current equipped items, limbs, and implants for this mercenary."
                        ),
                        new ButtonSpec(
                            "SaveInventoryButton",
                            "Update Quick Restock",
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
                            "Save the current inventory items into the quick restock configuration.\n\nSaves to a shared configuration for all mercenary profiles."
                        )
                    };

                    foreach (var spec in buttonSpecs)
                    {
                        AddButton(parent, spec);
                    }
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
                public string BaseLabel { get; }
                public Vector2 AnchoredPosition { get; }
                public float Width { get; }
                public float Height { get; }
                public Action OnClick { get; }
                public string TooltipText { get; }

                public ButtonSpec(
                    string objectName,
                    string baseLabel,
                    Vector2 anchoredPosition,
                    float width,
                    float height,
                    Action onClick,
                    string tooltipText
                )
                {
                    ObjectName = objectName;
                    BaseLabel = baseLabel;
                    AnchoredPosition = anchoredPosition;
                    Width = width;
                    Height = height;
                    OnClick = onClick;
                    TooltipText = tooltipText;
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
                txt.text = spec.BaseLabel;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.resizeTextForBestFit = false;
                txt.fontSize = 4;
                txt.raycastTarget = false;

                if (!string.IsNullOrEmpty(spec.TooltipText))
                {
                    var tooltip = buttonObj.AddComponent<QuickGearTooltip>();
                    tooltip.TooltipText = spec.TooltipText;
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
