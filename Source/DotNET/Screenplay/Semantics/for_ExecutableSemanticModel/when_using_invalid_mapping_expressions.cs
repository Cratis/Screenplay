// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_mapping_expressions : a_valid_semantic_model
{
    Exception _wrongRoot;
    Exception _wrongSource;
    Exception _wrongType;
    Exception _unresolvedTarget;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var produced = command.Produces.Single();
        _wrongRoot = Validate(produced with
        {
            Mappings = produced.Mappings.SetItem(0, produced.Mappings[0] with
            {
                Source = SemanticExpression.Property(SemanticExpressionRootKind.Event, _commandProjectIdPropertyId)
            })
        });
        _wrongSource = Validate(produced with
        {
            Mappings = produced.Mappings.SetItem(0, produced.Mappings[0] with
            {
                Source = new SemanticResolvedExpression(
                    SemanticExpressionRootKind.Command,
                    (SemanticExpressionSourceKind)1,
                    _queryArgumentId)
            })
        });
        _wrongType = Validate(produced with
        {
            Mappings = produced.Mappings.SetItem(1, produced.Mappings[1] with
            {
                Source = SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandProjectIdPropertyId)
            })
        });
        _unresolvedTarget = Validate(produced with
        {
            Mappings = produced.Mappings.SetItem(0, produced.Mappings[0] with
            {
                Source = SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(SemanticKind.Property, "RegisterProject.Missing"))
            })
        });

        Exception Validate(SemanticProducedEvent replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [command with { Produces = [replacement] }] })));
    }

    [Fact] void should_reject_the_wrong_root() => _wrongRoot.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_wrong_source_kind() => _wrongSource.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_wrong_type() => _wrongType.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_unresolved_target() => _unresolvedTarget.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
