using System;
using System.Collections.Generic;
using System.Globalization;
using Data;
using GameStats;
using UnityEngine;

public sealed class SaveValidationError
{
    public string FieldPath { get; }
    public string Expected { get; }
    public string Actual { get; }
    public string Code { get; }

    public SaveValidationError(string fieldPath, string expected, string actual, string code)
    {
        FieldPath = fieldPath ?? string.Empty;
        Expected = expected ?? string.Empty;
        Actual = actual ?? string.Empty;
        Code = code ?? string.Empty;
    }
}

public sealed class SaveValidationResult
{
    readonly List<SaveValidationError> errors = new();
    public IReadOnlyList<SaveValidationError> Errors => errors;
    public bool IsValid => errors.Count == 0;

    public void AddError(string fieldPath, string expected, string actual, string code)
    {
        errors.Add(new SaveValidationError(fieldPath, expected, actual, code));
    }
}

public sealed class SaveValidationOptions
{
    public bool CheckRepositories = true;
    public bool CheckStageRepository = true;
}

public static class SaveValidator
{
    public static SaveValidationResult Validate(SaveData data, SaveValidationOptions options = null)
    {
        options ??= new SaveValidationOptions();
        var result = new SaveValidationResult();

        if (data == null)
        {
            result.AddError("root", "non-null SaveData", "null", "null");
            return result;
        }

        ValidateMeta(data.meta, result);
        ValidateRun(data.run, result, options);
        ValidatePlayer(data.player, result, options);
        ValidateInventory(data.inventory, result, options);
        ValidateUpgradeInventory(data.upgradeInventory, result, options);

        return result;
    }

    static void ValidateMeta(SaveMeta meta, SaveValidationResult result)
    {
        if (meta == null)
        {
            result.AddError("meta", "object", "null", "missing");
            return;
        }

        if (meta.schemaVersion <= 0)
            result.AddError("meta.schemaVersion", ">= 1", FormatValue(meta.schemaVersion), "out_of_range");

        if (string.IsNullOrEmpty(meta.appVersion))
            result.AddError("meta.appVersion", "non-empty string", FormatValue(meta.appVersion), "missing");

        if (meta.timestampUtc <= 0)
            result.AddError("meta.timestampUtc", "> 0", FormatValue(meta.timestampUtc), "out_of_range");

        if (string.IsNullOrEmpty(meta.checksum))
            result.AddError("meta.checksum", "non-empty string", FormatValue(meta.checksum), "missing");
    }

    static void ValidateRun(SaveRun run, SaveValidationResult result, SaveValidationOptions options)
    {
        if (run == null)
        {
            result.AddError("run", "object", "null", "missing");
            return;
        }

        if (run.stageIndex < 0)
        {
            result.AddError("run.stageIndex", ">= 0", FormatValue(run.stageIndex), "out_of_range");
            return;
        }

        if (!options.CheckStageRepository)
            return;

        if (!StageRepository.IsInitialized)
        {
            result.AddError("repository.stage", "initialized", "false", "repository_not_initialized");
            return;
        }

        if (run.stageIndex >= StageRepository.Count)
            result.AddError("run.stageIndex", $"< {StageRepository.Count}", FormatValue(run.stageIndex), "out_of_range");
    }

    static void ValidatePlayer(SavePlayer player, SaveValidationResult result, SaveValidationOptions options)
    {
        if (player == null)
        {
            result.AddError("player", "object", "null", "missing");
            return;
        }

        if (string.IsNullOrEmpty(player.playerId))
        {
            result.AddError("player.playerId", "non-empty string", FormatValue(player.playerId), "missing");
        }
        else if (options.CheckRepositories)
        {
            if (!PlayerRepository.IsInitialized)
            {
                result.AddError("repository.player", "initialized", "false", "repository_not_initialized");
            }
            else
            {
                bool exists = false;
                foreach (var dto in PlayerRepository.All)
                {
                    if (dto != null && string.Equals(dto.id, player.playerId, StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    result.AddError("player.playerId", "existing player id", FormatValue(player.playerId), "not_found");
            }
        }

        if (player.currency < 0)
            result.AddError("player.currency", ">= 0", FormatValue(player.currency), "out_of_range");

        if (player.permanentStatModifiers == null)
        {
            result.AddError("player.permanentStatModifiers", "array", "null", "missing");
            return;
        }

        for (int i = 0; i < player.permanentStatModifiers.Count; i++)
        {
            var modifier = player.permanentStatModifiers[i];
            ValidateStatModifier(modifier, $"player.permanentStatModifiers[{i}]", result);
        }
    }

    static void ValidateInventory(SaveInventory inventory, SaveValidationResult result, SaveValidationOptions options)
    {
        if (inventory == null)
        {
            result.AddError("inventory", "object", "null", "missing");
            return;
        }

        if (inventory.slots == null)
        {
            result.AddError("inventory.slots", "array", "null", "missing");
            return;
        }

        int slotCount = inventory.slots.Count;
        int maxSlots = GameConfig.ItemSlotCount;
        if (slotCount > maxSlots)
            result.AddError("inventory.slots", $"length <= {maxSlots}", FormatValue(slotCount), "out_of_range");

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slot = inventory.slots[i];
            if (slot == null)
            {
                result.AddError($"inventory.slots[{i}]", "object", "null", "missing");
                continue;
            }

            string slotPath = $"inventory.slots[{i}]";
            bool isEmpty = string.IsNullOrEmpty(slot.itemId);
            if (isEmpty)
            {
                if (!string.IsNullOrEmpty(slot.itemUniqueId))
                    result.AddError($"{slotPath}.itemUniqueId", "empty when itemId is empty", FormatValue(slot.itemUniqueId), "invalid_state");
                if (slot.permanentStatModifiers != null && slot.permanentStatModifiers.Count > 0)
                    result.AddError($"{slotPath}.permanentStatModifiers", "empty when itemId is empty", FormatValue(slot.permanentStatModifiers.Count), "invalid_state");
                if (slot.upgrades != null && slot.upgrades.Count > 0)
                    result.AddError($"{slotPath}.upgrades", "empty when itemId is empty", FormatValue(slot.upgrades.Count), "invalid_state");
                continue;
            }

            if (string.IsNullOrEmpty(slot.itemUniqueId))
                result.AddError($"{slotPath}.itemUniqueId", "non-empty string", FormatValue(slot.itemUniqueId), "missing");

            if (options.CheckRepositories)
            {
                if (!ItemRepository.IsInitialized)
                {
                    result.AddError("repository.item", "initialized", "false", "repository_not_initialized");
                }
                else if (!ItemRepository.TryGet(slot.itemId, out _))
                {
                    result.AddError($"{slotPath}.itemId", "existing item id", FormatValue(slot.itemId), "not_found");
                }
            }

            if (slot.permanentStatModifiers == null)
            {
                result.AddError($"{slotPath}.permanentStatModifiers", "array", "null", "missing");
            }
            else
            {
                for (int m = 0; m < slot.permanentStatModifiers.Count; m++)
                    ValidateStatModifier(slot.permanentStatModifiers[m], $"{slotPath}.permanentStatModifiers[{m}]", result);
            }

            if (slot.upgrades == null)
            {
                result.AddError($"{slotPath}.upgrades", "array", "null", "missing");
            }
            else
            {
                for (int u = 0; u < slot.upgrades.Count; u++)
                    ValidateUpgrade(slot.upgrades[u], $"{slotPath}.upgrades[{u}]", result, options);
            }
        }
    }

    static void ValidateUpgradeInventory(SaveUpgradeInventory inventory, SaveValidationResult result, SaveValidationOptions options)
    {
        if (inventory == null)
        {
            result.AddError("upgradeInventory", "object", "null", "missing");
            return;
        }

        if (inventory.upgrades == null)
        {
            result.AddError("upgradeInventory.upgrades", "array", "null", "missing");
            return;
        }

        for (int i = 0; i < inventory.upgrades.Count; i++)
            ValidateUpgrade(inventory.upgrades[i], $"upgradeInventory.upgrades[{i}]", result, options);
    }

    static void ValidateUpgrade(SaveUpgrade upgrade, string path, SaveValidationResult result, SaveValidationOptions options)
    {
        if (upgrade == null)
        {
            result.AddError(path, "object", "null", "missing");
            return;
        }

        if (string.IsNullOrEmpty(upgrade.upgradeId))
        {
            result.AddError($"{path}.upgradeId", "non-empty string", FormatValue(upgrade.upgradeId), "missing");
        }
        else if (options.CheckRepositories)
        {
            if (!UpgradeRepository.IsInitialized)
            {
                result.AddError("repository.upgrade", "initialized", "false", "repository_not_initialized");
            }
            else if (!UpgradeRepository.TryGet(upgrade.upgradeId, out _))
            {
                result.AddError($"{path}.upgradeId", "existing upgrade id", FormatValue(upgrade.upgradeId), "not_found");
            }
        }

        if (string.IsNullOrEmpty(upgrade.upgradeUniqueId))
            result.AddError($"{path}.upgradeUniqueId", "non-empty string", FormatValue(upgrade.upgradeUniqueId), "missing");
    }

    static void ValidateStatModifier(SaveStatModifier modifier, string path, SaveValidationResult result)
    {
        if (modifier == null)
        {
            result.AddError(path, "object", "null", "missing");
            return;
        }

        if (string.IsNullOrEmpty(modifier.statId))
            result.AddError($"{path}.statId", "non-empty string", FormatValue(modifier.statId), "missing");

        if (modifier.layer != StatLayer.Permanent)
            result.AddError($"{path}.layer", "Permanent", FormatValue(modifier.layer), "invalid_layer");

        if (string.IsNullOrEmpty(modifier.source))
            result.AddError($"{path}.source", "non-empty string", FormatValue(modifier.source), "missing");
    }

    static string FormatValue(object value)
    {
        if (value == null)
            return "null";

        if (value is string s)
            return s;

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString();
    }
}
