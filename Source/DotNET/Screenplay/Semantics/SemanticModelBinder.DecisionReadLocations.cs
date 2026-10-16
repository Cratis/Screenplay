// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        HashSet<SourceLocation> DecisionReadLocations()
        {
            var locations = new HashSet<SourceLocation>();
            foreach (var command in AllSlices().SelectMany(entry => entry.Slice.Commands))
            {
                locations.UnionWith((command.Reads ?? []).Select(read => read.Location));
                if (command.Reads?.Any() == true && command.Concurrency is not null) locations.Add(command.Concurrency.Location);
                new DecisionReadPathLocations(locations).VisitCommand(command);
            }
            return locations;
        }
    }

    sealed class DecisionReadPathLocations(HashSet<SourceLocation> locations) : ScreenplaySyntaxWalker
    {
        public override void VisitNode(SyntaxNode node)
        {
            if ((node is PathExpressionSyntax path && path.Path.Contains('.')) ||
                (node is ComparisonConditionSyntax comparison && comparison.Left.Contains('.')) ||
                (node is PropertyResponseSourceSyntax response && response.Property.Contains('.')))
            {
                locations.Add(node.Location);
            }
        }
    }
}
