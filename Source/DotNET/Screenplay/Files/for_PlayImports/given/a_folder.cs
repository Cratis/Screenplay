// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayImports.given;

public class a_folder : Specification
{
    protected Dictionary<string, string> _documents = new(StringComparer.Ordinal);
    protected IReadOnlyList<PlacedPlayDocument> _resolved;
    protected IReadOnlyList<Diagnostic> _diagnostics;

    protected void Resolve(params string[] roots) =>
        (_resolved, _diagnostics) = PlayImports.Resolve(roots, new InMemoryPlayDocumentSource(_documents));

    protected PlayPlacement PlacementOf(string path) => _resolved.Single(document => document.Path == path).Placement;
}
