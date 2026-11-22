// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.given;

public class a_semantic_value_vector : Specification
{
    protected SemanticValue _value;
    protected Exception _exception;
    protected SemanticId _propertyId;

    void Establish() => _propertyId = SemanticId.Parse("sem1:0000000000000000000000000000000000000000000000000000000000000001");
}
