// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_described_command;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_linking_handler_requirements : given.a_semantic_binder
{
    SemanticApplication _application;
    SemanticImplementationRequirement _requirement;

    void Establish()
    {
        var result = Bind(and_it_produces_unconditionally.Source);
        _application = result.Value!.Model.Application;
        _requirement = result.ImplementationRequirements.Single();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("state")]
    [InlineData("role")]
    [InlineData("owner")]
    [InlineData("context")]
    [InlineData("result")]
    [InlineData("identity")]
    [InlineData("body")]
    void should_refuse_invalid_links(string kind)
    {
        ImmutableArray<SemanticImplementationRequirement> requirements = kind switch
        {
            "missing" => [],
            "duplicate" => [_requirement, _requirement],
            "state" => [_requirement with { AttachmentResolution = SemanticAttachmentResolution.Resolved }],
            "role" => [_requirement with { Role = SemanticImplementationRole.QueryPerformer }],
            "owner" => [_requirement with { Owner = SemanticAddress.ForCommand(SemanticAddress.ForSlice(_requirement.Owner.Application, "Other", "F", "S"), "Choose") }],
            "context" => [_requirement with { ContextVersion = 2 }],
            "result" => [_requirement with { ResultVersion = 2 }],
            "identity" => [_requirement with { RequirementId = new string('b', 64) }],
            _ => [_requirement with { File = "Choose.cs" }]
        };
        Catch.Exception(() => SemanticTypedContextCatalog.Create(_application, requirements, true)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_a_dangling_pending_requirement()
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = _application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [slice.Commands.Single() with { Handler = null }] }] }] }] };
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [_requirement], true)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
