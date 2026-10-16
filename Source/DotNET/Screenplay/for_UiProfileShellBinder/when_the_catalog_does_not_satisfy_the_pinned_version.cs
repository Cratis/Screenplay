// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_UiProfileShellBinder;

public class when_the_catalog_does_not_satisfy_the_pinned_version : given.a_profile_bound_to_a_catalog
{
    const string Source =
        """
        ui profile Desktop
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default AppShell version "^2.0"

        ui profile Satisfied
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default AppShell version "^1.4"

        ui profile Unpinned
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default AppShell
        """;

    void Because() => _diagnostics = Bind(Source);

    [Fact] void should_report_only_the_unsatisfied_pin() => _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateVersionNotSatisfied).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(5);
    [Fact] void should_warn() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateVersionNotSatisfied).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_pin_and_the_installed_version_1() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateVersionNotSatisfied).Message.ShouldContain("^2.0");
    [Fact] void should_name_the_pin_and_the_installed_version_2() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateVersionNotSatisfied).Message.ShouldContain("1.4.2");
    [Fact] void should_report_nothing_else() => _diagnostics.Count.ShouldEqual(1);
}
