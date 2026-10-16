// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_names_a_root_template_that_cannot_be_the_shell : given.a_compiler
{
    const string Source =
        """
        layout Tight
          category Shell
          scopes feature
          content

        layout Typed
          category Shell
          type Form
          content

        module M
          screen template Page
            category Page
            content

          screen template Docked
            category Shell
            scopes application
            fits slot content
            content

          dialog template Confirm
            category Dialog
            content

        ui profile Scoped
          root template Tight

        ui profile Typed
          root template Typed

        ui profile Plain
          root template Page

        ui profile Docked
          root template Docked

        ui profile Dialog
          root template Confirm
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_each_template_that_cannot_be_the_shell() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(26, 29, 32, 35, 38);
    [Fact] void should_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_say_why_a_scope_excludes_it() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).First().Message.ShouldContain("does not declare scope 'application'");
    [Fact] void should_say_why_a_type_excludes_it() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).ElementAt(1).Message.ShouldContain("type 'Form' is for content");
    [Fact] void should_say_a_screen_template_needs_the_root_role() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).ElementAt(2).Message.ShouldContain("only a layout or an application-scope screen template can be the shell");
    [Fact] void should_say_a_root_cannot_fit_a_slot() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateNotApplicable).ElementAt(3).Message.ShouldContain("fits a slot");
}
