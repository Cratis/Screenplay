// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_reading_a_revision_mismatch : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticIdentityCatalogSerializer.Serialize(_catalog));
        var revisionStart = json.IndexOf("rev1:", StringComparison.Ordinal);
        var mismatch = string.Concat(json.AsSpan(0, revisionStart + 5), new string('0', 64), json.AsSpan(revisionStart + 69));
        _exception = Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(mismatch)));
    }

    [Fact] void should_reject_the_catalog() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
