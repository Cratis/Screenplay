// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_Samples;

public class when_executing_library_borrowing : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    ApplicationSyntax _application;
    ApplicationSyntax _restored;
    SpecificationSyntax[] _specifications;
    string[] _clockDiagnostics;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Library/library.play"));
        var compiler = new ScreenplayCompiler();
        var compiled = compiler.Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _application = compiled.Value!;
        var bound = Bind(source);

        // The whole sample has unrelated executable-readiness refusals; its clock forms must bind.
        _clockDiagnostics = [.. bound.Diagnostics.Where(diagnostic => diagnostic.Message.Contains("clock", StringComparison.OrdinalIgnoreCase) || diagnostic.Message.Contains("calendar", StringComparison.OrdinalIgnoreCase) || diagnostic.Message.Contains("time zone", StringComparison.OrdinalIgnoreCase) || diagnostic.Message.Contains("'dueDate'", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Message)];
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_application));
        var roundTrip = compiler.Compile(new ScreenplayPrinter().Print(restored));
        Assert.True(roundTrip.Success, string.Join('\n', roundTrip.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _restored = roundTrip.Value!;
        _specifications = [.. _application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "BorrowBook").Specifications];
    }

    [Fact] void should_compile_all_three_specifications() => _specifications.Length.ShouldEqual(3);
    [Fact] void should_bind_without_clock_diagnostics() => _clockDiagnostics.ShouldBeEmpty();
    [Fact] void should_pin_every_clock() => _specifications.All(specification => specification.GivenClock is not null).ShouldBeTrue();
    [Fact] void should_pin_every_time_zone() => _specifications.All(specification => specification.GivenTimeZone is not null).ShouldBeTrue();
    [Fact] void should_round_trip_the_sample() => SyntaxJson.Serialize(_restored).GetRawText().ShouldEqual(SyntaxJson.Serialize(_application).GetRawText());
}
