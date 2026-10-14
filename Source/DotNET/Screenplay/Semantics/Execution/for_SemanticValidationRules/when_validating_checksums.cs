// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticValidationRules;

public class when_validating_checksums : Specification
{
    JsonElement[] _vectors;
    bool[] _matches;
    bool[] _absent;
    bool _required;

    void Establish()
    {
        var root = Root();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance", "generation-checksums.json")));
        _vectors = [.. document.RootElement.GetProperty("validation").EnumerateArray().Select(vector => vector.Clone())];
    }

    void Because()
    {
        _matches = [.. _vectors.Select(vector => SemanticValidationRules.Satisfies(Rule(vector), SemanticValue.Text(vector.GetProperty("value").GetString()!)))];
        _absent = [.. _vectors.Select(vector => SemanticValidationRules.Satisfies(Rule(vector), SemanticValue.Null))];
        _required = SemanticValidationRules.Satisfies(new(default, SemanticValidationRuleKind.NotEmpty, null, null), SemanticValue.Null);
    }

    [Fact] void should_check_a_nonempty_runtime_population() => _vectors.ShouldNotBeEmpty();
    [Fact] void should_execute_checksum_validation() => SemanticValidationRules.Evaluates(SemanticValidationRuleKind.Checksum).ShouldBeTrue();
    [Fact] void should_validate_only_the_exact_emitted_suffix() => _matches.ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("valid").GetBoolean())]);
    [Fact] void should_accept_absent_optional_values() => _absent.ShouldEachConformTo(value => value);
    [Fact] void should_leave_presence_to_not_empty() => _required.ShouldBeFalse();

    static SemanticValidationRule Rule(JsonElement vector) => new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text(vector.GetProperty("algorithm").GetString()!), null);

    static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
