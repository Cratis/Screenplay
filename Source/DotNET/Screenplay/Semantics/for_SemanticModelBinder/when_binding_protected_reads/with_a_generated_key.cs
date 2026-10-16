// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_a_generated_key : given.a_protected_read_model
{
    [Fact] void should_refuse_a_key_generated_after_reads() => Refusal(Bind("concept Id : Uuid\n" + Source.Replace("id String identifier", "id Id generated", StringComparison.Ordinal).Replace("id String", "id Id", StringComparison.Ordinal)), SemanticDecisionReadRefusal.GeneratedKey);
}
