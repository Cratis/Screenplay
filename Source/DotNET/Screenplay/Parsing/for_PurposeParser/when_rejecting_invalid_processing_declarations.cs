// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PurposeParser;

public class when_rejecting_invalid_processing_declarations : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile("""
        purpose Billing
          basis unknown
          basis consent
          condition unknown
          erasure exception unknown
          subjects invalid-name
        purpose Billing
          interest "Avoid fraud"
        module Finance
          purpose Missing
        """);

    [Fact] void should_reject_unknown_vocabulary() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPurposeVocabulary).ShouldEqual(3);
    [Fact] void should_reject_a_repeated_singleton() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicatePurposeField).ShouldBeTrue();
    [Fact] void should_reject_duplicate_names() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicatePurposeDeclaration).ShouldBeTrue();
    [Fact] void should_warn_on_unknown_references() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPurpose).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_warn_on_misplaced_interest() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PurposeInterestMismatch).Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
