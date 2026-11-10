// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_GlobMetacharacters;

public class when_inspecting_paths : Specification
{
    [Theory]
    [InlineData("a/*.play")]
    [InlineData("a/?.play")]
    [InlineData("a/[bc].play")]
    [InlineData("a/b].play")]
    [InlineData("a/{b,c}.play")]
    [InlineData("a/b}.play")]
    [InlineData("*")]
    public void should_recognize_every_glob_metacharacter(string path) => GlobMetacharacters.AreIn(path).ShouldBeTrue();

    [Theory]
    [InlineData("a/b.play")]
    [InlineData("")]
    [InlineData("a b/c-d_e.play")]
    public void should_leave_literal_paths_alone(string path) => GlobMetacharacters.AreIn(path).ShouldBeFalse();
}
