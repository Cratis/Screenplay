// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticCodes;

public class when_declaring_decision_read_refusals : Specification
{
    string[] _declared;
    string[] _mapped;

    void Because()
    {
        _declared = [.. typeof(DiagnosticCodes).GetField(nameof(DiagnosticCodes.UnprotectableDecisionRead))!
            .GetCustomAttributes(typeof(DiagnosticArgumentDefinitionsAttribute), false).Cast<DiagnosticArgumentDefinitionsAttribute>().Single().Definitions
            .Single(definition => definition.StartsWith("reason:keyword=", StringComparison.Ordinal))
            .Split(';')[0].Split('=')[1].Split('|')];
        _mapped = [.. Enum.GetValues<SemanticDecisionReadRefusal>().Select(refusal => refusal.Keyword())];
    }

    [Fact] void should_map_every_refusal_to_a_keyword() => _mapped.Length.ShouldEqual(Enum.GetValues<SemanticDecisionReadRefusal>().Length);
    [Fact] void should_map_every_refusal_to_a_distinct_keyword() => _mapped.Distinct().Count().ShouldEqual(_mapped.Length);
    [Fact] void should_declare_exactly_the_mapped_keywords() => _declared.OrderBy(value => value, StringComparer.Ordinal).ShouldEqual(_mapped.OrderBy(value => value, StringComparer.Ordinal));
    [Fact] void should_spell_every_keyword_in_lowercase_with_hyphens() => _mapped.ShouldEachConformTo(value => value.All(character => char.IsAsciiLetterLower(character) || character == '-'));
}
