using System.Collections.Generic;
using Newtonsoft.Json;

namespace LoreMobsSpawnReduce;

public class LoreMobsSpawnReduceConfig
{
    /// <summary>
    /// Fraction of spawn attempts that are allowed for matching entities.
    /// 0 = never spawn, 1 = vanilla spawn rate.
    /// </summary>
    public double SpawnChanceMultiplier { get; set; } = 0.01;

    /// <summary>
    /// Entity code wildcards (domain optional, defaults to "game") affected by the multiplier.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<string> EntityCodes { get; set; } = new()
    {
        "drifter-*",
        "shiver-*",
        "bowtorn-*",
        "locust-*",
        "bell-*"
    };
}
