// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

public class when_replaying_the_v1_design_time_outputs : Specification
{
    CanonicalDesignTimeOutputVector _vector;
    CompilationResult<ApplicationSyntax> _before;
    CompilationResult<ApplicationSyntax> _after;
    HashSet<string> _addedKinds;
    string _printed;
    string _printedAgain;

    void Because()
    {
        _vector = ScreenCompositionCorpus.V1.DesignTimeOutputs.Single();
        _before = new ScreenplayCompiler().Compile(_vector.Before.Text);
        _after = new ScreenplayCompiler().Compile(_vector.After.Text);
        var printer = new ScreenplayPrinter();
        _printed = printer.Print(_after.Value!);
        _printedAgain = printer.Print(new ScreenplayCompiler().Compile(_printed).Value!);
        var before = Kinds(_before.Value!);
        _addedKinds = [.. Kinds(_after.Value!).Where(entry => entry.Value > before.GetValueOrDefault(entry.Key)).Select(entry => entry.Key)];
    }

    [Fact] void should_compile_the_source_before_the_action() => _before.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_the_output_without_diagnostics() => _after.Diagnostics.ShouldBeEmpty();
    [Fact] void should_name_a_package_qualified_action() => _vector.Action.StartsWith($"{_vector.Package}.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_run_against_a_component_of_that_package() => Components(_after.Value!).Single(component => component.StableId == _vector.Component).Component.StartsWith($"{_vector.Package}.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_add_only_the_declared_output_kinds() => _addedKinds.Except(_vector.OutputSyntaxKinds.Concat(["LiteralExpressionSyntax"])).ShouldBeEmpty();
    [Fact] void should_add_every_declared_output_kind() => _vector.OutputSyntaxKinds.Except(_addedKinds).ShouldBeEmpty();
    [Fact] void should_carry_no_package_code() => _addedKinds.Intersect(["FileReferenceSyntax", "ScreenCodeSyntax", "ImplementationSyntax"]).ShouldBeEmpty();
    [Fact] void should_configure_only_exposed_inherited_settings() => Configured(_after.Value!).ShouldEqual([.. _vector.InheritedSettings]);
    [Fact] void should_expose_every_inherited_setting() => _vector.InheritedSettings.Except(Exposed(_after.Value!)).ShouldBeEmpty();
    [Fact] void should_be_in_canonical_printed_form() => _printed.ShouldEqual(_vector.After.Text);
    [Fact] void should_print_deterministically() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_serialize_deterministically() => SyntaxJson.Serialize(new ScreenplayCompiler().Compile(_printed).Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(_after.Value!).GetRawText());

    static Dictionary<string, int> Kinds(ApplicationSyntax application)
    {
        var counter = new KindCounter();
        counter.VisitApplication(application);
        return counter.Kinds;
    }

    static IEnumerable<ScreenComponentSyntax> Components(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => module.ScreenTemplates).SelectMany(template => template.Content).SelectMany(content => content.Directives).OfType<ScreenComponentSyntax>();

    static string[] Configured(ApplicationSyntax application) =>
        [.. application.InstanceContributions.SelectMany(instance => instance.Contributions).Select(contribution => $"{contribution.Component}.{contribution.Path}").Distinct()];

    static IEnumerable<string> Exposed(ApplicationSyntax application) =>
        application.Exposures.SelectMany(exposure => exposure.Properties).Select(property => $"{property.Component}.{property.Path}");

    sealed class KindCounter : ScreenplaySyntaxWalker
    {
        public Dictionary<string, int> Kinds { get; } = new(StringComparer.Ordinal);

        public override void VisitNode(SyntaxNode node) => Kinds[node.GetType().Name] = Kinds.GetValueOrDefault(node.GetType().Name) + 1;
    }
}
