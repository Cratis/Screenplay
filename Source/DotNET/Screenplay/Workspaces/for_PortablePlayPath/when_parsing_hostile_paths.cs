// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_PortablePlayPath;

public class when_parsing_hostile_paths : Specification
{
    static readonly string[] _paths =
    [
        string.Empty,
        "/Projects.play",
        "C:/Projects.play",
        "//server/Projects.play",
        "Projects//Register.play",
        "Projects/./Register.play",
        "Projects/../Register.play",
        "Projects/CON.play",
        "Projects/COM¹.play",
        "Projects/NUL.txt.play",
        "Projects/Register /Slice.play",
        "Projects/Register./Slice.play",
        "Projects/Register:Slice.play",
        "Projects/Register?.play",
        "Projects/Register.PLAY",
        "Projects/Bad\ud800.play",
        $"Projects/{new string('a', 256)}.play",
        $"Projects/{new string('é', 126)}.play",
        string.Join('/', Enumerable.Repeat(new string('é', 100), 21)) + ".play"
    ];
    bool[] _results;

    void Because() => _results = [.. _paths.Select(path => PortablePlayPath.TryParse(path, out _))];

    [Fact] void should_reject_every_hostile_path() => _results.Any(result => result).ShouldBeFalse();
    [Fact] void should_reject_null() => PortablePlayPath.TryParse(null, out _).ShouldBeFalse();
}
