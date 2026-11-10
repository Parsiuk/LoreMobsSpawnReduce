using System;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace LoreMobsSpawnReduce;

public class LoreMobsSpawnReduceModSystem : ModSystem
{
    public const string ConfigFileName = "loremobsspawnreduce.json";

    public static LoreMobsSpawnReduceModSystem Instance { get; private set; }

    private ICoreServerAPI sapi;
    private Harmony harmony;
    private double multiplier;
    private AssetLocation[] patterns;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        LoreMobsSpawnReduceConfig config = LoadConfig(api);

        multiplier = Math.Clamp(config.SpawnChanceMultiplier, 0, 1);
        patterns = (config.EntityCodes ?? new())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => new AssetLocation(code.Trim()))
            .DistinctBy(loc => loc.ToString())
            .ToArray();

        Mod.Logger.Notification("Spawn chance multiplier {0} applied to: {1}", multiplier, string.Join(", ", patterns.Select(p => p.ToString())));

        if (multiplier < 1 && patterns.Length > 0)
        {
            Instance = this;
            api.Event.OnTrySpawnEntity += OnTrySpawnEntity;
            PatchSpawnerBlocks();
        }
    }

    private void PatchSpawnerBlocks()
    {
        try
        {
            var original = AccessTools.Method(typeof(BlockEntitySpawner), "DoSpawn");
            if (original == null)
            {
                Mod.Logger.Warning("BlockEntitySpawner.DoSpawn not found, spawner blocks (e.g. locust nests) will not be affected.");
                return;
            }

            harmony = new Harmony(Mod.Info.ModID);
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(SpawnerPatch), nameof(SpawnerPatch.Prefix)));
            Mod.Logger.Notification("Patched spawner blocks (locust nests etc.)");
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch spawner blocks, they will not be affected. Error: {0}", e);
        }
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(Mod.Info.ModID);
        harmony = null;
        if (Instance == this) Instance = null;
    }

    private LoreMobsSpawnReduceConfig LoadConfig(ICoreServerAPI api)
    {
        try
        {
            LoreMobsSpawnReduceConfig config = api.LoadModConfig<LoreMobsSpawnReduceConfig>(ConfigFileName);
            if (config == null)
            {
                config = new LoreMobsSpawnReduceConfig();
                api.StoreModConfig(config, ConfigFileName);
                Mod.Logger.Notification("Created default config file {0}", ConfigFileName);
            }
            return config;
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to read config file {0}, using defaults. The file was left unchanged. Error: {1}", ConfigFileName, e.Message);
            return new LoreMobsSpawnReduceConfig();
        }
    }

    private bool OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
    {
        AssetLocation code = properties?.Code;
        if (code == null) return true;

        return ShouldAllowSpawn(code);
    }

    /// <summary>
    /// Returns false for a (1 - multiplier) fraction of spawn attempts of entities matching the configured patterns.
    /// </summary>
    public bool ShouldAllowSpawn(AssetLocation code)
    {
        foreach (AssetLocation pattern in patterns)
        {
            if (WildcardUtil.Match(pattern, code))
            {
                return sapi.World.Rand.NextDouble() < multiplier;
            }
        }

        return true;
    }
}
