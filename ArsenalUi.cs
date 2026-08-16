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
                        Debug.Log("[QuickGear] QuickGear buttons already exist; updating for new merc.");
                        UpdateExistingQuickGearButtons(parent, mercenary);
                        return;
                    }

                    Debug.Log(
                        "[QuickGear] Creating QuickRestockButtons in ArsenalScreen under "
                            + parent.name
                    );

                    Vector2 baseLocal = new Vector2(196.3f, 122.8f);

                    CreateQuickGearButton(
                        parent,
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
                                Debug.Log("[QuickGear] Error running Quick Restock: " + e.Message);
                            }
                        },
                        "Pulls configured items from cargo to inventory, this equipment list is shared between all mercenary profiles.\n\nIdeal for items that are frequently used and need to be restocked quickly, such as medkits or consumables.",
                        QuickGearButton.UpdateMode.QuickRestock,
                        null
                    );

                    CreateQuickGearButton(
                        parent,
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
                        "Load saved equipment, limbs, and implants for this mercenary.",
                        QuickGearButton.UpdateMode.LoadSavedEquipment,
                        mercenary.ProfileId
                    );

                    CreateQuickGearButton(
                        parent,
                        "SaveEquipmentButton",
                        "Save equipment",
                        baseLocal + new Vector2(92f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                Debug.Log("[QuickGear] Save Equipment clicked: saving equipment.");
                                QuickGearService.SaveEquipment(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("[QuickGear] Error saving equipment: " + e.Message);
                            }
                        },
                        "Save current equipped items, limbs, and implants for this mercenary.",
                        QuickGearButton.UpdateMode.SaveEquipment,
                        mercenary.ProfileId
                    );

                    CreateQuickGearButton(
                        parent,
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
                                    "[QuickGear] Error saving inventory quick equip: " + e.Message
                                );
                            }
                        },
                        "Save the current inventory items into the quick restock configuration.\n\nSaves to a shared configuration for all mercenary profiles.",
                        QuickGearButton.UpdateMode.SaveInventory,
                        mercenary.ProfileId
                    );
                }
                catch (Exception e)
                {
                    Debug.Log("[QuickGear] Exception in ArsenalScreen patch: " + e.Message);
                    Debug.Log("[QuickGear] " + e.StackTrace);
                }
            }

            private static void CreateQuickGearButton(
                Transform parent,
                string objectName,
                string baseLabel,
                Vector2 anchoredPosition,
                float width,
                float height,
                Action onClick,
                string tooltipText = null,
                QuickGearButton.UpdateMode updateMode = QuickGearButton.UpdateMode.None,
                string mercProfileId = null
            )
            {
                if (parent.Find(objectName) != null)
                    return;

                var buttonObj = new GameObject(
                    objectName,
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
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = new Vector2(width, height);
                rect.localScale = Vector3.one;

                var layout = buttonObj.AddComponent<LayoutElement>();
                layout.preferredWidth = width;
                layout.preferredHeight = height;
                layout.minWidth = width;
                layout.minHeight = height;
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
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
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
                txt.text = baseLabel;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.resizeTextForBestFit = false;
                txt.fontSize = 4;
                txt.raycastTarget = false;

                if (!string.IsNullOrEmpty(tooltipText))
                {
                    var tooltip = buttonObj.AddComponent<QuickGearTooltip>();
                    tooltip.TooltipText = tooltipText;
                }

                if (updateMode != QuickGearButton.UpdateMode.None)
                {
                    var updater = buttonObj.AddComponent<QuickGearButton>();
                    updater.Mode = updateMode;
                    updater.BaseText = baseLabel;
                    updater.MercProfileId = mercProfileId;
                }
            }

            private static int GetQuickRestockItemCount()
            {
                return ModConfigStore.Config.Items?.Sum(item => Math.Max(0, item.Count)) ?? 0;
            }

            private static int GetSavedEquipmentCount(Mercenary merc)
            {
                if (merc == null || !TryGetSavedEquipment(merc.ProfileId, out var savedEquip))
                {
                    return 0;
                }

                return CountSavedEquipmentItems(savedEquip);
            }

            private static int GetCurrentEquipmentCount(Mercenary merc)
            {
                if (merc == null)
                    return 0;

                var inventory = merc.CreatureData?.Inventory;
                if (inventory == null)
                    return 0;

                int count = 0;
                foreach (var slot in new[]
                    {
                        inventory.BackpackSlot,
                        inventory.PrimarySlot,
                        inventory.SecondarySlot,
                        inventory.ServoArmSlot,
                        inventory.AdditionalSlot,
                        inventory.ArmorSlot,
                        inventory.HelmetSlot,
                        inventory.LeggingsSlot,
                        inventory.BootsSlot,
                        inventory.VestSlot
                    })
                {
                    if (slot?.First != null)
                    {
                        count += 1;
                    }
                }

                return count;
            }

            private static int GetInventorySaveCount(Mercenary merc)
            {
                if (merc == null)
                    return 0;

                var inventory = merc.CreatureData?.Inventory;
                if (inventory == null)
                    return 0;

                int total = 0;
                foreach (ItemStorage storage in inventory.Storages)
                {
                    if (storage == null)
                        continue;

                    foreach (BasePickupItem item in storage.Items)
                    {
                        if (item == null)
                            continue;

                        total += item.IsStackable ? item.StackCount : 1;
                    }
                }

                return total;
            }

            private static int CountSavedEquipmentItems(ModConfig.SavedEquipment savedEquip)
            {
                if (savedEquip == null)
                    return 0;

                int count = savedEquip.Equipment?.Count ?? 0;
                count += savedEquip.Limbs?.Count ?? 0;
                count += savedEquip.Implants?.Values.Sum(list => list?.Count ?? 0) ?? 0;
                return count;
            }

            private static void UpdateExistingQuickGearButtons(Transform parent, Mercenary mercenary)
            {
                UpdateQuickGearButton(parent, "QuickRestockButton", () => QuickGearService.EquipQuickGear(mercenary), QuickGearButton.UpdateMode.QuickRestock, null);
                UpdateQuickGearButton(parent, "LoadSavedEquipmentButton", () =>
                {
                    Debug.Log("[QuickGear] Load Saved Equipment clicked: loading saved equipment.");
                    QuickGearService.LoadSavedEquipment(mercenary);
                }, QuickGearButton.UpdateMode.LoadSavedEquipment, mercenary.ProfileId);
                UpdateQuickGearButton(parent, "SaveEquipmentButton", () =>
                {
                    Debug.Log("[QuickGear] Save Equipment clicked: saving equipment.");
                    QuickGearService.SaveEquipment(mercenary);
                }, QuickGearButton.UpdateMode.SaveEquipment, mercenary.ProfileId);
                UpdateQuickGearButton(parent, "SaveInventoryButton", () =>
                {
                    Debug.Log("[QuickGear] Save Inventory clicked: saving current inventory to quick equip config.");
                    QuickGearService.SaveInventoryQuickGear(mercenary);
                }, QuickGearButton.UpdateMode.SaveInventory, mercenary.ProfileId);
            }

            private static void UpdateQuickGearButton(
                Transform parent,
                string objectName,
                Action onClick,
                QuickGearButton.UpdateMode mode,
                string mercProfileId
            )
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

                var updater = buttonObj.GetComponent<QuickGearButton>();
                if (updater != null)
                {
                    updater.Mode = mode;
                    updater.MercProfileId = mercProfileId;
                    updater.UpdateLabelImmediate();
                }
            }

            private static string FormatButtonLabel(string baseText, int count)
            {
                return count > 0 ? $"{baseText} ({count})" : baseText;
            }

            private static bool TryGetSavedEquipment(string profileId, out ModConfig.SavedEquipment savedEquip)
            {
                if (ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(profileId, out savedEquip))
                    return true;

                string normalized = NormalizeProfileId(profileId);
                if (normalized != profileId && ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(normalized, out savedEquip))
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

            private class QuickGearTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
                        SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip(TooltipText);
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

            public class QuickGearButton : MonoBehaviour
            {
                public enum UpdateMode { None = 0, QuickRestock = 1, LoadSavedEquipment = 2, SaveEquipment = 3, SaveInventory = 4 }
                public UpdateMode Mode = UpdateMode.None;
                public string BaseText;
                public string MercProfileId;

                private Text _caption;
                private float _nextUpdate;

                private void Start()
                {
                    var t = transform.Find("Caption");
                    if (t != null)
                        _caption = t.GetComponent<Text>();
                    _nextUpdate = Time.time;
                    UpdateLabelImmediate();
                }

                private void Update()
                {
                    if (Time.time < _nextUpdate)
                        return;
                    _nextUpdate = Time.time + 0.5f;
                    UpdateLabelImmediate();
                }

                public void UpdateLabelImmediate()
                {
                    if (_caption == null || string.IsNullOrEmpty(BaseText))
                        return;

                    int count = 0;
                    try
                    {
                        switch (Mode)
                        {
                            case UpdateMode.QuickRestock:
                                count = GetQuickRestockItemCount();
                                break;
                            case UpdateMode.LoadSavedEquipment:
                                {
                                    var merc = ResolveMerc();
                                    count = GetSavedEquipmentCount(merc);
                                }
                                break;
                            case UpdateMode.SaveEquipment:
                                {
                                    var merc = ResolveMerc();
                                    count = GetCurrentEquipmentCount(merc);
                                }
                                break;
                            case UpdateMode.SaveInventory:
                                {
                                    var merc = ResolveMerc();
                                    count = GetInventorySaveCount(merc);
                                }
                                break;
                        }
                    }
                    catch { }

                    _caption.text = FormatButtonLabel(BaseText, count);
                }

                private Mercenary ResolveMerc()
                {
                    if (string.IsNullOrEmpty(MercProfileId) || ModMain._modContext == null)
                        return null;
                    var mercs = ModMain._modContext.State.Get<Mercenaries>();
                    if (mercs == null)
                        return null;
                    return mercs.Get(MercProfileId);
                }
            }
        }
    }
}
