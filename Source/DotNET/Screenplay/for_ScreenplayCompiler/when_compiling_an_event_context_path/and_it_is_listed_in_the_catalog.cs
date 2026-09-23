// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_it_is_listed_in_the_catalog : given.a_projection_reading_the_event_context
{
    List<string> _diagnosed;

    void Because() => _diagnosed =
    [
        .. EventContextCatalog.Paths
            .Select(path => path.Path)
            .Where(path => _compiler.CompileProjection(Projection($"$eventContext.{path}")).Diagnostics.Any())
    ];

    [Fact] void should_accept_every_path_without_a_diagnostic() => _diagnosed.ShouldBeEmpty();
}
