// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_repeated_reads_with_aliases : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                destinationId Uuid
                reads Account as source by sourceId
                reads Account as destination by destinationId
                validate
                  require source.balance > 0 and destination.active == true
                    message "Both accounts must be available"
                produces TransferRecorded
                  balance = source.balance

              event TransferRecorded
                balance Int

            slice StateView Accounts
              projection Accounts => Account
                from AccountOpened key accountId
                  balance = balance

              event AccountOpened
                accountId Uuid
                balance Int
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_instances_of_the_view() => Reads.Select(read => read.ReadModel).ShouldEqual("Account", "Account");
    [Fact] void should_keep_distinct_aliases() => Reads.Select(read => read.Alias).ShouldEqual("source", "destination");
    [Fact] void should_keep_each_key() => Reads.Select(read => read.By).ShouldEqual("sourceId", "destinationId");
    [Fact] void should_retain_the_alias_qualified_requirement() =>
        ((ComparisonConditionSyntax)((LogicalConditionSyntax)((DeclarativeValidateSyntax)Command.Validations.Single()).Requirements!.Single().Condition).Left)
            .Left.ShouldEqual("source.balance");
    [Fact] void should_keep_the_alias_qualified_mapping() =>
        ((PathExpressionSyntax)Command.Produces.Single().Mappings.Single().Source).Path.ShouldEqual("source.balance");
    [Fact] void should_round_trip_the_alias_in_typed_json() =>
        SyntaxJson.StructurallyEqual(Command, SyntaxJson.Deserialize(SyntaxJson.Serialize(Command))).ShouldBeTrue();

    CommandSyntax Command => _result.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single();
    IEnumerable<ReadsSyntax> Reads => Command.Reads!;
}
