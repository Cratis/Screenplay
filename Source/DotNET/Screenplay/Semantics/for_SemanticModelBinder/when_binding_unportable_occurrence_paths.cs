// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_unportable_occurrence_paths : given.a_semantic_binder
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
                  audit = $context.PATH
              event ProjectRegistered
                audit String
        """;

    CompilationResult<SemanticCompilation>[] _results = [];

    void Because() => _results = [
        Bind(Source.Replace("PATH", "identity.roles", StringComparison.Ordinal)),
        Bind(Source.Replace("PATH", "identity.claims.department", StringComparison.Ordinal)),
        Bind(Source.Replace("PATH", "causation.type", StringComparison.Ordinal)),
        Bind(Source.Replace("PATH", "tenant", StringComparison.Ordinal)),
        Bind(Source.Replace("PATH", "correlation", StringComparison.Ordinal))
    ];

    [Fact] void should_reject_every_unportable_path() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_precise_unsupported_semantics() => _results.All(result => result.Diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("no scalar counterpart", StringComparison.Ordinal))).ShouldBeTrue();
}
