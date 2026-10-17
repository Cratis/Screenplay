// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks Chronicle's one-use-per-event rule independently at each projection level.
/// </summary>
internal static class ProjectionEventScopeValidator
{
    /// <summary>
    /// Reports second and later references in each level's event and join sets.
    /// </summary>
    /// <param name="context">The parsing context receiving diagnostics.</param>
    /// <param name="projection">The authored projection name.</param>
    /// <param name="blocks">The blocks at this level.</param>
    /// <param name="scope">The description of this level.</param>
    public static void Validate(ParserContext context, string projection, IEnumerable<ProjectionBlockSyntax> blocks, string scope = "the root level")
    {
        var levelEvents = new HashSet<string>(StringComparer.Ordinal);
        var joinEvents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var reference in from.Events)
                    {
                        Check(reference.Event, reference.Location, levelEvents);
                    }
                    break;
                case JoinSyntax join:
                    foreach (var reference in join.Events)
                    {
                        Check(reference.Event, reference.Location, joinEvents);
                    }
                    break;
                case RemoveWithSyntax removal:
                    Check(removal.Event, removal.Location, levelEvents);
                    break;
                case RemoveViaJoinSyntax removal:
                    Check(removal.Event, removal.Location, levelEvents);
                    break;
                case ClearWithSyntax clear:
                    Check(clear.Event, clear.Location, levelEvents);
                    break;
                case ChildrenSyntax children:
                    Validate(context, projection, children.Blocks, $"children '{children.Property}'");
                    break;
                case NestedSyntax nested:
                    Validate(context, projection, nested.Blocks, $"nested '{nested.Property}'");
                    break;
                case ProjectionVariantSyntax variant:
                    Validate(context, projection, variant.Blocks, $"variant '{variant.Name}'");
                    break;
            }
        }

        void Check(string name, SourceLocation location, HashSet<string> seen)
        {
            if (!seen.Add(name))
            {
                context.Error(
                    DiagnosticCodes.DuplicateEventInProjectionScope,
                    $"Event '{name}' is used more than once in projection '{projection}' at {scope} (Chronicle allows one use of an event type per projection level)",
                    location);
            }
        }
    }
}
