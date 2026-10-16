// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_a_described_command : Specification
{
    SemanticApplication _application;
    SemanticModule _module;
    SemanticFeature _feature;
    SemanticSlice _slice;
    SemanticCommand _command;

    void Establish()
    {
        _application = canonical_serialization_golden_vectors.CreateSemanticModelV23().Application;
        _module = _application.Modules[0];
        _feature = _module.Features[^1];
        _slice = _feature.Slices.Single();
        _command = _slice.Commands.Single();
    }

    [Theory]
    [InlineData("missing handler")]
    [InlineData("identity")]
    [InlineData("result")]
    [InlineData("empty")]
    [InlineData("conditional")]
    [InlineData("duplicate")]
    [InlineData("mapping type")]
    void should_refuse_invalid_handler_contracts(string kind)
    {
        var command = kind switch
        {
            "missing handler" => _command with { Handler = null },
            "identity" => _command with { Handler = _command.Handler! with { RequirementId = new string('b', 64) } },
            "result" => _command with { Handler = _command.Handler! with { ResultVersion = 2 } },
            "empty" => _command with { Produces = [] },
            "conditional" => _command with { Produces = [_command.Produces.Single() with { Condition = SemanticExpression.FromValue(SemanticValue.Boolean(true)) }] },
            "mapping type" => _command with { Produces = [_command.Produces.Single() with { Mappings = [new(_slice.Events.Single().Properties.Single().Id, SemanticExpression.FromValue(SemanticValue.Boolean(true)))] }] },
            _ => _command with { Id = SemanticId.Parse($"sem1:{123099:x64}") }
        };
        var application = _application with { Modules = _application.Modules.SetItem(0, _module with { Features = _module.Features.SetItem(_module.Features.Length - 1, _feature with { Slices = [_slice with { Commands = kind == "duplicate" ? [_command, command] : [command] }] }) }) };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V23, SemanticVersion.V23, application, 2026)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact] void should_refuse_downgrading_the_handler() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, _application, 2026)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_parse_v23_language() => LanguageVersion.Parse("23.0").ShouldEqual(LanguageVersion.V23);
    [Fact] void should_parse_v23_semantics() => SemanticVersion.Parse("23.0").ShouldEqual(SemanticVersion.V23);
}
