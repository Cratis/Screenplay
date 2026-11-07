// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayVersion;

public class when_reading_versions : Specification
{
    [Theory]
    [InlineData("5", 5)]
    [InlineData("5.0", 5)]
    [InlineData("v5.11.0", 5)]
    [InlineData(" 6 ", 6)]
    void should_read_the_major(string text, int major) => (ScreenplayVersion.TryParse(text, out var version) ? version.Major : -1).ShouldEqual(major);

    [Theory]
    [InlineData("")]
    [InlineData("five")]
    [InlineData("0")]
    [InlineData("-5")]
    void should_refuse_what_is_not_a_version(string text) => ScreenplayVersion.TryParse(text, out _).ShouldBeFalse();
}
