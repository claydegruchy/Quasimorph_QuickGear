using System;
using System.Collections.Generic;
using System.Linq;

namespace QuasimorphHelloWorld
{
    public static class QuickGearLocalization
    {
        public static class Keys
        {
            public const string QuickRestockButton = "button.quick_restock";
            public const string LoadEquipmentButton = "button.load_equipment";
            public const string SaveEquipmentButton = "button.save_equipment";
            public const string UpdateQuickRestockButton = "button.update_quick_restock";

            public const string QuickRestockTooltip = "tooltip.quick_restock";
            public const string LoadEquipmentTooltip = "tooltip.load_equipment";
            public const string SaveEquipmentTooltip = "tooltip.save_equipment";
            public const string UpdateQuickRestockTooltip = "tooltip.update_quick_restock";
        }

        private static readonly Dictionary<string, string> LanguageAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["english"] = "en",
                ["ru"] = "ru",
                ["russian"] = "ru",
                ["es"] = "es",
                ["spanish"] = "es",
                ["de"] = "de",
                ["german"] = "de",
                ["fr"] = "fr",
                ["french"] = "fr",
                ["pt"] = "pt-BR",
                ["pt-br"] = "pt-BR",
                ["portuguese"] = "pt-BR",
                ["zh"] = "zh-CN",
                ["zh-cn"] = "zh-CN",
                ["chinese"] = "zh-CN",
                ["ja"] = "ja",
                ["japanese"] = "ja",
                ["ko"] = "ko",
                ["korean"] = "ko",
                ["pl"] = "pl",
                ["polish"] = "pl"
            };

        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> BuiltInTranslations =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Quick Restock",
                    [Keys.LoadEquipmentButton] = "Load equipment",
                    [Keys.SaveEquipmentButton] = "Save equipment",
                    [Keys.UpdateQuickRestockButton] = "Update Quick Restock",
                    [Keys.QuickRestockTooltip] = "Pull configured items from cargo to inventory, this equipment list is shared between all mercenary profiles.\n\nIdeal for items that are frequently used and need to be restocked quickly, such as medkits or consumables.",
                    [Keys.LoadEquipmentTooltip] = "Load saved equipment, limbs, and implants for this mercenary.",
                    [Keys.SaveEquipmentTooltip] = "Save current equipped items, limbs, and implants for this mercenary.",
                    [Keys.UpdateQuickRestockTooltip] = "Save the current inventory items into the quick restock configuration.\n\nSaves to a shared configuration for all mercenary profiles."
                },
                ["ru"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Быстрое пополнение",
                    [Keys.LoadEquipmentButton] = "Загрузить экип.",
                    [Keys.SaveEquipmentButton] = "Сохранить экип.",
                    [Keys.UpdateQuickRestockButton] = "Обновить пополнение",
                    [Keys.QuickRestockTooltip] = "Переносит настроенные предметы из грузового отсека в инвентарь. Этот список общий для всех наемников.\n\nУдобно для часто используемых предметов, например аптечек и расходников.",
                    [Keys.LoadEquipmentTooltip] = "Загрузить сохраненную экипировку, конечности и импланты для этого наемника.",
                    [Keys.SaveEquipmentTooltip] = "Сохранить текущую экипировку, конечности и импланты этого наемника.",
                    [Keys.UpdateQuickRestockTooltip] = "Сохранить текущие предметы инвентаря в конфигурацию быстрого пополнения.\n\nЭта конфигурация общая для всех наемников."
                },
                ["es"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Reabastecer",
                    [Keys.LoadEquipmentButton] = "Cargar equipo",
                    [Keys.SaveEquipmentButton] = "Guardar equipo",
                    [Keys.UpdateQuickRestockButton] = "Actualizar repuesto",
                    [Keys.QuickRestockTooltip] = "Pasa objetos configurados desde la bodega al inventario. Esta lista es compartida por todos los mercenarios.\n\nIdeal para objetos de uso frecuente como botiquines o consumibles.",
                    [Keys.LoadEquipmentTooltip] = "Carga equipo, extremidades e implantes guardados para este mercenario.",
                    [Keys.SaveEquipmentTooltip] = "Guarda el equipo, extremidades e implantes actuales de este mercenario.",
                    [Keys.UpdateQuickRestockTooltip] = "Guarda los objetos actuales del inventario en la configuracion de reabastecimiento rapido.\n\nEsta configuracion es compartida por todos los mercenarios."
                },
                ["de"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Schnell auffullen",
                    [Keys.LoadEquipmentButton] = "Ausrustung laden",
                    [Keys.SaveEquipmentButton] = "Ausrustung speichern",
                    [Keys.UpdateQuickRestockButton] = "Restock aktualisieren",
                    [Keys.QuickRestockTooltip] = "Holt konfigurierte Gegenstande aus dem Frachtraum ins Inventar. Diese Liste wird von allen Soldnerprofilen geteilt.\n\nIdeal fur haufig genutzte Gegenstande wie Medkits oder Verbrauchsguter.",
                    [Keys.LoadEquipmentTooltip] = "Geladene Ausrustung, Gliedmassen und Implantate fur diesen Soldner anwenden.",
                    [Keys.SaveEquipmentTooltip] = "Aktuelle Ausrustung, Gliedmassen und Implantate dieses Soldners speichern.",
                    [Keys.UpdateQuickRestockTooltip] = "Aktuelle Inventargegenstande in die Schnellauffull-Konfiguration speichern.\n\nDiese Konfiguration ist fur alle Soldnerprofile gemeinsam."
                },
                ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Reappro rapide",
                    [Keys.LoadEquipmentButton] = "Charger equip.",
                    [Keys.SaveEquipmentButton] = "Sauver equip.",
                    [Keys.UpdateQuickRestockButton] = "Maj reappro",
                    [Keys.QuickRestockTooltip] = "Recupere les objets configures depuis la soute vers l'inventaire. Cette liste est partagee entre tous les mercenaires.\n\nIdeal pour les objets souvent utilises, comme les medikits ou consommables.",
                    [Keys.LoadEquipmentTooltip] = "Charge l'equipement, les membres et les implants sauvegardes pour ce mercenaire.",
                    [Keys.SaveEquipmentTooltip] = "Sauvegarde l'equipement, les membres et les implants actuels de ce mercenaire.",
                    [Keys.UpdateQuickRestockTooltip] = "Sauvegarde les objets d'inventaire actuels dans la configuration de reappro rapide.\n\nCette configuration est partagee entre tous les mercenaires."
                },
                ["pt-BR"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Reposicao rapida",
                    [Keys.LoadEquipmentButton] = "Carregar equipo",
                    [Keys.SaveEquipmentButton] = "Salvar equipo",
                    [Keys.UpdateQuickRestockButton] = "Atualizar reposicao",
                    [Keys.QuickRestockTooltip] = "Puxa itens configurados do cargueiro para o inventario. Esta lista e compartilhada entre todos os mercenarios.\n\nIdeal para itens usados com frequencia, como medkits ou consumiveis.",
                    [Keys.LoadEquipmentTooltip] = "Carrega equipamento, membros e implantes salvos para este mercenario.",
                    [Keys.SaveEquipmentTooltip] = "Salva equipamento, membros e implantes atuais deste mercenario.",
                    [Keys.UpdateQuickRestockTooltip] = "Salva os itens atuais do inventario na configuracao de reposicao rapida.\n\nEsta configuracao e compartilhada entre todos os mercenarios."
                },
                ["zh-CN"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "快速补给",
                    [Keys.LoadEquipmentButton] = "加载装备",
                    [Keys.SaveEquipmentButton] = "保存装备",
                    [Keys.UpdateQuickRestockButton] = "更新补给",
                    [Keys.QuickRestockTooltip] = "将已配置物品从货舱拉取到背包。该列表在所有佣兵之间共享。\n\n适合常用物品，例如医疗包和消耗品。",
                    [Keys.LoadEquipmentTooltip] = "为该佣兵加载已保存的装备、义肢和植入体。",
                    [Keys.SaveEquipmentTooltip] = "保存该佣兵当前的装备、义肢和植入体。",
                    [Keys.UpdateQuickRestockTooltip] = "将当前背包物品保存到快速补给配置。\n\n该配置在所有佣兵之间共享。"
                },
                ["ja"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "クイック補充",
                    [Keys.LoadEquipmentButton] = "装備を読み込み",
                    [Keys.SaveEquipmentButton] = "装備を保存",
                    [Keys.UpdateQuickRestockButton] = "補充を更新",
                    [Keys.QuickRestockTooltip] = "設定済みアイテムを貨物からインベントリへ移動します。このリストは全傭兵で共有されます。\n\n医療キットや消耗品など、よく使うアイテムに最適です。",
                    [Keys.LoadEquipmentTooltip] = "この傭兵の保存済み装備、義肢、インプラントを読み込みます。",
                    [Keys.SaveEquipmentTooltip] = "この傭兵の現在の装備、義肢、インプラントを保存します。",
                    [Keys.UpdateQuickRestockTooltip] = "現在のインベントリアイテムをクイック補充設定として保存します。\n\nこの設定は全傭兵で共有されます。"
                },
                ["ko"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "빠른 보급",
                    [Keys.LoadEquipmentButton] = "장비 불러오기",
                    [Keys.SaveEquipmentButton] = "장비 저장",
                    [Keys.UpdateQuickRestockButton] = "보급 갱신",
                    [Keys.QuickRestockTooltip] = "설정된 아이템을 화물칸에서 인벤토리로 가져옵니다. 이 목록은 모든 용병 프로필에서 공유됩니다.\n\n의료 키트나 소모품처럼 자주 쓰는 아이템에 적합합니다.",
                    [Keys.LoadEquipmentTooltip] = "이 용병의 저장된 장비, 의수, 임플란트를 불러옵니다.",
                    [Keys.SaveEquipmentTooltip] = "이 용병의 현재 장비, 의수, 임플란트를 저장합니다.",
                    [Keys.UpdateQuickRestockTooltip] = "현재 인벤토리 아이템을 빠른 보급 설정으로 저장합니다.\n\n이 설정은 모든 용병 프로필에서 공유됩니다."
                },
                ["pl"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [Keys.QuickRestockButton] = "Szybkie uzup.",
                    [Keys.LoadEquipmentButton] = "Wczytaj ekw.",
                    [Keys.SaveEquipmentButton] = "Zapisz ekw.",
                    [Keys.UpdateQuickRestockButton] = "Aktualizuj uzup.",
                    [Keys.QuickRestockTooltip] = "Przenosi skonfigurowane przedmioty z ladowni do ekwipunku. Lista jest wspolna dla wszystkich najemnikow.\n\nIdealne dla czesto uzywanych przedmiotow, takich jak apteczki i materialy zuzywalne.",
                    [Keys.LoadEquipmentTooltip] = "Wczytuje zapisany ekwipunek, konczyny i implanty dla tego najemnika.",
                    [Keys.SaveEquipmentTooltip] = "Zapisuje obecny ekwipunek, konczyny i implanty tego najemnika.",
                    [Keys.UpdateQuickRestockTooltip] = "Zapisuje aktualne przedmioty z ekwipunku do konfiguracji szybkiego uzupelniania.\n\nTa konfiguracja jest wspolna dla wszystkich najemnikow."
                }
            };

        public static string NormalizeLanguageCode(string rawLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(rawLanguageCode))
            {
                return "en";
            }

            string trimmed = rawLanguageCode.Trim();
            if (LanguageAliases.TryGetValue(trimmed, out var aliased))
            {
                return aliased;
            }

            if (BuiltInTranslations.ContainsKey(trimmed))
            {
                return trimmed;
            }

            string twoLetterCode = trimmed.Length >= 2 ? trimmed.Substring(0, 2).ToLowerInvariant() : trimmed.ToLowerInvariant();
            if (LanguageAliases.TryGetValue(twoLetterCode, out aliased))
            {
                return aliased;
            }

            return "en";
        }

        public static IReadOnlyCollection<string> GetSupportedLanguageCodes()
        {
            return BuiltInTranslations.Keys.ToList();
        }

        public static string Get(string key)
        {
            string selectedLanguage = NormalizeLanguageCode(ModConfigStore.GlobalSettings?.Language);

            if (BuiltInTranslations.TryGetValue(selectedLanguage, out var selectedMap) && selectedMap.TryGetValue(key, out var localizedValue))
            {
                return localizedValue;
            }

            if (BuiltInTranslations["en"].TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }

            return key;
        }
    }
}
