// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_enumeration_keys : given.a_protected_read_model
{
    [Fact]
    void should_refuse_an_enumeration_input_even_with_a_matching_view_key()
    {
        var source = "concept Key : Enum\n  one\n  two\n" + Source.Replace("id String", "id Key", StringComparison.Ordinal);
        Refusal(Bind(source), SemanticDecisionReadRefusal.KeyType);
    }
}
