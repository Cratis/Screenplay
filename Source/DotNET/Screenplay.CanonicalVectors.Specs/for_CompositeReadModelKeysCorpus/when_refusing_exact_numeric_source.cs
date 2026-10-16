// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_exact_numeric_source : Specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because()
    {
        var single = CompositeReadModelKeysCorpus.SourceForms.Single(form => form.Name == "single");
        var document = single.Documents.Single();
        var exact = single with { Documents = [document with { Bytes = [.. Encoding.UTF8.GetBytes($"numbers exact\n{document.Text}")] }] };
        _result = when_loading_composite_read_model_keys.CompileSource(exact);
    }

    [Fact] void should_refuse_execution() => _result.Success.ShouldBeFalse();
    [Fact] void should_publish_no_model() => _result.Value.ShouldBeNull();
    [Fact] void should_preserve_the_exact_numeric_admission_refusal() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_name_the_exact_numeric_issue() => _result.Diagnostics.Single().Message.ShouldContain("#285");
}
