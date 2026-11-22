// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_Agents;

public class when_checking_the_exposed_operation_count : given.an_agent_model
{
    const string Code = "PLAY1167";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(30)]
    void should_warn_only_above_twenty_distinct_operations(int count)
    {
        var result = Compiler.Compile(Source(count));
        result.Success.ShouldBeTrue();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == Code).ShouldEqual(count > 20 ? 1 : 0);
    }

    [Fact]
    void should_report_the_count_at_the_block_with_typed_arguments()
    {
        var source = Source(21);
        var warning = Compiler.Compile(source).Diagnostics.Single(diagnostic => diagnostic.Code == Code);
        warning.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        warning.Message.ShouldEqual("The agents block exposes 21 operations; expose at most 20 focused operations to keep agent tool selection effective");
        warning.Location.Line.ShouldEqual(source.Split('\n').ToList().IndexOf("agents") + 1);
        warning.Location.Column.ShouldEqual(1);
        warning.Arguments["count"].ShouldEqual(DiagnosticArgument.Text("21"));
        warning.Arguments["recommendedMaximum"].ShouldEqual(DiagnosticArgument.Text("20"));
    }

    [Fact]
    void should_not_count_unexposed_declarations()
    {
        var source = Source(21);
        var model = source[..source.IndexOf("agents\n", StringComparison.Ordinal)];
        Compiler.Compile(model).Diagnostics.ShouldBeEmpty();
        Compiler.Compile(model + "agents\n  tokens personal").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.AgentsExposeNothing, DiagnosticCodes.UnexposedAgentOperations]);
    }

    static string Source(int count)
    {
        var operations = Enumerable.Range(0, count).Select(index => (Kind: index % 2 == 0 ? "command" : "query", Name: $"Operation{index}")).ToArray();
        var declarations = string.Join('\n', operations.Select(operation => operation.Kind == "command" ? $"      command {operation.Name}\n        id ItemId identifier" : $"      query {operation.Name} => Row[]"));
        var exposures = string.Join('\n', operations.Select(operation => $"  {operation.Kind} {operation.Name}\n    scope {(operation.Kind == "command" ? "write" : "read")}\n    description \"Operation\""));
        return $"concept ItemId : Uuid\npolicy Staff\n  require role \"Staff\"\nmodule M\n  authorize Staff\n  feature F\n    slice StateChange S\n      readmodel Row\n        id ItemId\n{declarations}\nagents\n  tokens personal\n{exposures}";
    }
}
