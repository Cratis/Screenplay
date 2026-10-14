// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_checksum_before_admission : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _string;
    CompilationResult<SemanticCompilation>[] _primitives;

    void Because()
    {
        _string = Bind("concept InvoiceRef : String\n  validate checksum luhn");
        _primitives = [.. new[] { "Int", "Decimal", "Bool", "Uuid", "Date", "DateTime", "Enum" }.Select(type => Bind($"concept InvoiceRef : {type}\n  validate checksum luhn"))];
    }

    [Fact] void should_admit_string_checksum_only_at_v21() => _string.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_refuse_every_non_string_primitive() => _primitives.ShouldEachConformTo(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("String concept", StringComparison.Ordinal)));
}
