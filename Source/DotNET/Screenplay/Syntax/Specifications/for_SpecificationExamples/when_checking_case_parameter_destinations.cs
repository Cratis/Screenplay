// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_checking_case_parameter_destinations : Specification
{
    const string Declarations = """
        module M
          feature F
            slice StateChange Receiving
              event Received
              command Receive
                id Uuid identifier
                produces Received
                  for id
            slice StateChange Recording
              event Recorded
              command Record
                id Int identifier
                produces Recorded
                  for id
        """;
    const string History = """
              specification Recording
                parameter receivedId Uuid
                case Prior receivedId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                given Received
                  for case.receivedId
                when Record id = 42
                then Recorded
                  for 42
        """;
    const string Append = """
              specification Appending
                parameter id Uuid
                case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when append Received
                  for case.id
                then Received
        """;

    [Fact] void should_use_the_given_events_producer_not_the_action_command() => TypeDiagnostics(Declarations + "\n" + History).ShouldBeEmpty();
    [Fact] void should_type_an_append_without_a_command_action() => TypeDiagnostics(Declarations + "\n" + Append).ShouldBeEmpty();
    [Fact] void should_reject_an_incompatible_append_parameter() => TypeDiagnostics(Declarations + "\n" + Append.Replace("parameter id Uuid", "parameter id Int", StringComparison.Ordinal).Replace("\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", "1", StringComparison.Ordinal)).Single().Code.ShouldEqual(DiagnosticCodes.IncompatibleSpecificationParameterType);
    [Fact] void should_reject_an_optional_append_parameter() => TypeDiagnostics(Declarations + "\n" + Append.Replace("parameter id Uuid", "parameter id Uuid optional", StringComparison.Ordinal)).Single().Code.ShouldEqual(DiagnosticCodes.OptionalSpecificationParameterTarget);

    [Theory]
    [InlineData("Decimal", DiagnosticCodes.IncompatibleSpecificationParameterType)]
    [InlineData("Int optional", DiagnosticCodes.OptionalSpecificationParameterTarget)]
    void should_check_trigger_values(string type, string code)
    {
        var source = $"trigger Kick\n  amount Int\nmodule M\n  feature F\n    slice Automation S\n      specification Kicking\n        parameter value {type}\n        case One value = 1\n        when trigger Kick\n          amount = case.value";
        TypeDiagnostics(source).Single().Code.ShouldEqual(code);
    }

    [Theory]
    [InlineData("      reaction Forward\n        when Kick\n          produces Received\n            for id", "trigger Kick\n  id Uuid\n", true)]
    [InlineData("      reaction Forward\n        when Kick\n          produces Received\n            for \"record\"", "trigger Kick\n", true)]
    [InlineData("      event Prior\n      command Seed\n        id Uuid identifier\n        produces Prior\n          for id\n      reaction Forward\n        when Prior\n          produces Received", "", true)]
    [InlineData("      capture Legacy\n        identified by id\n        append Received", "", true)]
    [InlineData("      reaction Forward\n        when Unknown\n          produces Received\n            for missing", "", false)]
    [InlineData("      event Prior\n      reaction Forward\n        when Prior\n          produces Received\n        when Received\n          produces Prior", "", false)]
    [InlineData("      command First\n        id Uuid identifier\n        produces Received\n          for id\n      command Second\n        id String identifier\n        produces Received\n          for id", "", false)]
    void should_infer_other_producers_without_guessing_unknown_types(string producer, string prefix, bool known)
    {
        var source = $"{prefix}module M\n  feature F\n    slice Automation S\n      event Received\n{producer}\n      specification Appending\n        parameter id Int\n        case One id = 1\n        when append Received\n          for case.id\n        then Received";
        TypeDiagnostics(source).Count(diagnostic => diagnostic.Code == DiagnosticCodes.IncompatibleSpecificationParameterType).ShouldEqual(known ? 1 : 0);
    }

    [Fact]
    void should_use_the_production_destination_instead_of_an_unrelated_identifier()
    {
        var declarations = Declarations.Replace("id Uuid identifier", "id Int identifier\n        target Uuid", StringComparison.Ordinal).Replace("for id\n    slice", "for target\n    slice", StringComparison.Ordinal);
        TypeDiagnostics(declarations + "\n" + History).ShouldBeEmpty();
    }

    [Fact]
    void should_check_error_parameter_types_in_standalone_expansion()
    {
        var application = new ScreenplayCompiler().Parse(string.Empty).Value;
        var specification = new ScreenplayCompiler().CompileSpecification("specification Rejecting\n  parameter reason Int\n  case One reason = 1\n  then error case.reason").Value;
        SpecificationExamples.ExpandAll(specification, application, []).Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.IncompatibleSpecificationParameterType);
    }

    static Diagnostic[] TypeDiagnostics(string source) => new ScreenplayCompiler().Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code is DiagnosticCodes.IncompatibleSpecificationParameterType or DiagnosticCodes.OptionalSpecificationParameterTarget).ToArray();
}
