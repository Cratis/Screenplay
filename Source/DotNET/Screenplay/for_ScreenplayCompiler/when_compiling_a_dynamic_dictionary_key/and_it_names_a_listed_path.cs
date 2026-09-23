// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_dynamic_dictionary_key;

public class and_it_names_a_listed_path : given.a_projection_counting_by_key
{
    List<string> _diagnosed;

    void Because() => _diagnosed =
    [
        .. EventContextCatalog.Paths
            .Select(path => path.Path)
            .Where(path => _compiler.CompileProjection(Projection($"countBy.$eventContext.{path}")).Diagnostics.Any())
    ];

    [Fact] void should_accept_every_path_without_a_diagnostic() => _diagnosed.ShouldBeEmpty();
}
