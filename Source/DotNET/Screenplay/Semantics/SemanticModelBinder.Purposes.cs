// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    static IEnumerable<PurposeReferenceSyntax> PurposeReferences(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => module.Purposes.Concat(module.Features.SelectMany(PurposeReferences)));

    static IEnumerable<PurposeReferenceSyntax> PurposeReferences(FeatureSyntax feature) =>
        feature.Purposes.Concat(feature.Slices.SelectMany(slice => slice.Purposes)).Concat(feature.Features.SelectMany(PurposeReferences));
}
