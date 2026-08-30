// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_PortablePlayPath;

public class when_comparing_portable_aliases : Specification
{
    HashSet<PortablePlayPath> _paths;

    void Because() => _paths = new(PortablePlayPath.CollisionComparer)
    {
        PortablePlayPath.Parse("Projects/Café.play"),
        PortablePlayPath.Parse("projects/Cafe\u0301.play")
    };

    [Fact] void should_treat_case_and_normalization_aliases_as_one_collision_key() => _paths.Count.ShouldEqual(1);
    [Fact] void should_preserve_authored_casing_on_the_value_itself() => PortablePlayPath.Parse("Projects/File.play").ShouldNotEqual(PortablePlayPath.Parse("projects/file.play"));
}
