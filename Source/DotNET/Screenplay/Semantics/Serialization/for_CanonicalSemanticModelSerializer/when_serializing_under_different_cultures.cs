// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_under_different_cultures : a_valid_semantic_model
{
    CultureInfo _originalCulture;
    byte[] _french;
    byte[] _turkish;

    void Establish() => _originalCulture = CultureInfo.CurrentCulture;

    void Because()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _french = SemanticModelSerializer.Serialize(_model);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        _turkish = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_be_culture_invariant() => _turkish.SequenceEqual(_french).ShouldBeTrue();

    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;
}
