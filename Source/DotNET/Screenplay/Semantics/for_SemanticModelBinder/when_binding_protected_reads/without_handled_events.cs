// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class without_handled_events : given.a_protected_read_model
{
    [Fact] void should_refuse_an_empty_handled_event_set() => Refusal(Bind(Source.Replace("        from Recorded\n          id = $eventSourceId\n          value = value\n        remove with Removed\n", string.Empty, StringComparison.Ordinal)), SemanticDecisionReadRefusal.EmptyHandledSet);
}
