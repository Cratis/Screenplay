// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_duplicate_mapping_targets : a_valid_semantic_model
{
    Exception _duplicate;
    Exception _missingRequired;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var produced = command.Produces.Single();
        var duplicate = produced with { Mappings = [produced.Mappings[0], produced.Mappings[0]] };
        _duplicate = Validate(duplicate);
        _missingRequired = Validate(produced with { Mappings = [produced.Mappings[0]] });

        Exception Validate(SemanticProducedEvent replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [command with { Produces = [replacement] }] })));
    }

    [Fact] void should_reject_the_duplicate_target() => _duplicate.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_missing_required_target() => _missingRequired.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
