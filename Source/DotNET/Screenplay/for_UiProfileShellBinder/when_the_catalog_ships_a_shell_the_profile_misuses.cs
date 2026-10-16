// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_UiProfileShellBinder;

public class when_the_catalog_ships_a_shell_the_profile_misuses : given.a_profile_bound_to_a_catalog
{
    const string Source =
        """
        ui profile Good
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default AppShell
            option navigationMode "slim"

        ui profile WithoutContent
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default Bare

        ui profile UnknownOption
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default AppShell
            option showFooter true
        """;

    void Because() => _diagnostics = Bind(Source);

    [Fact] void should_report_the_shell_without_a_content_outlet() => _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateWithoutContentOutlet).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(12);
    [Fact] void should_report_the_option_the_shell_does_not_expose() => _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(19);
    [Fact] void should_name_the_option_1() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Message.ShouldContain("'showFooter'");
    [Fact] void should_name_the_option_2() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Message.ShouldContain("'navigationMode'");
    [Fact] void should_report_nothing_for_the_good_profile() => _diagnostics.Count.ShouldEqual(2);
}
