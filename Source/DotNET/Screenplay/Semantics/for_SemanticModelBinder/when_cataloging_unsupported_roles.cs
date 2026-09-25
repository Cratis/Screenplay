// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_cataloging_unsupported_roles : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    SemanticImplementationRequirement _requirement;

    void Establish()
    {
        _result = Bind("module Billing\n  feature Accounts\n    slice StateChange Commands\n      command Deposit\n        validate csharp\n          ```csharp\n          return true;\n          ```");
        _requirement = _result.ImplementationRequirements.Single();
    }

    [Theory]
    [InlineData(SemanticImplementationRole.QueryPerformer)]
    [InlineData(SemanticImplementationRole.ReactionEffect)]
    [InlineData(SemanticImplementationRole.ConstraintPredicate)]
    void should_not_publish_a_descriptor_for_an_unsupported_role(SemanticImplementationRole role) =>
        SemanticTypedContextCatalog.Create(_result.Value!.Model.Application, [_requirement with { Role = role }], true).ShouldBeEmpty();
}
