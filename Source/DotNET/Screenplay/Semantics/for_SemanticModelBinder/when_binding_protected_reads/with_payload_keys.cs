// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_payload_keys : given.a_protected_read_model
{
    [Fact] void should_refuse_event_payload_routing() => Refusal(Bind(Source.Replace("from Recorded", "from Recorded key value", StringComparison.Ordinal)), SemanticDecisionReadRefusal.PayloadKey);
}
