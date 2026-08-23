// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_renaming_expression_sources : a_valid_semantic_model
{
    ExecutableSemanticModel _renamed;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var renamed = command with
        {
            Name = "CreateProject",
            Properties =
            [
                command.Properties[0] with { Name = "Identifier" },
                command.Properties[1] with { Name = "DisplayName" }
            ]
        };
        _renamed = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [renamed] }));
    }

    [Fact]
    void should_keep_the_stable_property_target() =>
        ((SemanticResolvedExpression)_renamed.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0)
            .Commands.Single().Produces.Single().Mappings[0].Source).Target.ShouldEqual(_commandProjectIdPropertyId);
}
#endif
