// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_default_and_malformed_values : a_valid_semantic_model
{
    Exception _defaultTarget;
    Exception _malformedUuid;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var produced = command.Produces.Single();
        var defaultTarget = produced with
        {
            Mappings = produced.Mappings.SetItem(0, produced.Mappings[0] with
            {
                Source = SemanticExpression.Property(SemanticExpressionRootKind.Command, default)
            })
        };
        _defaultTarget = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [command with { Produces = [defaultTarget] }] })));

        var success = slice.Specifications.Single(_ => _.ThenEvents.Length > 0);
        var malformed = success.When with
        {
            Values = success.When.Values.SetItem(0, success.When.Values[0] with { Value = SemanticValue.Text("project-1") })
        };
        _malformedUuid = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Specifications = [.. slice.Specifications.Select(_ => _.Id == success.Id ? success with { When = malformed } : _)]
            })));
    }

    [Fact] void should_reject_a_default_resolved_target() => _defaultTarget.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_noncanonical_uuid() => _malformedUuid.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
