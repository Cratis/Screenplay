// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_policy_file : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _attached;
    CompilationResult<SemanticCompilation> _declarative;

    void Because()
    {
        _attached = Bind("policy Access\n  file Policies/Access.cs");
        _declarative = Bind("policy Access\n  require authenticated");
    }

    [Fact] void should_block_a_file_attachment() => _attached.Success.ShouldBeFalse();
    [Fact] void should_explain_the_attachment() => _attached.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("Policy 'Access' uses file; portable implementation attachments are deferred to #139.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_bind_the_declarative_alternative() => _declarative.Success.ShouldBeTrue();
    [Fact] void should_keep_the_declarative_policy() => _declarative.Value!.Model.Application.Policies.Single().Name.ShouldEqual("Access");
}
