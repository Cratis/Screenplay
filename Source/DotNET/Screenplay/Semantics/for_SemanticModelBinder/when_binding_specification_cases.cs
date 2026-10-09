// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_specification_cases : given.a_semantic_binder
{
    const string Declarations = """
        module M
          feature F
            slice StateChange S
              event Recorded
                amount Int
              command Record
                id String identifier
                amount Int
                produces Recorded
                  for id
                  amount = amount
        """;
    const string Table = """
              specification Recording
                parameter amount Int
                case Small amount = 10
                case Large amount = 100
                when Record amount = case.amount
                  id = "record"
                then Recorded amount = case.amount
        """;
    const string Expanded = """
              specification Recording_Small
                when Record amount = 10
                  id = "record"
                then Recorded amount = 10
              specification Recording_Large
                when Record amount = 100
                  id = "record"
                then Recorded amount = 100
        """;
    CompilationResult<SemanticCompilation> _table;
    CompilationResult<SemanticCompilation> _expanded;

    void Because()
    {
        _table = Bind(Declarations + "\n" + Table);
        _expanded = Bind(Declarations + "\n" + Expanded);
    }

    [Fact] void should_bind_the_table() => _table.Success.ShouldBeTrue();
    [Fact] void should_match_hand_written_bytes() => SemanticModelSerializer.Serialize(_table.Value.Model).SequenceEqual(SemanticModelSerializer.Serialize(_expanded.Value.Model)).ShouldBeTrue();
    [Fact] void should_match_hand_written_revision() => _table.Value.Model.Revision.ShouldEqual(_expanded.Value.Model.Revision);
    [Fact]
    void should_execute_every_case()
    {
        var runs = _table.Value.SpecificationOrigins.Keys.Select(id => new SemanticSpecificationRunner().Run(_table.Value, id)).ToArray();
        Assert.True(runs.All(run => run.Passed), string.Join("; ", runs.Select(run => $"{string.Join("; ", run.Failures)} {(run.Execution as SemanticUnsupported)?.Details}")));
    }
    [Fact] void should_keep_every_case_origin() => _table.Value.SpecificationOrigins.Values.Select(origin => origin.Case.Name).ShouldContainOnly("Small", "Large");

    [Fact]
    void should_prefix_failure_without_examples()
    {
        var bound = Bind(Declarations + "\n" + Table.Replace("then Recorded amount = case.amount", "then Recorded amount = 999", StringComparison.Ordinal)).Value;
        var origin = bound.SpecificationOrigins.Single(pair => pair.Value.Case.Name == "Small");
        new SemanticSpecificationRunner().Run(bound, origin.Key).Failures[0].StartsWith("Case 'Small' of 'Recording':", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Amount required")]
    [InlineData("$strings.amountRequired")]
    void should_bind_case_error_messages_like_quoted_messages(string reason)
    {
        var table = Table.Replace("parameter amount Int", "parameter amount Int\n        parameter reason String", StringComparison.Ordinal)
            .Replace("case Small amount = 10", $"case Small amount = 10\n          reason = \"{reason}\"", StringComparison.Ordinal)
            .Replace("case Large amount = 100", $"case Large amount = 100\n          reason = \"{reason}\"", StringComparison.Ordinal)
            .Replace("then Recorded amount = case.amount", "then error case.reason", StringComparison.Ordinal);
        var expanded = Expanded.Replace("then Recorded amount = 100", $"then error \"{reason}\"", StringComparison.Ordinal)
            .Replace("then Recorded amount = 10", $"then error \"{reason}\"", StringComparison.Ordinal);
        var boundTable = Bind(Declarations + "\n" + table);
        var boundExpanded = Bind(Declarations + "\n" + expanded);
        Assert.True(boundTable.Success, string.Join('\n', boundTable.Diagnostics.Select(diagnostic => diagnostic.Message)));
        boundExpanded.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(boundTable.Value.Model).SequenceEqual(SemanticModelSerializer.Serialize(boundExpanded.Value.Model)).ShouldBeTrue();
        boundTable.Value.Model.Revision.ShouldEqual(boundExpanded.Value.Model.Revision);
        boundTable.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.All(specification => specification.ThenErrors.Single().Message == reason).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Int optional", DiagnosticCodes.OptionalSpecificationParameterTarget)]
    [InlineData("Decimal", DiagnosticCodes.IncompatibleSpecificationParameterType)]
    void should_check_parameter_types_before_substitution(string type, string code) => Bind(Declarations + "\n" + Table.Replace("parameter amount Int", $"parameter amount {type}", StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
}
