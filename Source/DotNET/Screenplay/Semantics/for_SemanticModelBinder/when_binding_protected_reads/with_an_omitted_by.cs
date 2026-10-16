// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_an_omitted_by : given.a_protected_read_model
{
    [Fact]
    void should_use_the_declared_identifier()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View", StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Reads.Single().Key.ShouldEqual(Command(result.Value.Model).Properties.Single(property => property.IsIdentifier).Id);
    }

    [Fact]
    void should_refuse_an_omitted_by_without_an_identifier()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View", StringComparison.Ordinal).Replace("id String identifier", "id String", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.DecisionReadWithoutIdentifier);
    }

    [Fact]
    void should_allow_an_explicit_key_without_a_command_identifier()
    {
        var source = Source.Replace("id String identifier", "id String", StringComparison.Ordinal).Replace("                for id\n", string.Empty, StringComparison.Ordinal);
        source = source.Replace("          for id\n", string.Empty, StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Reads.Single().Key.ShouldEqual(Command(result.Value.Model).Properties.Single(property => property.Name == "id").Id);
    }
}
