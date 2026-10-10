// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Samples;

// The older binder specification uses a legacy embedded compiler fixture.
// This specification pins the new forms in the maintained root sample.
public class when_binding_the_invoicing_sample : Specification
{
    Diagnostic[] _diagnostics;
    ApplicationSyntax _application;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"));
        _application = new ScreenplayCompiler().Compile(source).Value!;
        var workspace = ScreenplayWorkspace.Create("Invoicing",
            [WorkspaceDocument.Create("sample", PortablePlayPath.Parse("invoicing.play"), Encoding.UTF8.GetBytes(source))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing")));
        _diagnostics = [.. workspace.Compilation.Diagnostics];
    }

    [Fact]
    void should_pin_the_composite_key_disposition() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("InvoicePaymentMethodTotals", StringComparison.Ordinal) &&
        diagnostic.Message.Contains("issues/599", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_keep_the_explicit_single_key_out_of_identity_ambiguity_dispositions() => _diagnostics.Any(diagnostic =>
        diagnostic.Message.Contains("Read model 'InvoiceListReadModel' must have one unambiguous keyed query", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_pin_command_reads_as_a_separate_disposition() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.PreservedLegacySemanticSyntax && diagnostic.Message.Contains("reads 'InvoicePaymentMethodTotals'", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_not_refuse_the_new_route_or_filter_constructs() => _diagnostics.Where(diagnostic => diagnostic.Code is
        DiagnosticCodes.ProductionRouteOutsideCommand or DiagnosticCodes.RedundantProductionRoute or DiagnosticCodes.InvalidObserverFilter or DiagnosticCodes.ObserverFilterExcludesEveryProducer).ShouldBeEmpty();

    [Fact]
    void should_show_both_key_shapes_and_production_routes() => SyntaxNodes.Under(_application).OfType<ReadModelSyntax>().Count(model => model.Properties.Any(property => property.IsKey)).ShouldEqual(3);
}
