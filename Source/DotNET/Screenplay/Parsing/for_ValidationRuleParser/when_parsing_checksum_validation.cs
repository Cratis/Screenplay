// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ValidationRuleParser;

public class when_parsing_checksum_validation : Specification
{
    JsonElement[] _syntax;
    string[] _invalid;
    CompilationResult<ApplicationSyntax>[] _parsed;
    CompilationResult<ApplicationSyntax>[] _refused;
    ValidationRuleSyntax[] _rules;
    ValidationRuleSyntax[] _roundTripped;

    void Establish()
    {
        var vectors = Vectors();
        _syntax = [.. vectors.GetProperty("syntax").EnumerateArray()];
        _invalid = [.. vectors.GetProperty("invalid").EnumerateArray().Select(value => value.GetString()!)];
    }

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _parsed = [.. _syntax.Select(vector => compiler.Parse(Source(vector.GetProperty("directive").GetString()!)))];
        _refused = [.. _invalid.Select(directive => compiler.Parse(Source(directive)))];
        _rules = [.. _parsed.Select(result => Rule(result.Value!))];
        _roundTripped = [.. _parsed.Select(result => Rule(compiler.Parse(new ScreenplayPrinter().Print(result.Value!)).Value!))];
    }

    [Fact] void should_check_a_nonempty_syntax_population() => _syntax.ShouldNotBeEmpty();
    [Fact] void should_check_a_nonempty_refusal_population() => _invalid.ShouldNotBeEmpty();
    [Fact] void should_parse_both_forms_without_diagnostics() => _parsed.SelectMany(result => result.Diagnostics).ShouldBeEmpty();
    [Fact] void should_use_the_appended_checksum_kind() => _rules.Select(rule => rule.Rule).ShouldEachConformTo(kind => kind == ValidationRuleKind.Checksum);
    [Fact] void should_use_the_implied_concept_subject() => _rules.Select(rule => rule.Property).ShouldEachConformTo(property => property == ValidationRuleSyntax.ConceptValue);
    [Fact] void should_use_a_text_algorithm_operand() => _rules.Select(rule => ((LiteralExpressionSyntax)rule.Value!).Value).ShouldEqual([.. _syntax.Select(vector => (object)vector.GetProperty("algorithm").GetString()!)]);
    [Fact] void should_preserve_messages() => _rules.Select(rule => rule.Message).ShouldEqual([.. _syntax.Select(vector => vector.GetProperty("message").GetString())]);
    [Fact] void should_preserve_severities() => _rules.Select(rule => rule.Severity.ToString()).ShouldEqual([.. _syntax.Select(vector => vector.GetProperty("severity").GetString())]);
    [Fact] void should_report_invalid_algorithms_at_the_rule() => _refused.ShouldEachConformTo(result => !result.Success && result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidGenerationArgument));
    [Fact] void should_print_unquoted_algorithms_and_round_trip() => _roundTripped.Select(rule => ((LiteralExpressionSyntax)rule.Value!).Value).ShouldEqual([.. _rules.Select(rule => ((LiteralExpressionSyntax)rule.Value!).Value)]);

    static string Source(string directive) => $"concept InvoiceRef : String\n  {directive}\n";

    static ValidationRuleSyntax Rule(ApplicationSyntax syntax) => ((DeclarativeValidateSyntax)syntax.Concepts.Single().Validations!.Single()).Rules.Single();

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "generation-checksum-rules.json")));

        return document.RootElement.Clone();
    }
}
