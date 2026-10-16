// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_UiProfileShellBinder;

public class when_the_catalog_does_not_know_the_blueprint_or_template : given.a_profile_bound_to_a_catalog
{
    const string Source =
        """
        ui profile NotInstalled
          packages
            Crm.Blueprint

          root template blueprint Crm.Blueprint AppShell

        ui profile NoSuchTemplate
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default Missing
        """;

    void Because() => _diagnostics = Bind(Source);

    [Fact] void should_report_the_blueprint_that_is_not_installed() => _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootBlueprint).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(5);
    [Fact] void should_say_it_is_not_installed() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootBlueprint).Message.ShouldContain("not installed");
    [Fact] void should_report_the_template_the_blueprint_does_not_ship() => _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(11);
    [Fact] void should_name_the_template_and_blueprint_1() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).Message.ShouldContain("'Missing'");
    [Fact] void should_name_the_template_and_blueprint_2() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).Message.ShouldContain("Cratis.Blueprint.Default");
}
