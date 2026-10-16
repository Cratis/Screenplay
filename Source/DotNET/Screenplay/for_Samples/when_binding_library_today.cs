// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_library_today : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        _result = Bind(File.ReadAllText(Path.Combine(root, "Samples/Library/library.play")));
    }

    [Fact] void should_not_admit_today_with_date_ordering() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("date", StringComparison.Ordinal)).ShouldBeTrue();
}
