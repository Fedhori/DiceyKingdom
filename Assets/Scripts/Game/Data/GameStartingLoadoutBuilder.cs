using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GameStartingDiceUpgradeGrant
{
    public int diceIndex { get; set; }
    public string upgradeId { get; set; } = string.Empty;
}

public sealed class GameStartingLoadout
{
    public int defense { get; set; } = 5;
    public int stability { get; set; } = 5;
    public int gold { get; set; }
    public int totalDiceCount { get; set; } = 10;
    public int advisorSlotCount { get; set; } = 4;
    public int decreeSlotCount { get; set; } = 3;
    public List<string> advisorIds { get; set; } = new();
    public List<string> decreeIds { get; set; } = new();
    public List<GameStartingDiceUpgradeGrant> diceUpgradeGrants { get; set; } = new();
}

public static class GameStartingLoadoutBuilder
{
    const int startingAdvisorCount = 3;
    const int startingDecreeCount = 2;
    const int startingUpgradeDiceCount = 3;

    public static bool TryBuild(
        GameStaticDataCatalog catalog,
        System.Random random,
        int totalDiceCount,
        out GameStartingLoadout loadout,
        out string errorMessage)
    {
        loadout = null;
        errorMessage = string.Empty;

        var errors = new List<string>();
        if (catalog == null)
            errors.Add("catalog is null");
        if (random == null)
            errors.Add("random is null");
        if (totalDiceCount <= 0)
            errors.Add("totalDiceCount must be > 0");

        if (errors.Count > 0)
        {
            errorMessage = string.Join("\n", errors);
            return false;
        }

        if (catalog.advisors.Count < startingAdvisorCount)
            errors.Add($"advisor pool requires at least {startingAdvisorCount} entries");
        if (catalog.decrees.Count < startingDecreeCount)
            errors.Add($"decree pool requires at least {startingDecreeCount} entries");
        if (catalog.diceUpgrades.Count == 0)
            errors.Add("dice upgrade pool is empty");
        if (totalDiceCount < startingUpgradeDiceCount)
            errors.Add($"totalDiceCount must be >= {startingUpgradeDiceCount}");

        if (errors.Count > 0)
        {
            errorMessage = string.Join("\n", errors);
            return false;
        }

        var advisorIds = TakeUniqueIds(catalog.advisors.Select(item => item.advisorId).ToList(), startingAdvisorCount, random);
        var decreeIds = TakeUniqueIds(catalog.decrees.Select(item => item.decreeId).ToList(), startingDecreeCount, random);
        var selectedDiceIndices = TakeUniqueIndices(totalDiceCount, startingUpgradeDiceCount, random);

        var diceUpgradeGrants = new List<GameStartingDiceUpgradeGrant>();
        var upgradePool = catalog.diceUpgrades.Select(item => item.upgradeId).ToList();
        for (int i = 0; i < selectedDiceIndices.Count; i++)
        {
            var randomIndex = random.Next(0, upgradePool.Count);
            diceUpgradeGrants.Add(new GameStartingDiceUpgradeGrant
            {
                diceIndex = selectedDiceIndices[i],
                upgradeId = upgradePool[randomIndex]
            });
        }

        loadout = new GameStartingLoadout
        {
            totalDiceCount = totalDiceCount,
            advisorIds = advisorIds,
            decreeIds = decreeIds,
            diceUpgradeGrants = diceUpgradeGrants
        };

        return true;
    }

    static List<int> TakeUniqueIndices(int sourceCount, int takeCount, System.Random random)
    {
        var indices = Enumerable.Range(0, sourceCount).ToList();
        Shuffle(indices, random);
        return indices.Take(takeCount).ToList();
    }

    static List<string> TakeUniqueIds(List<string> source, int takeCount, System.Random random)
    {
        Shuffle(source, random);
        return source.Take(takeCount).ToList();
    }

    static void Shuffle<TValue>(List<TValue> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            var swapIndex = random.Next(0, i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }
}

