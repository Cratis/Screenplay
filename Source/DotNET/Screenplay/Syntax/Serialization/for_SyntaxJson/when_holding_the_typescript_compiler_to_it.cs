// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

// The TypeScript compiler (Source/Screenplay/Compiler) writes the syntax it reads for each document of a
// shared corpus as golden files, and holds itself to them. This holds the C# compiler to the same files:
// for every member the TypeScript compiler models, the C# syntax has to say the same. A member only the C#
// compiler models is not compared - the TypeScript compiler leaves it out rather than claiming it empty.
public class when_holding_the_typescript_compiler_to_it : Specification
{
    readonly List<string> _mismatches = [];
    List<(string Name, string Path)> _documents;
    int _compared;
    bool _exact;

    void Establish() => _documents = [.. Documents()];

    void Because()
    {
        foreach (var (name, path) in _documents)
        {
            var parsed = new ScreenplayCompiler().Parse(File.ReadAllText(Path.Combine(Root(), path)));
            using var golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(Conformance(), $"{name}.syntax.json")));

            // Unmarked vectors are frozen at the Legacy wire form; only Exact vectors carry the expanded members.
            _exact = golden.RootElement.TryGetProperty("sourceOptions", out _);

            // Old vectors remain byte-for-byte fixtures. Decode their additive omissions as defaults;
            // new vectors must explicitly include every new member.
            var expected = name.StartsWith("source-stream", StringComparison.Ordinal) ? golden.RootElement : WithSourceStreamDefaults(golden.RootElement);
            Compare(expected, SyntaxJson.Serialize(parsed.Value!), $"{name}: $");
        }
    }

    // Without these two the comparison could pass by reading nothing - a moved manifest or an emptied golden
    // file would otherwise turn it into a check of nothing against nothing.
    [Fact] void should_hold_documents() => _documents.Count.ShouldBeGreaterThan(5);
    [Fact] void should_compare_members() => _compared.ShouldBeGreaterThan(5000);

    [Fact] void should_agree_on_every_member_the_typescript_compiler_models() => Report().ShouldEqual(string.Empty);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    static string Conformance() => Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance");

    static IEnumerable<(string Name, string Path)> Documents()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Conformance(), "manifest.json")));
        return [.. manifest.RootElement.GetProperty("documents").EnumerateArray()
            .Select(document => (document.GetProperty("name").GetString()!, document.GetProperty("path").GetString()!))];
    }

    static JsonElement WithSourceStreamDefaults(JsonElement golden)
    {
        var node = JsonNode.Parse(golden.GetRawText())!;
        AddDefaults(node);
        return JsonSerializer.SerializeToElement(node);
    }

    static void AddDefaults(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["kind"]?.GetValue<string>() == "ApplicationSyntax" && !obj.ContainsKey("eventSources")) obj["eventSources"] = new JsonArray();
            if (obj["kind"]?.GetValue<string>() == "CommandSyntax" && !obj.ContainsKey("stream")) obj["stream"] = null;
            if (obj["kind"]?.GetValue<string>() == "CommandSyntax" && !obj.ContainsKey("streamCandidates")) obj["streamCandidates"] = new JsonArray();
            foreach (var child in obj.Select(entry => entry.Value).OfType<JsonNode>()) AddDefaults(child);
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.OfType<JsonNode>()) AddDefaults(child);
        }
    }

    // The members exact numeric source introduced. An unmarked document writes none of them.
    static readonly HashSet<string> ExactOnly =
    [
        "ApplicationSyntax.policies", "ApplicationSyntax.seeds", "ProducesSyntax.when", "ProjectionSyntax.key",
        "ConceptSyntax.validations", "DeclarativeValidateSyntax.requirements", "CodeValidateSyntax.code",
        "QueryParameterSyntax.source", "ReactionSyntax.where", "InvokesSyntax.mappings",
        "FromSyntax.key", "FromSyntax.parentKey", "EventSpecSyntax.key", "ProjectionEntersOnSyntax.key", "RemoveViaJoinSyntax.key",
        "RemoveWithSyntax.key", "RemoveWithSyntax.parentKey", "ChildrenSyntax.identifiedBy", "SetMappingSyntax.source",
        "AddMappingSyntax.value", "SubtractMappingSyntax.value", "CaptureSyntax.map", "CaptureChildrenSyntax.map",
        "CaptureNestedSyntax.map", "CaptureAppendSyntax.when", "CaptureAppendSyntax.mappings", "CaptureAppendSyntax.tags",
        "SpecificationSyntax.thenAbsentReadModels", "SpecificationSyntax.thenQueries"
    ];

    static bool SameScalar(JsonElement golden, JsonElement actual) => golden.ValueKind switch
    {
        JsonValueKind.String => actual.ValueKind == JsonValueKind.String && golden.GetString() == actual.GetString(),
        JsonValueKind.Number => actual.ValueKind == JsonValueKind.Number && golden.GetDouble() == actual.GetDouble(),
        _ => golden.ValueKind == actual.ValueKind
    };

    void Compare(JsonElement golden, JsonElement actual, string path)
    {
        switch (golden.ValueKind)
        {
            case JsonValueKind.Object when actual.ValueKind == JsonValueKind.Object:
                if (actual.TryGetProperty("kind", out var actualKind))
                {
                    string[] required = actualKind.GetString() switch
                    {
                        "ApplicationSyntax" => ["systems", "policies", "seeds", "eventSources"],
                        "SliceSyntax" => ["operations"],
                        "ProducesSyntax" when actual.TryGetProperty("stream", out _) => ["inlineOperation", "when", "stream"],
                        "ProducesSyntax" => ["inlineOperation", "when"],
                        "ProjectionSyntax" => ["key"],
                        "LiteralExpressionSyntax" when actual.GetProperty("value").ValueKind == JsonValueKind.Object => ["value"],
                        "ConceptSyntax" => ["validations"],
                        "DeclarativeValidateSyntax" => ["requirements"],
                        "CodeValidateSyntax" => ["code"],
                        "QueryParameterSyntax" => ["source"],
                        "ReactionSyntax" when actual.TryGetProperty("from", out _) => ["where", "from", "runsAs"],
                        "ReactionSyntax" => ["where", "runsAs"],
                        "ReactionIdentitySyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        "InvokesSyntax" when actual.TryGetProperty("onRefused", out _) => ["mappings", "onRefused"],
                        "InvokesSyntax" => ["mappings"],
                        "FromSyntax" => ["key", "parentKey"],
                        "EventSpecSyntax" or "ProjectionEntersOnSyntax" or "RemoveViaJoinSyntax" => ["key"],
                        "RemoveWithSyntax" => ["key", "parentKey"],
                        "ChildrenSyntax" => ["identifiedBy"],
                        "SetMappingSyntax" => ["source"],
                        "AddMappingSyntax" or "SubtractMappingSyntax" => ["value"],
                        "CaptureSyntax" or "CaptureChildrenSyntax" or "CaptureNestedSyntax" => ["map"],
                        "CaptureAppendSyntax" => ["when", "mappings", "tags"],
                        "ExpressionKeySyntax" or "CompositeKeySyntax" or "KeyPartSyntax" or
                        "TemplateExpressionSyntax" or "TemplateInterpolationSyntax" or "TemplateTextSyntax" or
                        "EventSourceIdExpressionSyntax" or "EventContextExpressionSyntax" or "CausedByExpressionSyntax" or
                        "CaptureMapEntrySyntax" or "CaptureTranslationSyntax" or "CaptureSplitSyntax" or "CaptureWhenSyntax" or
                        "PolicySyntax" or "AuthenticatedConditionSyntax" or "RoleConditionSyntax" or "ClaimConditionSyntax" or
                        "LogicalPolicyConditionSyntax" or "NotPolicyConditionSyntax" or "SpecificationAbsentReadModelSyntax" or "SpecificationQuerySyntax" or "SeedSyntax" or "SeedGroupSyntax" or "SeedEventSyntax" or "RequirementSyntax" or
                        "ComparisonConditionSyntax" or "LogicalConditionSyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        "SystemSyntax" or "OperationSyntax" or "OperationPhaseSyntax" or "SpecificationOperationFailureSyntax" or "SpecificationOperationSyntax" or "SpecificationCompensatedSyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        "PropertySyntax" => ["isGenerated"],
                        "CommandSyntax" => ["response", "handler", "stream", "streamCandidates"],
                        "ObserverFilterSyntax" or "EventSourceSyntax" or "EventStreamSyntax" or "EventStreamIdPartSyntax" or "CommandStreamSyntax" or "SpecificationStreamSyntax" or "SpecificationNoStreamSyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],

                        // Legacy only models rule payloads on wire when the rule opts into an implementation wrapper.
                        "ValidationRuleSyntax" when !_exact && actual.GetProperty("implementation").ValueKind == JsonValueKind.Null => ["kind", "message", "property", "rule", "severity", "value"],
                        "ValidationRuleSyntax" or "HandlerSyntax" or "ImplementationSyntax" or "ImplementationHintSyntax" or "FileReferenceSyntax" or "CodeBlockSyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        "SpecificationCommandSyntax" => ["generatedValues"],
                        "InvocationRefusalSyntax" or "RefusalExpressionSyntax" or "SpecificationRedeliverySyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        "SpecificationSyntax" when actual.TryGetProperty("whenRedelivered", out _) => ["whenRedelivered"],
                        "SpecificationSyntax" => ["thenReturns", "thenDenied", "givenOperationFailures", "thenOperations", "thenCompensated", "thenAbsentReadModels", "thenQueries"],
                        "ScalarCommandResponseSyntax" or "RecordCommandResponseSyntax" or "ResponseFieldSyntax" or "PropertyResponseSourceSyntax" or "ScalarSpecificationReturnSyntax" or "RecordSpecificationReturnSyntax" => [.. actual.EnumerateObject().Select(member => member.Name)],
                        _ => []
                    };
                    var kindName = actualKind.GetString()!;
                    foreach (var member in required.Where(member => _exact || !ExactOnly.Contains($"{kindName}.{member}")))
                    {
                        if (!golden.TryGetProperty(member, out _)) _mismatches.Add($"{path}.{member}: the TypeScript syntax omits a required response member");
                        if (!actual.TryGetProperty(member, out _)) _mismatches.Add($"{path}.{member}: the C# syntax omits a required response member");
                    }
                }

                foreach (var member in golden.EnumerateObject())
                {
                    _compared++;
                    if (actual.TryGetProperty(member.Name, out var value))
                    {
                        Compare(member.Value, value, $"{path}.{member.Name}");
                    }
                    else
                    {
                        _mismatches.Add($"{path}.{member.Name}: the C# syntax has no such member");
                    }
                }

                break;

            case JsonValueKind.Array when actual.ValueKind == JsonValueKind.Array && golden.GetArrayLength() == actual.GetArrayLength():
                var index = 0;
                foreach (var (expected, item) in golden.EnumerateArray().Zip(actual.EnumerateArray()))
                {
                    Compare(expected, item, $"{path}[{index++}]");
                }

                break;

            case JsonValueKind.Object or JsonValueKind.Array:
                _mismatches.Add($"{path}: TypeScript has {Describe(golden)}, C# has {Describe(actual)}");
                break;

            default:
                if (!SameScalar(golden, actual))
                {
                    _mismatches.Add($"{path}: TypeScript has {golden.GetRawText()}, C# has {actual.GetRawText()}");
                }

                break;
        }
    }

    static string Describe(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Array => $"{element.GetArrayLength()} item(s)",
        JsonValueKind.Object when element.TryGetProperty("kind", out var kind) => $"a {kind.GetString()}",
        _ => element.GetRawText()
    };

    string Report() =>
        _mismatches.Count == 0
            ? string.Empty
            : $"The TypeScript compiler reads {_mismatches.Count} member(s) differently from the C# compiler:{Environment.NewLine}" +
              string.Join(Environment.NewLine, _mismatches.Take(50));
}
