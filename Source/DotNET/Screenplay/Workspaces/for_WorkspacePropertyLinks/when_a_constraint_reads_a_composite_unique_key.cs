// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_constraint_reads_a_composite_unique_key : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _keys;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              event Registered
                name String
                region String
              constraint UniqueName
                unique name, region on Registered
        """);
    void Because() => _keys = [.. _links.Links.Where(link => link.Reference.Role == "key")];

    [Fact] void should_index_both_key_parts() => _keys.SelectMany(link => link.Candidates).Select(property => property.Name).ShouldContainOnly("name", "region");
    [Fact] void should_resolve_the_constraint_readers() => _keys.Select(link => link.Resolution).Distinct().ShouldContainOnly("resolved");
}
