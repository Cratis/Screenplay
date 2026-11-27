// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_an_invocation_outcome : Specification
{
    SemanticInvocationValueExpression _outcome;
    string _serialized;

    void Establish() => _outcome = new("outcome", string.Empty, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));

    void Because() => _serialized = Encoding.UTF8.GetString(SemanticModelCanonicalJson.SerializeExpressionVector([_outcome]));

    [Fact] void should_write_the_source_without_a_member_suffix() => _serialized.ShouldEqual("""[{"kind":"invocationValue","member":"outcome","type":{"kind":"primitive","primitive":"string","target":null,"collection":false,"optional":false}}]""");
}
#endif
