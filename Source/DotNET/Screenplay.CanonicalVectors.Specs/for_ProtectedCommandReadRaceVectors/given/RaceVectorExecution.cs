// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;

// Runs a vector through the public host surface only: EstablishWorld, Decide, ExtendWorld and Commit.
internal static class RaceVectorExecution
{
    internal const string RegenerationVariable = "SCREENPLAY_REGENERATE_PROTECTED_READ_RACES";
    internal const string RegenerationVariableV1R2 = "SCREENPLAY_REGENERATE_PROTECTED_READ_RACES_V1R2";
    internal const string ApplicationName = "ProtectedCommandReadRaces";
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string ResourcePath(string name, [CallerFilePath] string path = "") => FixturePath("races-v1", name, path);

    internal static string ResourcePathV1R2(string name, [CallerFilePath] string path = "") => FixturePath("races-v1r2", name, path);

    static string FixturePath(string revision, string name, string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        while (!File.Exists(Path.Combine(directory, "Screenplay.slnx"))) directory = Path.GetDirectoryName(directory)!;

        return Path.Combine(directory, "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ProtectedCommandReads", revision, name);
    }

    internal static string Format(JsonNode document) => document.ToJsonString(Options).ReplaceLineEndings("\n") + "\n";

    internal static CompilationResult<SemanticCompilation> Compile(string source, ImmutableArray<byte> catalogBytes)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(catalogBytes.AsSpan());
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("races"), "races", "races.play", source);

        return new SemanticModelCompiler().Compile(ApplicationName, SemanticDocumentSet.Create([document], catalog));
    }

    internal static SemanticExecutionPlan Plan(ExecutableSemanticModel model)
    {
        var compilation = SemanticExecutionPlan.Compile(model);
        Assert.True(compilation.Success, string.Join('\n', compilation.Issues.Select(issue => issue.Details)));

        return compilation.Plan!;
    }

    // Regenerates the model files first, then the vectors from the authoring table, then fails so the result is reviewed.
    internal static void Regenerate()
    {
        var catalogBytes = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName))).ToImmutableArray();
        var source = File.ReadAllText(ResourcePath("races.play"));
        var compiled = Compile(source, catalogBytes);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compiled.Value!.Model;
        File.WriteAllBytes(ResourcePath("identity-catalog.json"), [.. catalogBytes]);
        File.WriteAllBytes(ResourcePath("esm-v25.json"), SemanticModelSerializer.Serialize(model));
        File.WriteAllText(ResourcePath("semantic-revision.txt"), $"{model.Revision}\n", new UTF8Encoding(false));

        var plan = Plan(model);
        var vectors = new JsonArray();
        foreach (var raceCase in RaceVectorCases.All) vectors.Add(Author(plan, raceCase));
        var document = new JsonObject
        {
            ["version"] = 1,
            ["model"] = new JsonObject { ["application"] = ApplicationName, ["esm"] = 25, ["source"] = "races.play" },
            ["vectors"] = vectors
        };
        File.WriteAllText(ResourcePath("races.v1.json"), Format(document), new UTF8Encoding(false));
        Assert.Fail($"Protected command read race vectors regenerated; review the diff, update ProtectedCommandReadRaceVectors.Sha256 deliberately, rebuild and rerun without {RegenerationVariable}.");
    }

    // Shares the frozen source/model with V1 and never writes any V1 resources.
    internal static void RegenerateV1R2()
    {
        var original = JsonNode.Parse(File.ReadAllText(ResourcePath("races.v1.json")))!;
        var model = SemanticModelSerializer.Deserialize(File.ReadAllBytes(ResourcePath("esm-v25.json")));
        var plan = Plan(model);
        var vectors = new JsonArray();
        foreach (var raceCase in RaceVectorCases.AllV1R2) vectors.Add(Author(plan, raceCase));
        var previous = original["vectors"]!.AsArray();
        Assert.Equal(16, previous.Count);
        Assert.Equal(previous.Count + 1, vectors.Count);
        for (var index = 0; index < previous.Count; index++)
        {
            Assert.True(JsonNode.DeepEquals(previous[index], vectors[index]), $"V1 vector {index} changed; the additive revision must preserve it.");
        }

        var document = original.DeepClone();
        document["vectors"] = vectors;
        var destination = ResourcePathV1R2("races.v1r2.json");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, Format(document), new UTF8Encoding(false));
        Assert.Fail($"Protected command read race V1R2 vectors regenerated; review the diff, pin ProtectedCommandReadRaceVectors.Sha256V1R2, rebuild and rerun without {RegenerationVariableV1R2}.");
    }

    internal static JsonObject Author(SemanticExecutionPlan plan, RaceCase raceCase)
    {
        var vector = new JsonObject
        {
            ["name"] = raceCase.Name,
            ["description"] = raceCase.Description,
            ["decided"] = Facts(raceCase.Decided),
            ["request"] = Request(raceCase.Request)
        };
        var commits = new JsonArray();
        foreach (var commit in raceCase.Commits)
        {
            var entry = new JsonObject();
            if (commit.Later is not null) entry["later"] = Facts(commit.Later);
            else entry["history"] = Facts(commit.History!);
            commits.Add(entry);
        }

        vector["commits"] = commits;
        var observed = Run(plan, vector);
        vector["dependencies"] = observed.Dependencies;
        for (var index = 0; index < raceCase.Commits.Length; index++)
        {
            var expect = observed.Commits[index]!;
            Assert.True(Describe(expect) == raceCase.Commits[index].Intended, $"{raceCase.Name}[{index}]: observed {expect.ToJsonString()}, intended {raceCase.Commits[index].Intended}.");
            commits[index]!["expect"] = expect.DeepClone();
        }

        // Keep a stable property order: dependencies sit before the commits.
        var ordered = new JsonObject();
        foreach (var name in new[] { "name", "description", "decided", "request", "dependencies", "commits" }) ordered[name] = vector[name]!.DeepClone();

        return ordered;
    }

    internal static void Verify(SemanticExecutionPlan plan, JsonNode vector)
    {
        var name = vector["name"]!.GetValue<string>();
        var observed = Run(plan, vector);
        Assert.True(JsonNode.DeepEquals(observed.Dependencies, vector["dependencies"]), $"{name}: dependencies differ: {observed.Dependencies.ToJsonString()}");
        var commits = vector["commits"]!.AsArray();
        observed.Commits.Count.ShouldEqual(commits.Count);
        for (var index = 0; index < commits.Count; index++)
        {
            Assert.True(JsonNode.DeepEquals(observed.Commits[index], commits[index]!["expect"]), $"{name}[{index}]: outcome differs: {observed.Commits[index]!.ToJsonString()}");
        }
    }

    static (JsonArray Dependencies, JsonArray Commits) Run(SemanticExecutionPlan plan, JsonNode vector)
    {
        var name = vector["name"]!.GetValue<string>();
        var evaluator = new SemanticEvaluator();
        var established = evaluator.EstablishWorld(plan, [.. vector["decided"]!.AsArray().Select(fact => Fact(plan, fact!))]);
        Assert.True(established is SemanticAccepted, $"{name}: the decided facts were not accepted: {established}");
        var decided = established.World;
        var decision = evaluator.Decide(plan, decided, ExecutionRequest(plan, vector["request"]!));
        var dependencies = new JsonArray();
        foreach (var entry in decision.Dependencies.Entries)
        {
            dependencies.Add(new JsonObject
            {
                ["key"] = ((SemanticTextValue)entry.Key).Value,
                ["eventContracts"] = new JsonArray([.. entry.EventContracts.Select(id => plan.Events[id].Name).Order(StringComparer.Ordinal).Select(value => (JsonNode?)JsonValue.Create(value))])
            });
        }

        var commits = new JsonArray();
        foreach (var commit in vector["commits"]!.AsArray())
        {
            SemanticExecutionResult world;
            if (commit!["history"] is JsonArray history)
            {
                world = evaluator.EstablishWorld(plan, [.. history.Select(fact => Fact(plan, fact!))]);
            }
            else
            {
                world = evaluator.ExtendWorld(plan, decided, [.. commit["later"]!.AsArray().Select(fact => Fact(plan, fact!))]);
            }

            Assert.True(world is SemanticAccepted, $"{name}: a commit world was not accepted: {world}");
            commits.Add(Outcome(plan, decision.Commit(world.World)));
        }

        return (dependencies, commits);
    }

    static string Describe(JsonNode expect) => expect["outcome"]!.GetValue<string>() switch
    {
        "accepted" => "accepted",
        "conflict" => "conflict",
        var outcome => $"{outcome}:{expect["category"]!.GetValue<string>()}"
    };

    static JsonObject Outcome(SemanticExecutionPlan plan, SemanticExecutionResult result)
    {
        var outcome = result switch
        {
            SemanticAccepted accepted => new JsonObject { ["outcome"] = "accepted", ["facts"] = new JsonArray([.. accepted.Facts.Select(fact => (JsonNode?)Observed(plan, fact))]) },
            SemanticConflict conflict => new JsonObject { ["outcome"] = "conflict", ["category"] = conflict.Category.ToString() },
            SemanticRejected rejected => Rejected(rejected),
            _ => throw new InvalidSemanticContract($"A race vector produced an unexpected {result.Kind} outcome.")
        };
        outcome["world"] = new JsonArray([.. result.World.Facts.Select(fact => (JsonNode?)JsonValue.Create(plan.Events[fact.EventContract].Name))]);

        return outcome;
    }

    static JsonObject Rejected(SemanticRejected rejected)
    {
        var outcome = new JsonObject { ["outcome"] = "rejected", ["category"] = rejected.Category.ToString() };
        if (!string.IsNullOrEmpty(rejected.Code)) outcome["code"] = rejected.Code;
        outcome["message"] = rejected.Details;

        return outcome;
    }

    static JsonObject Observed(SemanticExecutionPlan plan, SemanticFact fact)
    {
        var contract = plan.Events[fact.EventContract];
        var observed = new JsonObject { ["event"] = contract.Name, ["source"] = ((SemanticTextValue)fact.Destination).Value };
        if (fact.Values.Length > 0)
        {
            var values = new JsonObject();
            foreach (var property in contract.Properties)
            {
                values[property.Name] = fact.Values.Single(value => value.TargetProperty == property.Id).Value switch
                {
                    SemanticTextValue text => text.Value,
                    SemanticNullValue => null,
                    _ => throw new InvalidSemanticContract("A race vector fact holds a value that is not text.")
                };
            }

            observed["values"] = values;
        }

        return observed;
    }

    static JsonArray Facts(RaceFact[] facts) => new([.. facts.Select(fact =>
    {
        var json = new JsonObject { ["event"] = fact.Event, ["source"] = fact.Source };
        if (fact.SourceType is not null) json["sourceType"] = fact.SourceType;
        if (fact.Value is not null) json["values"] = new JsonObject { ["value"] = fact.Value };

        return (JsonNode?)json;
    })]);

    static JsonObject Request(RaceRequest request)
    {
        var json = new JsonObject { ["command"] = request.Command, ["values"] = Pairs(request.Values) };
        if (request.Generated is not null) json["generated"] = Pairs(request.Generated);

        return json;
    }

    static JsonObject Pairs((string Name, string Value)[] pairs)
    {
        var json = new JsonObject();
        foreach (var (name, value) in pairs) json[name] = value;

        return json;
    }

    static SemanticFact Fact(SemanticExecutionPlan plan, JsonNode json)
    {
        var contract = plan.Events.Values.Single(value => value.Name == json["event"]!.GetValue<string>());
        var source = SemanticValue.Text(json["source"]!.GetValue<string>());
        var type = json["sourceType"] is { } concept
            ? SemanticTypeReference.ForConcept(plan.Model.Application.Concepts.Single(value => value.Name == concept.GetValue<string>()).Id)
            : SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var values = contract.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(json["values"]![property.Name]!.GetValue<string>())));

        return new SemanticFact(contract.Id, source, [.. values]) { Context = new(new(type, source)) };
    }

    static SemanticExecutionRequest ExecutionRequest(SemanticExecutionPlan plan, JsonNode json)
    {
        var command = plan.Commands.Values.Single(value => value.Name == json["command"]!.GetValue<string>());
        var request = SemanticExecutionRequest.Create(command.Id, [.. Values(command, json["values"]!.AsObject())], []);

        return json["generated"] is JsonObject generated ? request with { GeneratedValues = [.. Values(command, generated)] } : request;
    }

    static IEnumerable<SemanticPropertyValue> Values(SemanticCommand command, JsonObject values) =>
        values.Select(pair => new SemanticPropertyValue(command.Properties.Single(property => property.Name == pair.Key).Id, SemanticValue.Text(pair.Value!.GetValue<string>())));
}
