// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_the_library_clock_rules : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    ApplicationSyntax _application;
    ApplicationSyntax _restored;
    Diagnostic[] _legacy;
    Diagnostic[] _protected;
    SemanticCommand _command;
    SemanticSpecification[] _specifications;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Library/library.play"));
        var compiler = new ScreenplayCompiler();
        var compiled = compiler.Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _application = compiled.Value!;
        _legacy = [.. Bind(source).Diagnostics];
        _protected = [.. Bind("command reads protected\n" + source).Diagnostics];
        var clocks = Bind(ClockRules(source));
        Assert.True(clocks.Success, string.Join('\n', clocks.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var slice = clocks.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "BorrowBook");
        _command = slice.Commands.Single();
        _specifications = [.. slice.Specifications];
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_application));
        var roundTrip = compiler.Compile(new ScreenplayPrinter().Print(restored));
        Assert.True(roundTrip.Success, string.Join('\n', roundTrip.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _restored = roundTrip.Value!;
    }

    [Fact] void should_bind_today_as_the_clock_rule_operand() => _command.Validations.Single().ClockOperand.ShouldEqual(new SemanticClockOperand(SemanticClockKind.Today, 0, SemanticClockUnit.Day));
    [Fact] void should_bind_all_three_specifications() => _specifications.Length.ShouldEqual(3);
    [Fact] void should_pin_both_daytime_clocks() => _specifications.Where(specification => specification.Name != "RefusingALoanDueAtMidnightInOslo").All(specification => specification.GivenClock == "2026-10-02T09:00:00.0000000Z").ShouldBeTrue();
    [Fact] void should_pin_the_midnight_clock() => _specifications.Single(specification => specification.Name == "RefusingALoanDueAtMidnightInOslo").GivenClock.ShouldEqual("2026-10-29T23:30:00.0000000Z");
    [Fact] void should_pin_every_time_zone() => _specifications.All(specification => specification.GivenTimeZone == "Europe/Oslo").ShouldBeTrue();
    [Fact] void should_report_legacy_borrowing_reads() => _legacy.ShouldContain(diagnostic => diagnostic.Code == "PLAY0271" && diagnostic.Message.Contains("Command 'BorrowBook' reads 'CatalogEntry'", StringComparison.Ordinal));
    [Fact] void should_refuse_the_legacy_available_path() => _legacy.ShouldContain(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("book.available", StringComparison.Ordinal));
    [Fact] void should_not_report_protected_admission_without_the_preamble() => _legacy.Any(diagnostic => diagnostic.Code == "PLAY0781").ShouldBeFalse();
    [Fact] void should_report_no_removal_after_opting_into_protected_reads() => _protected.ShouldContain(diagnostic => diagnostic.Code == "PLAY0781" && diagnostic.Message.Contains("NoRemoval", StringComparison.Ordinal) && diagnostic.Message.Contains("CatalogEntry", StringComparison.Ordinal));
    [Fact] void should_round_trip_the_sample() => SyntaxJson.Serialize(_restored).GetRawText().ShouldEqual(SyntaxJson.Serialize(_application).GetRawText());

    // Bind the sample's clock rule and fixtures independently of its legacy reads; never execute this excerpt.
    static string ClockRules(string source)
    {
        var lines = source.Split('\n');
        var borrowing = lines.SkipWhile(line => line.Trim() != "slice StateChange BorrowBook").TakeWhile(line => line.Trim() != "slice StateChange ReturnBook").ToArray();
        var command = borrowing.TakeWhile(line => line.Trim() != "screen BorrowBook");
        var specifications = borrowing.SkipWhile(line => line.Trim() != "specification BorrowingAnAvailableBook");
        return string.Join('\n', lines.Where(line => line.StartsWith("time zone ", StringComparison.Ordinal))
            .Concat(lines.Where(line => line.StartsWith("concept ", StringComparison.Ordinal)).Take(5))
            .Concat(lines.SkipWhile(line => line != "policy IsMember").Take(2))
            .Concat(["module Lending", "  feature Loans", "    slice StateView Catalog"])
            .Concat(lines.SkipWhile(line => line.Trim() != "readmodel CatalogEntry").Take(5))
            .Concat(lines.SkipWhile(line => line.Trim() != "query BookById => CatalogEntry optional").Take(2))
            .Concat(command).Concat(specifications))
            .Replace("        reads CatalogEntry as book by bookId\n", string.Empty, StringComparison.Ordinal)
            .Replace("          require book.available == true\n            message \"That book is already on loan\"\n", string.Empty, StringComparison.Ordinal);
    }
}
