// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_template_version_is_a_range_scene_understands : given.a_compiler
{
    [Theory]
    [InlineData("*")]
    [InlineData("1.2.3")]
    [InlineData("=1.2")]
    [InlineData("^1")]
    [InlineData("~1.2.0")]
    [InlineData(">=2.0.0")]
    [InlineData(">1.0.0")]
    [InlineData("<=3")]
    [InlineData("<4.0.0-beta.1")]
    void should_not_report_it(string range) =>
        _compiler.Compile($"module Sales\n  template blueprint Crm Workspace version \"{range}\"\n").Diagnostics.ShouldBeEmpty();

    [Theory]
    [InlineData("latest")]
    [InlineData("1.2.3.4")]
    [InlineData("^")]
    [InlineData("1.x")]
    [InlineData(">=a")]
    void should_report_a_range_it_cannot_read(string range) =>
        _compiler.Compile($"module Sales\n  template blueprint Crm Workspace version \"{range}\"\n").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidTemplateVersionRange);
}
