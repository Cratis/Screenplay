// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_uuid_comparisons : for_SemanticModelBinder.when_binding_uuid_comparisons.given.a_uuid_comparison
{
    ExecutableSemanticModel _bound;
    SemanticApplication _application;
    SemanticCommand _command;

    void Establish()
    {
        var result = Bind(Model($"requestedBy == \"{Canonical}\" and View.memberId != requestedBy and raw == other"));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _bound = result.Value!.Model;
        _application = _bound.Application;
        _command = Commands(_application).Single();
    }

    [Fact] void should_accept_the_bound_model_at_the_selected_version() => Create(_application, _bound.LanguageVersion, _bound.SemanticVersion).ShouldNotBeNull();
    [Fact] void should_reject_the_shape_at_the_previous_version() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V27, SemanticVersion.V27, _application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_uuid_comparison_at_the_protected_read_version() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V25, SemanticVersion.V25, _application)).Message.ShouldEqual("Uuid command comparisons require ESM v28.");
    [Fact] void should_reject_a_non_canonical_constant() => Refuse(Replace(_command, comparison => comparison.Right.Value is null ? comparison : comparison with { Right = new(default, SemanticValue.Text(Canonical.ToUpperInvariant())) }));
    [Fact] void should_reject_a_non_text_constant() => Refuse(Replace(_command, comparison => comparison.Right.Value is null ? comparison : comparison with { Right = new(default, SemanticValue.Number(5)) }));
    [Fact] void should_reject_ordering() => Refuse(Replace(_command, comparison => comparison with { Operator = SemanticComparisonOperator.GreaterThan }));
    [Fact] void should_reject_a_constant_on_the_left() => Refuse(Replace(_command, comparison => comparison with { Left = new(default, SemanticValue.Text(Canonical)) }));

    [Fact]
    void should_reject_a_claimed_version_without_a_uuid_comparison()
    {
        var plain = Bind(Model("note != \"\"")).Value!.Model;
        Catch.Exception(() => ExecutableSemanticModel.Create(_bound.LanguageVersion, _bound.SemanticVersion, plain.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_reject_two_different_concepts()
    {
        var member = _command.Properties.Single(property => property.Name == "requestedBy");
        var plant = _command.Properties.Single(property => property.Name == "plant");
        Refuse(Replace(_command, comparison => comparison.Left.Read is null && comparison.Left.Property == member.Id ? comparison with { Right = new(plant.Id, null) } : comparison));
    }

    static IEnumerable<SemanticCommand> Commands(SemanticApplication application) => application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands);

    static SemanticCondition Map(SemanticCondition condition, Func<SemanticComparison, SemanticComparison> change) => condition switch
    {
        SemanticComparison comparison => change(comparison),
        SemanticLogicalCondition logical => logical with { Left = Map(logical.Left, change), Right = Map(logical.Right, change) },
        _ => condition
    };

    static SemanticCommand Replace(SemanticCommand command, Func<SemanticComparison, SemanticComparison> change) => command with
    {
        Requirements = [.. command.Requirements.Select(requirement => requirement with { Condition = Map(requirement.Condition, change) })]
    };

    ExecutableSemanticModel Create(SemanticApplication application, LanguageVersion language, SemanticVersion semantic) => ExecutableSemanticModel.Create(language, semantic, application);

    void Refuse(SemanticCommand command)
    {
        var application = _application with
        {
            Modules = [.. _application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Commands.Length == 0 ? slice : slice with { Commands = [command] })]
                })]
            })]
        };
        Catch.Exception(() => Create(application, _bound.LanguageVersion, _bound.SemanticVersion)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
