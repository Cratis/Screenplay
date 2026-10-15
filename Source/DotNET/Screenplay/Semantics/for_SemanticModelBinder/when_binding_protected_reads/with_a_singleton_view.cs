// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_a_singleton_view : given.a_protected_read_model
{
    [Fact] void should_refuse_an_unkeyed_view() => Refusal(Bind(Source.Replace("id String key", "id String", StringComparison.Ordinal).Replace("      query Get => View optional\n        by id String", string.Empty, StringComparison.Ordinal)), SemanticDecisionReadRefusal.Singleton);
}
