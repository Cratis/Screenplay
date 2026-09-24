// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_validation_severities : a_valid_semantic_model
{
    ExecutableSemanticModel _roundTrip;
    string _json;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var rule = command.Validations.Single();
        var condition = new SemanticComparison(
            new(command.Properties.Single(_ => _.Name == "Name").Id, null),
            SemanticComparisonOperator.NotEqual,
            new(default, SemanticValue.Text("forbidden")));
        var updated = command with
        {
            Validations = [rule, rule with { Severity = SemanticValidationSeverity.Warning }],
            Requirements = [new(condition, "Forbidden") { Severity = SemanticValidationSeverity.Information }]
        };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(slice with { Commands = [updated], Specifications = [] }));
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(model));
        _roundTrip = SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(_json));
    }

    [Fact] void should_omit_default_error() => _json.Split("\"severity\"", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_write_warning_and_information() => (_json.Contains("\"severity\":\"warning\"") && _json.Contains("\"severity\":\"information\"")).ShouldBeTrue();
    [Fact] void should_read_missing_severity_as_error() => Command.Validations[0].Severity.ShouldEqual(SemanticValidationSeverity.Error);
    [Fact] void should_read_explicit_warning() => Command.Validations[1].Severity.ShouldEqual(SemanticValidationSeverity.Warning);
    [Fact] void should_read_requirement_information() => Command.Requirements.Single().Severity.ShouldEqual(SemanticValidationSeverity.Information);

    SemanticCommand Command => _roundTrip.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0).Commands.Single();
}
