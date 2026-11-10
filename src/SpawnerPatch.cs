using Vintagestory.API.Common.Entities;

namespace LoreMobsSpawnReduce;

/// <summary>
/// Harmony prefix for Vintagestory.GameContent.BlockEntitySpawner.DoSpawn.
/// Spawner blocks (including locust nests, which subclass BlockEntitySpawner) spawn entities
/// directly via World.SpawnEntity, bypassing the OnTrySpawnEntity event, so they need a separate hook.
/// Skipping DoSpawn still lets the spawner start its cooldown, so the rate is genuinely reduced.
/// </summary>
public static class SpawnerPatch
{
    public static bool Prefix(EntityProperties entityType)
    {
        LoreMobsSpawnReduceModSystem system = LoreMobsSpawnReduceModSystem.Instance;
        if (system == null || entityType?.Code == null) return true;

        return system.ShouldAllowSpawn(entityType.Code);
    }
}
