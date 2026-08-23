// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_reading_an_unknown_field : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticIdentityCatalogSerializer.Serialize(_catalog));
        var unknown = json.Replace("\"documents\":", "\"unknown\":true,\"documents\":", StringComparison.Ordinal);
        _exception = Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(unknown)));
    }

    [Fact] void should_reject_the_catalog() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
