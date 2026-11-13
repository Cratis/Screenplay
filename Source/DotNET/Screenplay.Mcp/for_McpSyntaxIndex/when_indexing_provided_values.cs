// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_indexing_provided_values : Specification
{
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Compile("""
            provide region String from host
            module Organizations
              provide region String from query RegionById by $context.tenant
              feature Membership
                slice StateView View
                  readmodel Region
                    name String
                  query RegionById => Region
                    by id Guid
            """).Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_list_each_provide_as_a_declaration() => _index.Declarations.Count(declaration => declaration.Kind == "Provide").ShouldEqual(2);
    [Fact] void should_keep_the_levels_apart() => _index.Declarations.Where(declaration => declaration.Kind == "Provide").Select(declaration => declaration.Scope.Length).Distinct().Count().ShouldEqual(2);
    [Fact] void should_resolve_the_query_source() => _index.Resolve(_index.References.Single(reference => reference.Role == "provideSource")).Single().Name.ShouldEqual("RegionById");
    [Fact] void should_own_the_source_reference_by_the_provide() => _index.References.Single(reference => reference.Role == "provideSource").Owner!.Kind.ShouldEqual("Provide");
}
