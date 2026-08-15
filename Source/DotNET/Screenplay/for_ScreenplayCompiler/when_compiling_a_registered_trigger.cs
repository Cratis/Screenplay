// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// The open half of the trigger model. A runtime integration's trigger has no declaration in the document
/// that reacts to it, so without a registry the compiler would have to either report every one of them or
/// know about all of them - and the second is what the extensibility exists to avoid.
/// </summary>
public class when_compiling_a_registered_trigger : Specification
{
    const string Source =
        """
        module Deployment
          feature Releases
            slice Automation OnPush
              reaction Deploy
                when GitPushed
                  repository
                  ref

            slice Automation OnBoot
              reaction Warmup
                when Startup
        """;

    ScreenplayCompiler _named;
    ScreenplayCompiler _shaped;
    ScreenplayCompiler _bare;
    CompilationResult<ApplicationSyntax> _registeredByName;
    CompilationResult<ApplicationSyntax> _registeredWithValues;
    CompilationResult<ApplicationSyntax> _unregistered;

    void Establish()
    {
        // A name and nothing more: the registration states no shape, so what the reaction takes is its own business.
        _named = new(new ScreenplayLanguageRegistry(triggers: ["GitPushed"]));

        // The same trigger, this time saying what an occurrence carries.
        _shaped = new(new ScreenplayLanguageRegistry(triggers: [new TriggerDefinition("GitPushed", ["repository", "sha"])]));

        _bare = new();
    }

    void Because()
    {
        _registeredByName = _named.Compile(Source);
        _registeredWithValues = _shaped.Compile(Source);
        _unregistered = _bare.Compile(Source);
    }

    // The reaction takes 'repository' and 'ref'. Nothing is reported, which is what says the values went
    // unchecked rather than that they happened to be right - the registration named no shape to check against.
    [Fact] void should_accept_a_trigger_registered_by_name_and_leave_its_values_alone() => _registeredByName.Diagnostics.ShouldBeEmpty();

    // The registration declares 'repository' and 'sha'; the reaction takes 'repository' and 'ref'. Exactly one
    // diagnostic is what says the declared one was accepted and the undeclared one was not.
    [Fact] void should_report_only_the_value_the_registration_does_not_declare() => _registeredWithValues.Diagnostics.Count().ShouldEqual(1);
    [Fact] void should_name_the_value_the_registration_does_not_declare() =>
        _registeredWithValues.Diagnostics.Single().Message.ShouldEqual("'GitPushed' carries no 'ref' - a reaction can only take values the occurrence provides");
    [Fact] void should_report_that_value_under_the_unknown_value_code() =>
        _registeredWithValues.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownTriggerData);

    [Fact] void should_report_a_trigger_nothing_registered() => _unregistered.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownTrigger);
    [Fact] void should_report_it_as_a_warning() => _unregistered.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_accept_a_built_in_trigger_without_registration() =>
        _unregistered.Diagnostics.Any(_ => _.Message.Contains("Startup", StringComparison.Ordinal)).ShouldBeFalse();
}
