// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds an exact model instance only after SemanticCompilation.Create cross-checks its identity catalog.
/// Copied, deserialized and hand-built models have no binding, so execution fails closed.
/// </summary>
internal static class SemanticModelProvenance
{
    static readonly ConditionalWeakTable<ExecutableSemanticModel, Entry> _entries = [];

    internal static void Record(ExecutableSemanticModel model, ApplicationIdentity application, IReadOnlyDictionary<SemanticAddress, SemanticId> declarations)
    {
        lock (_entries)
        {
            if (!_entries.TryGetValue(model, out var entry))
            {
                _entries.Add(model, new(application, declarations));
            }
            else if (entry.Application != application || !entry.Application.IsSet)
            {
                _entries.Remove(model);
                _entries.Add(model, new(default, ImmutableDictionary<SemanticAddress, SemanticId>.Empty));
            }
        }
    }

    internal static bool Declares(ExecutableSemanticModel model, SemanticAddress? owner, SemanticId id) =>
        owner is not null && _entries.TryGetValue(model, out var entry) &&
        entry.Declarations.TryGetValue(owner, out var declared) && declared == id;

    sealed record Entry(ApplicationIdentity Application, IReadOnlyDictionary<SemanticAddress, SemanticId> Declarations);
}
