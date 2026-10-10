// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_caller_identity_in_a_capture : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Legacy
            slice Translate LegacySync
              capture LegacyCapture
                source api
                  api LegacyApi
                key id
                append LegacyChanged
                  when name
                    caller = $identity.id
              event LegacyChanged
                caller String
        """;

    CompilationResult<SemanticCompilation>[] _results;

    void Because() => _results = [Bind(Source), Bind(Source.Replace("$identity.id", "$context.identity.id", StringComparison.Ordinal))];

    [Fact] void should_refuse_both_caller_spellings() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_the_same_unsupported_capture_mapping() => _results.Select(result => result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).Message).Distinct(StringComparer.Ordinal).Count().ShouldEqual(1);
}
