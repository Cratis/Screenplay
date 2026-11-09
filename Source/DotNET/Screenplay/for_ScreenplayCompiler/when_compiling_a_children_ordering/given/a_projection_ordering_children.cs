// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering.given;

public class a_projection_ordering_children : for_ScreenplayCompiler.given.a_compiler
{
    protected CompilationResult<ProjectionSyntax> _result;

    protected ChildrenSyntax Children => _result.Value!.Blocks.OfType<ChildrenSyntax>().Single();

    protected static string Projection(params string[] orderings) =>
        $"""
        projection Models => ModelReadModel
          children entries identified by entryId
            no automap
        {string.Join(Environment.NewLine, orderings.Select(ordering => $"    {ordering}"))}
            from EntryAdded
              name = name
        """;
}
