// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    static List<Diagnostic> CommandProductionAdmission(ApplicationSyntax syntax)
    {
        var walker = new CommandProductionAdmissionWalker();
        walker.VisitApplication(syntax);

        return walker.Diagnostics;
    }

    sealed class CommandProductionAdmissionWalker : ScreenplaySyntaxWalker
    {
        internal List<Diagnostic> Diagnostics { get; } = [];

        public override void VisitNode(SyntaxNode node)
        {
            if (node is InvocationRefusalSyntax or RefusalExpressionSyntax or SpecificationRedeliverySyntax)
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Reaction refusal handling and redelivery are not admitted by any supported executable model (ESM) version yet (#433).",
                    node.Location));
            }

            if (node is SpecificationSyntax { ThenNoEvents: true } specification)
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Explicit no-event assertions are not admitted by any supported executable model (ESM) version yet (#433).",
                    specification.DirectiveLocations.GetValueOrDefault("then no events", specification.Location)));
            }

            if (node is SpecificationStreamSyntax or SpecificationNoStreamSyntax)
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Specification event routes are not admitted by any supported executable model (ESM) version yet (#457).",
                    node.Location));
            }
            if (node is EventSourceSyntax or EventStreamSyntax or CommandStreamSyntax)
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Event sources, streams and routes are not admitted by any supported executable model (ESM) version yet (#302).",
                    node.Location));
            }
            if (node is SystemSyntax or OperationSyntax or OperationPhaseSyntax or SpecificationOperationFailureSyntax or SpecificationOperationSyntax or SpecificationCompensatedSyntax)
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Operations and systems are not admitted by any supported executable model (ESM) version yet (#301).",
                    node.Location));
            }
        }

        // Intent has no production feature gates. Do not dereference malformed hint collections here;
        // named-rule binding reports their typed diagnostics while retaining partial attachments.
        public override void VisitImplementation(ImplementationSyntax syntax) => VisitNode(syntax);
    }
}
