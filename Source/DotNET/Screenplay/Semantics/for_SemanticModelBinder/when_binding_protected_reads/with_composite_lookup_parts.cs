// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_composite_lookup_parts : given.a_protected_read_model
{
    [Fact] void should_refuse_composite_lookup_parts() => Refusal(Bind(Source.Replace("reads View by id", "reads View\n          by\n            first = id\n            second = value", StringComparison.Ordinal)), SemanticDecisionReadRefusal.ByParts);
}
