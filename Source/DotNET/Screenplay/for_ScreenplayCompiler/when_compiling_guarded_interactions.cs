// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_guarded_interactions : given.a_compiler
{
    const string Source = """
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                status String
              query ItemDetails => Item[]
              command Retry
              screen Details
                data Item[] via query ItemDetails
                table Item
                  column status
                  on double click
                    when item.status == "failed"
                      confirm "Retry?"
                        on success
                          execute Retry
                    when item.status == "open"
                      navigate to Details
                    otherwise
                      notify info "Closed"
        """;

    CompilationResult<ApplicationSyntax> _result;
    InteractionBindingSyntax _binding;
    string _printed;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _binding = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenTableSyntax>().Single().Behaviors.Single().Bindings.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value!);
    }

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_two_ordered_alternatives() => _binding.Alternatives.Count().ShouldEqual(2);
    [Fact] void should_not_flatten_choices_into_plain_actions() => _binding.Actions.ShouldBeEmpty();
    [Fact] void should_keep_nested_continuations() => _binding.Alternatives.First().Actions.Single().OnSuccess.Single().ShouldBeOfExactType<ExecuteCommandActionSyntax>();
    [Fact] void should_keep_fallback() => _binding.Otherwise!.Actions.Single().ShouldBeOfExactType<NotifyActionSyntax>();
    [Fact] void should_round_trip_printed_alternatives() => _compiler.Compile(_printed).Diagnostics.ShouldBeEmpty();
    [Fact] void should_round_trip_syntax_transport() => SyntaxJson.StructurallyEqual(_result.Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(_result.Value!))).ShouldBeTrue();

    [Theory]
    [InlineData("submit")]
    [InlineData("change")]
    [InlineData("event Changed")]
    [InlineData("Signal")]
    void should_reject_non_item_triggers(string trigger) => _compiler.Compile(Source.Replace("on double click", $"on {trigger}", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedInteractionAlternatives).ShouldBeTrue();

    [Theory]
    [InlineData("item.status == \"failed\"", DiagnosticSeverity.Warning)]
    [InlineData("$context.value == true", DiagnosticSeverity.Information)]
    void should_deprecate_only_item_where(string condition, DiagnosticSeverity severity)
    {
        var result = _compiler.Compile($"""
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    on click
                      where {condition}
                      notify info "Done"
            """);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInteractionWhere).Severity.ShouldEqual(severity);
    }

    [Fact] void should_validate_the_activated_row_field() => _compiler.Compile(Source.Replace("item.status", "item.missing", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownActionSubjectField).ShouldBeTrue();
    [Fact] void should_detect_shadowed_whole_lists() => _compiler.Compile(Source.Replace("== \"open\"", "== \"failed\"", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableActionAlternative).ShouldBeTrue();

    [Fact]
    void should_leave_event_where_unchanged() => _compiler.Compile("""
        module Work
          feature Items
            slice StateView Details
              event Changed
                amount Decimal
              screen Details
                on event Changed
                  where amount > 0
                  notify info "Done"
        """).Diagnostics.ShouldBeEmpty();
}
