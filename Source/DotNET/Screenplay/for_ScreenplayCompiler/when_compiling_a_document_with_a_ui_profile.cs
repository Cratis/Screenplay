// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_ui_profile : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          target platform web
          target size expanded

          packages
            core
            PrimeReact
            Internal.Widgets

        ui profile Mobile
          target platform ios, android
          target size compact
        """;

    CompilationResult<ApplicationSyntax> _result;
    UiProfileSyntax _desktop;
    UiProfileSyntax _mobile;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _desktop = _result.Value!.UiProfiles!.First(profile => profile.Name == "Desktop");
        _mobile = _result.Value!.UiProfiles!.First(profile => profile.Name == "Mobile");
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_both_profiles() => _result.Value!.UiProfiles!.Count().ShouldEqual(2);
    [Fact] void should_parse_the_desktop_platform() => _desktop.Platforms.ShouldContainOnly("web");
    [Fact] void should_parse_the_desktop_size_class() => _desktop.DefaultSizeClass.ShouldEqual("expanded");
    [Fact] void should_parse_the_desktop_packages_in_declaration_order() => _desktop.Packages.ShouldContainOnly("core", "PrimeReact", "Internal.Widgets");
    [Fact] void should_parse_multiple_comma_separated_platforms() => _mobile.Platforms.ShouldContainOnly("ios", "android");
    [Fact] void should_parse_the_mobile_size_class() => _mobile.DefaultSizeClass.ShouldEqual("compact");
    [Fact] void should_leave_mobile_packages_empty_when_not_declared() => _mobile.Packages.ShouldBeEmpty();
}
