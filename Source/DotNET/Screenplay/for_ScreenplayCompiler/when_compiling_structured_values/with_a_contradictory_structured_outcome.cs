// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_contradictory_structured_outcome : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var position = Source.LastIndexOf(Lines, StringComparison.Ordinal);
        _result = Compiler.Compile($"{Source[..position]}lines none{Source[(position + Lines.Length)..]}");
    }

    [Fact] void should_reject_the_incompatible_outcome() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
}
