// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_importing;

public class and_the_application_declares_the_name : given.a_compiler
{
    const string Source =
        """
        import External.OrderPlaced
        concept OrderId : Uuid
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                produces OrderPlaced
                  for orderId
                  missing = orderId
        module Tracking
          feature History
            slice StateView Orders
              event OrderPlaced
                orderId OrderId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_check_against_the_declaration_in_the_application() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEventField).ShouldEqual(1);
    [Fact] void should_report_the_import_as_redundant() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).Message.ShouldEqual("Import 'External.OrderPlaced' names 'OrderPlaced', which this application declares - the import has no effect");
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_it_at_the_import() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).Location.Line.ShouldEqual(1);
}
