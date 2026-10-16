// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_a_numeric_key : given.a_protected_read_model
{
    [Fact] void should_refuse_a_non_identity_key_type() => Refusal(Bind(Source.Replace("id String", "id Int", StringComparison.Ordinal)), SemanticDecisionReadRefusal.KeyType);
}
