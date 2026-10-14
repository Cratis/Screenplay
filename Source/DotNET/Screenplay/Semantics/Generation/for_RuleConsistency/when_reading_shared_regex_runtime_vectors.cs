// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_reading_shared_regex_runtime_vectors
{
    [Fact]
    void should_agree_with_exhaustive_runtime_languages_and_witness_order()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance", "generation-regex-runtime.json")));
        var root = document.RootElement;
        var alphabet = root.GetProperty("alphabet").GetString()!;
        var patterns = root.GetProperty("patterns").EnumerateArray().Select(value => value.GetString()!).ToArray();
        patterns.ShouldNotBeEmpty();
        foreach (var pattern in patterns)
        {
            var runtime = SemanticMatchPattern.Create(pattern);
            for (var length = root.GetProperty("minimumLength").GetInt32(); length <= root.GetProperty("maximumLength").GetInt32(); length++)
            {
                var values = new[] { string.Empty }.AsEnumerable();
                for (var position = 0; position < length; position++) values = [.. values.SelectMany(prefix => alphabet.Select(character => prefix + character))];
                var witness = values.FirstOrDefault(value => !runtime.IsMatch(value));
                var expression = new RegexFragmentR(pattern, length, boundToShape: false).Parse();
                var safe = !expression.HasNullableRepeat && new GlushkovAutomaton(expression).IsDeterministic;
                var result = RuleConsistency.Check(new("nanoid") { Size = length, Alphabet = alphabet }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null));
                var expected = safe ? RuleConsistencyStatus.Proven : RuleConsistencyStatus.Unproven;
                if (witness is not null) expected = RuleConsistencyStatus.Refuted;
                result.Status.ShouldEqual(expected);
                result.Witness.ShouldEqual(witness);
            }
        }
    }

    static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
