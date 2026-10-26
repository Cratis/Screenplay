// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_an_attached_command_handler;

namespace Cratis.Screenplay.Semantics.for_SemanticHandlerLinkage;

public class when_validating_a_resolved_handler : for_SemanticModelBinder.given.a_semantic_binder
{
    SemanticCompilation _compilation;
    SemanticImplementationRequirement _requirement;

    void Establish()
    {
        var result = Bind(and_the_inline_body_is_constrained.Source);
        _compilation = result.Value!;
        _requirement = result.ImplementationRequirements.Single();
    }

    [Fact] void should_accept_the_resolved_body() => Validate([_requirement]);
    [Fact] void should_refuse_a_missing_sidecar() => Assert.Throws<InvalidSemanticContract>(() => Validate([]));
    [Fact] void should_refuse_duplicate_sidecars() => Assert.Throws<InvalidSemanticContract>(() => Validate([_requirement, _requirement]));

    [Theory]
    [InlineData("hash")]
    [InlineData("uppercase hash")]
    [InlineData("body")]
    [InlineData("language")]
    [InlineData("hints")]
    [InlineData("unresolved")]
    [InlineData("ambiguous")]
    [InlineData("context")]
    [InlineData("result")]
    [InlineData("owner")]
    void should_refuse_a_malformed_sidecar(string field)
    {
        var changed = MalformedBody(field) ?? MalformedContract(field);
        Assert.Throws<InvalidSemanticContract>(() => Validate([changed]));
    }

    [Fact]
    void should_preserve_the_pending_checks()
    {
        var pending = _requirement with { AttachmentResolution = SemanticAttachmentResolution.Pending, Language = null, File = null, ContentHash = string.Empty, BodySpan = null, BodyLines = [] };
        Validate([pending]);
        Assert.Throws<InvalidSemanticContract>(() => Validate([pending with { ContentHash = _requirement.ContentHash }]));
    }

    SemanticImplementationRequirement? MalformedBody(string field) => field switch
    {
        "hash" => _requirement with { ContentHash = string.Empty },
        "uppercase hash" => _requirement with { ContentHash = _requirement.ContentHash.ToUpperInvariant() },
        "body" => _requirement with { BodySpan = null, File = null },
        "language" => _requirement with { Language = null },
        "hints" => _requirement with { Hints = ["Not an attached result"] },
        _ => null
    };

    SemanticImplementationRequirement MalformedContract(string field) => field switch
    {
        "unresolved" => _requirement with { AttachmentResolution = SemanticAttachmentResolution.UnresolvedFile },
        "ambiguous" => _requirement with { IsAmbiguous = true },
        "context" => _requirement with { ContextVersion = 2 },
        "result" => _requirement with { ResultVersion = 2 },
        _ => _requirement with { Owner = SemanticAddress.ForApplication(_applicationIdentity) }
    };

    void Validate(ImmutableArray<SemanticImplementationRequirement> requirements) => SemanticHandlerLinkage.Validate(_compilation.Model.Application, requirements);
}
