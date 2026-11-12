// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionVersionRange;

public class when_matching_versions
{
    [Theory]
    [InlineData("latest", "9.9.9", true)]
    [InlineData("2", "2.9.1", true)]
    [InlineData("2", "3.0.0", false)]
    [InlineData("2.3", "2.3.7", true)]
    [InlineData("2.3", "2.4.0", false)]
    [InlineData("2.3.1", "2.3.1", true)]
    [InlineData("2.3.1", "2.3.2", false)]
    [InlineData("^2.3", "2.9.0", true)]
    [InlineData("^2.3", "2.2.9", false)]
    [InlineData("^2.3", "3.0.0", false)]
    [InlineData("~2.3", "2.3.9", true)]
    [InlineData("~2.3", "2.4.0", false)]
    [InlineData("^2", "nonsense", false)]
    void should_decide(string range, string version, bool expected) => ExtensionVersionRange.Satisfies(range, version).ShouldEqual(expected);
}
