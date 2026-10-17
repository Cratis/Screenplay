// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_every_refusal_reason_declared : Specification
{
    string[] _declared;

    void Establish() => _declared = [.. typeof(DiagnosticCodes).GetField(nameof(DiagnosticCodes.UnprotectableDecisionRead))!
        .GetCustomAttributes(typeof(DiagnosticArgumentDefinitionsAttribute), false).Cast<DiagnosticArgumentDefinitionsAttribute>().Single().Definitions
        .Single(definition => definition.StartsWith("reason:keyword=", StringComparison.Ordinal))
        .Split(';')[0].Split('=')[1].Split('|')];

    [Fact] void should_list_every_refusal_reason() => _declared.OrderBy(value => value, StringComparer.Ordinal).ShouldEqual(Enum.GetNames<SemanticDecisionReadRefusal>().OrderBy(value => value, StringComparer.Ordinal));
}
