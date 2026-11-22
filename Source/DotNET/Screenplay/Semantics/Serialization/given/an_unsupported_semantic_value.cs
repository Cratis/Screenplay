// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.given;

public record an_unsupported_semantic_value : SemanticValue
{
    public an_unsupported_semantic_value()
        : base(SemanticValueKind.Null)
    {
    }
}
