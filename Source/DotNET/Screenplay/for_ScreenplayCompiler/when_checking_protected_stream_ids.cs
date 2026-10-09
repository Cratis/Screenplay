// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_protected_stream_ids : given.a_compiler
{
    const string Scalar = "eventsource A\n  stream S\n    streamId Personal";
    const string Composite = "eventsource A\n  stream S\n    streamId\n      owner Personal\n      period String";
    const string RoutePrefix = "concept Public : String\ntype Input\n  personal Personal\neventsource A\n  stream S\n    streamId Public\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input Input\n        stream A.S\n          streamId = input.personal";
    const string CompositeRoutePrefix = "concept Public : String\ntype Input\n  personal Personal\neventsource A\n  stream S\n    streamId\n      owner Public\n      period String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input Input\n        stream A.S\n          streamId\n            owner = input.personal\n            period = \"public\"";

    [Theory]
    [InlineData("pii", Scalar, 4, 14, "a stream id")]
    [InlineData("secret", Scalar, 4, 14, "a stream id")]
    [InlineData("pii", Composite, 5, 13, "a stream id part 'owner'")]
    [InlineData("secret", Composite, 5, 13, "a stream id part 'owner'")]
    [InlineData("pii", RoutePrefix, 14, 22, "a stream id route mapping")]
    [InlineData("secret", RoutePrefix, 14, 22, "a stream id route mapping")]
    [InlineData("pii", CompositeRoutePrefix, 17, 21, "a stream id route mapping")]
    [InlineData("secret", CompositeRoutePrefix, 17, 21, "a stream id route mapping")]
    void should_reject_protected_stream_identity_positions(string attribute, string source, int line, int column, string position)
    {
        var result = _compiler.Compile($"concept Personal : String {attribute}\n{source}");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Location.Column.ShouldEqual(column);
        diagnostic.Message.ShouldEqual($"Concept 'Personal' is {attribute} and cannot be {position} - use a surrogate Uuid identifier and keep the {attribute} value as a property");
        diagnostic.Message.ShouldNotContain("input.personal");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_repeat_the_declaration_error_for_its_own_concept(bool composite)
    {
        var source = composite ? CompositeRoutePrefix : RoutePrefix;
        source = source.Replace("owner Public", "owner Personal", StringComparison.Ordinal).Replace("streamId Public", "streamId Personal", StringComparison.Ordinal);
        var result = _compiler.Compile("concept Personal : String pii\n" + source);
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Location.Line.ShouldEqual(composite ? 8 : 7);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_still_report_a_different_protected_source_concept(bool composite)
    {
        var source = (composite ? CompositeRoutePrefix : RoutePrefix).Replace("owner Public", "owner Other", StringComparison.Ordinal).Replace("streamId Public", "streamId Other", StringComparison.Ordinal);
        var result = _compiler.Compile("concept Personal : String pii\nconcept Other : String secret\n" + source);
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier).ToArray();
        diagnostics.Length.ShouldEqual(2);
        diagnostics[0].Message.ShouldContain("secret");
        diagnostics[1].Message.ShouldContain("pii");
        diagnostics[1].Message.ShouldContain("route mapping");
    }

    [Fact]
    void should_report_both_attributes_once_using_the_existing_pii_precedence()
    {
        var result = _compiler.Compile("concept Personal : String secret pii\n" + Scalar);
        result.Diagnostics.Single().Message.ShouldContain("pii");
    }

    [Fact]
    void should_leave_surrogates_ordinary_protected_properties_and_literal_routes_valid()
    {
        var source = RoutePrefix.Replace("input.personal", "\"public\"", StringComparison.Ordinal);
        _compiler.Compile("concept Personal : String pii\n" + source).Diagnostics.ShouldBeEmpty();
    }
}
