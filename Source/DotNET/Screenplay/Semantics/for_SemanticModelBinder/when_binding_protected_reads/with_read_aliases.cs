// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_read_aliases : given.a_protected_read_model
{
    [Fact]
    void should_bind_an_alias_in_requirements_and_mappings()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View as current by id", StringComparison.Ordinal).Replace("View.value", "current.value", StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Reads.Single().Name.ShouldEqual("current");
    }

    [Fact]
    void should_refuse_an_ambiguous_unaliased_path()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View as first by id\n        reads View as second by id", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousReadPath);
    }

    [Fact]
    void should_refuse_an_unknown_read_path()
    {
        var result = Bind(Source.Replace("View.value", "unknown.value", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownReadPath);
    }

    [Fact]
    void should_refuse_a_second_read_without_an_alias()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View by id\n        reads View as second by id", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.MissingReadsAlias);
    }
}
