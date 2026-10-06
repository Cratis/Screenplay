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
            if (node is PropertySyntax { IsGenerated: true } or CommandResponseSyntax or SpecificationReturnSyntax)
            {
                Reject(node.Location);
            }
        }

        // Intent has no production feature gates. Do not dereference malformed hint collections here;
        // named-rule binding reports their typed diagnostics while retaining partial attachments.
        public override void VisitImplementation(ImplementationSyntax syntax) => VisitNode(syntax);

        public override void VisitSpecificationCommand(SpecificationCommandSyntax syntax)
        {
            foreach (var fixture in syntax.GeneratedValues) Reject(fixture.Location);
            base.VisitSpecificationCommand(syntax);
        }

        void Reject(SourceLocation location) => Diagnostics.Add(Diagnostic.Error(
            DiagnosticCodes.UnsupportedSemanticSyntax,
            "Generated values, responses and return expectations are not admitted by any supported executable model (ESM) version yet (#300/#303).",
            location));
    }
}
