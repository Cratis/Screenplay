// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_host_registrations_override_built_in_triggers : Specification
{
    const string Reaction = """
        module Billing
          feature Host
            slice Automation Host
              reaction Host
                where enabled == true
                when SIGNAL
                  SELECTED
                  produces Started
                    for "root"
              event Started
        """;
    const string Scenario = """
        module Billing
          feature Host
            slice Automation Scenarios
              specification Host
                when trigger SIGNAL
                then Started
                  for "root"
        """;

    [Fact]
    void should_fail_closed_for_shaped_or_unknown_host_signals_with_selected_values_or_only_a_guard()
    {
        foreach (var signal in new[] { "Startup", "Shutdown" })
        {
            foreach (var shape in new IReadOnlyList<string>?[] { ["enabled"], null })
            {
                foreach (var selected in new[] { "enabled", "" })
                {
                    foreach (var folder in new[] { false, true })
                    {
                        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: [new(signal, shape)]));
                        var texts = Sources(signal, selected, folder);
                        var documents = Documents(texts);
                        var result = new SemanticModelCompiler(compiler, new SemanticModelBinder()).Compile("Billing", documents);
                        result.Success.ShouldBeFalse();
                        Assert.True(
                            result.Diagnostics.Any(diagnostic => diagnostic.Code == Diagnostics.DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains(signal, StringComparison.Ordinal)),
                            $"{signal}, known={shape is not null}, selected={selected}, folder={folder}: {string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message))}");
                    }
                }
            }
        }
    }

    [Fact]
    void should_reject_an_overridden_signal_even_when_only_the_specification_uses_it()
    {
        foreach (var signal in new[] { "Startup", "Shutdown" })
        {
            var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: [new(signal)]));
            var documents = Documents([Scenario.Replace("SIGNAL", signal, StringComparison.Ordinal).Replace("then Started\n          for \"root\"", "then error", StringComparison.Ordinal)]);
            new SemanticModelCompiler(compiler, new SemanticModelBinder()).Compile("Billing", documents).Success.ShouldBeFalse();
        }
    }

    [Fact]
    void should_snapshot_the_actual_registration_for_programmatic_binding_without_a_provider_or_typed_descriptor()
    {
        var values = new List<string> { "enabled" };
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: [new("Startup", values)]));
        var source = string.Join('\n', Sources("Startup", "", false));
        var parsed = compiler.Compile(source);
        parsed.Success.ShouldBeTrue();
        values.Clear();
        var bound = new SemanticModelBinder().Bind("Billing", parsed.Value! with { }, Documents([source]));
        bound.Success.ShouldBeFalse();
        bound.TypedContextDescriptors.ShouldBeEmpty();
    }

    [Fact]
    void should_keep_default_empty_host_signals_supported_without_a_caller()
    {
        foreach (var signal in new[] { "Startup", "Shutdown" })
        {
            var source = string.Join('\n', Sources(signal, "", false)).Replace("        where enabled == true\n", "", StringComparison.Ordinal);
            var model = given.v6_regression_models.Compile(source);
            foreach (var run in given.v6_regression_models.Runs(model))
            {
                run.Passed.ShouldBeTrue();
                ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(1);
            }
        }
    }

    static string[] Sources(string signal, string selected, bool folder)
    {
        var reaction = Reaction.Replace("SIGNAL", signal, StringComparison.Ordinal).Replace("SELECTED", selected, StringComparison.Ordinal);
        var scenario = Scenario.Replace("SIGNAL", signal, StringComparison.Ordinal);
        return folder ? [reaction, scenario] : [reaction + "\n" + scenario[scenario.IndexOf("      specification", StringComparison.Ordinal)..]];
    }

    static SemanticDocumentSet Documents(string[] texts)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        return SemanticDocumentSet.Create([.. texts.Select((text, index) => SemanticSourceDocument.Create(catalog.ResolveDocument($"host-{index}"), $"host-{index}", $"Host{index}.play", text))], catalog);
    }
}
