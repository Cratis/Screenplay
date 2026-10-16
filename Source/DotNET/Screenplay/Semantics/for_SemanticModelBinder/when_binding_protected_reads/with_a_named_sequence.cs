// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_a_named_sequence : given.a_protected_read_model
{
    [Fact] void should_refuse_a_named_event_sequence() => Refusal(Bind(Source.Replace("projection Build => View", "projection Build => View\n        sequence other", StringComparison.Ordinal)), SemanticDecisionReadRefusal.NamedSourceOrStream);
}
