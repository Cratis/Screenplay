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
        const string value = "[{\"sku\":\"A-1\",\"status\":\"open\"}]";
        var position = Source.LastIndexOf(value, StringComparison.Ordinal);
        _result = Compiler.Compile($"{Source[..position]}[]{Source[(position + value.Length)..]}");
    }

    [Fact] void should_reject_the_incompatible_outcome() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
}
