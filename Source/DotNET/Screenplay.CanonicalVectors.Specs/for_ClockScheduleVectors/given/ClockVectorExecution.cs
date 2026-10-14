// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors.given;

internal static class ClockVectorExecution
{
    internal const string RegenerationVariable = "SCREENPLAY_REGENERATE_CLOCK_VECTORS";
    static readonly SemanticId ReactionId = SemanticId.Parse("sem1:3a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9");
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string FixturePath([CallerFilePath] string path = "")
    {
        var directory = Path.GetDirectoryName(path)!;
        while (!File.Exists(Path.Combine(directory, "Screenplay.slnx"))) directory = Path.GetDirectoryName(directory)!;

        return Path.Combine(directory, "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ClockSchedules/clock-schedules.v1.json");
    }

    internal static (bool Refused, string[] Due) Run(JsonNode vector)
    {
        var triggers = vector["triggers"] is JsonArray many ? many.Select(value => Trigger(value!)) : [Trigger(vector["trigger"]!)];
        var reaction = new SemanticReaction(ReactionId, vector["name"]!.GetValue<string>(), [.. triggers]);
        var accepted = SemanticClock.TryFindDue([reaction], Instant(vector["from"]!), Instant(vector["to"]!), out var due);

        return (!accepted, [.. due.Select(value => value.At.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))]);
    }

    internal static void Regenerate()
    {
        var path = FixturePath();
        var document = JsonNode.Parse(File.ReadAllBytes(path))!;
        foreach (var vector in document["vectors"]!.AsArray())
        {
            var result = Run(vector!);
            if (vector!["triggers"] is not null)
            {
                vector["refused"] = result.Refused;
                vector["count"] = result.Due.Length;
            }
            else
            {
                result.Refused.ShouldBeFalse();
                vector["due"] = new JsonArray([.. result.Due.Select(value => (JsonNode?)JsonValue.Create(value))]);
            }
        }

        File.WriteAllText(path, document.ToJsonString(Options).ReplaceLineEndings("\n") + "\n", new UTF8Encoding(false));
        Assert.Fail($"Clock schedule vectors regenerated; review the diff, update ClockScheduleVectors.Sha256 deliberately, rebuild and rerun without {RegenerationVariable}.");
    }

    static DateTimeOffset Instant(JsonNode value) => DateTimeOffset.Parse(value.GetValue<string>(), CultureInfo.InvariantCulture);

    static SemanticReactionTrigger Trigger(JsonNode value) => value["kind"]!.GetValue<string>() switch
    {
        "interval" => new(SemanticReactionTriggerKind.Interval) { Every = value["everySeconds"]!.GetValue<long>() },
        "schedule" => new(SemanticReactionTriggerKind.Schedule)
        {
            At = value["secondOfDay"]!.GetValue<int>(),
            OnDayOfWeek = value["dayOfWeek"]?.GetValue<int>(),
            OnDayOfMonth = value["dayOfMonth"]?.GetValue<int>()
        },
        _ => throw new InvalidSemanticContract("Unknown clock vector trigger kind.")
    };
}
