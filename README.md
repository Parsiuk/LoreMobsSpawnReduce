# Lore Mobs Spawn Reduce

Server-side Vintage Story (1.22.x) mod that reduces the spawn rate of lore mobs: drifters, shivers, bowtorns, locusts and bells. By default only 1% of their spawn attempts succeed.

## How it works

Lore mobs reach the world in two ways, and the mod hooks both:

1. **Natural and storm spawning.** The mod listens to the server's `OnTrySpawnEntity` event and rejects a configurable fraction of spawn attempts for matching entities. This covers normal darkness/underground spawning (drifters, shivers, bowtorns, bells, sawblade locusts) as well as temporal storm and rift waves.
2. **Spawner blocks, such as locust nests.** Spawner blocks create entities directly and never fire `OnTrySpawnEntity`. The mod therefore uses a Harmony prefix on `BlockEntitySpawner.DoSpawn`, the routine every spawner block uses. Locust nests (`locustnest-cage`, `locustnest-metalspike-*`, `locustnest-stalagmite-*`) are the only source of bronze and corrupt locusts, so without this patch those locusts would not be affected at all. A rejected nest spawn still starts the nest's cooldown, so the rate actually drops instead of the spawn just being delayed.

### What is not affected

- Entities spawned by commands (`/entity spawn` and similar) or by creative spawn items.
- Entities summoned by other mobs, e.g. a bell's minis (`bellmini-*`, which `bell-*` does not match).

Spawner blocks you place yourself in creative are affected when they spawn a matching entity.

## Config

Created on first start at `VintagestoryData/ModConfig/loremobsspawnreduce.json`:

```json
{
  "SpawnChanceMultiplier": 0.01,
  "EntityCodes": ["drifter-*", "shiver-*", "bowtorn-*", "locust-*", "bell-*"]
}
```

- `SpawnChanceMultiplier`: fraction of spawn attempts allowed. `0` means never, `1` means the vanilla rate. Values are clamped to `[0, 1]`. At `1` the mod does nothing: no event hook and no Harmony patch.
- `EntityCodes`: entity code wildcards affected by the multiplier. The domain defaults to `game`; use `domain:code-*` for mobs from other mods. This list **replaces** the defaults, so removing an entry turns the mod off for that mob, and an empty list turns the mod off entirely. Duplicate entries are ignored.

Changes take effect after restarting the world or server.

## Checking that it works

After starting the server, `VintagestoryData/Logs/server-main.log` should contain:

```
[loremobsspawnreduce] Spawn chance multiplier 0.01 applied to: game:drifter-*, game:shiver-*, game:bowtorn-*, game:locust-*, game:bell-*
[loremobsspawnreduce] Patched spawner blocks (locust nests etc.)
```

If the patch line is replaced by a warning or an error, the game's spawner code has changed. Natural and storm spawns are still reduced, but locust nests are not.

## Build

Requires the .NET 10 SDK. The game path defaults to `~/Apps/VintageStory`; override it with `-p:VINTAGE_STORY=/path/to/VintageStory`. The project compiles against the game's own `VintagestoryAPI.dll`, `Mods/VSSurvivalMod.dll`, `Lib/0Harmony.dll` and `Lib/Newtonsoft.Json.dll`. None of them are bundled into the mod.

```sh
dotnet build -c Release
```

The installable mod is written to `bin/LoreMobsSpawnReduce_<version>.zip`. Copy it to `VintagestoryData/Mods` and remove any older version. Only the server (or singleplayer host) needs it.

## Changelog

### 1.1.0
- Spawner blocks are now covered, including locust nests, so bronze and corrupt locusts are reduced.
- Fixed `EntityCodes` from the config being added to the defaults instead of replacing them. Before this fix, removing an entry had no effect and the log listed every pattern twice.

### 1.0.0
- Initial release: reduces natural, temporal storm and rift spawns of lore mobs.
