// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_identity_expressions : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  subject = $identity.id
                  name = $identity.name
                  userName = $identity.userName
              event ProjectRegistered
                subject String
                name String
                userName String
        """;

    CompilationResult<SemanticCompilation> _identity;
    CompilationResult<SemanticCompilation> _context;

    void Because()
    {
        _identity = Bind(Source);
        _context = Bind(Source.Replace("$identity.", "$context.identity.", StringComparison.Ordinal));
    }

    SemanticProducedEvent Produced => _identity.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();

    [Fact] void should_bind_without_diagnostics() => _identity.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_legacy_form_without_diagnostics() => _context.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_same_context_kinds() => Produced.Mappings.Select(mapping => ((SemanticEventContextExpression)mapping.Source).Value).ShouldEqual([SemanticEventContextValueKind.CausedBySubject, SemanticEventContextValueKind.CausedByName, SemanticEventContextValueKind.CausedByUserName]);
    [Fact] void should_keep_canonical_executable_bytes_identical() => SemanticModelSerializer.Serialize(_identity.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_context.Value!.Model)).ShouldBeTrue();
}
