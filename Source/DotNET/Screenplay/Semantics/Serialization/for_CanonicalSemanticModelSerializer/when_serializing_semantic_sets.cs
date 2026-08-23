// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_semantic_sets : a_valid_semantic_model
{
    string _json;
    string _firstId;
    string _secondId;

    void Because()
    {
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        var ids = _application.Concepts.Select(_ => _.Id.ToString()).Order(StringComparer.Ordinal).ToArray();
        _firstId = ids[0];
        _secondId = ids[1];
    }

    [Fact] void should_order_sets_ordinally_by_identity() => _json.IndexOf(_firstId, StringComparison.Ordinal).ShouldBeLessThan(_json.IndexOf(_secondId, StringComparison.Ordinal));
}
