// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    internal const bool SpecificationRoutesJoin = true;
    internal const bool CompositeStreamIdsJoin = true;

    static List<Diagnostic> CommandProductionAdmission(ApplicationSyntax syntax, out bool usesPublicEvents)
    {
        var walker = new CommandProductionAdmissionWalker();
        walker.VisitApplication(syntax);
        usesPublicEvents = walker.UsesPublicEvents || PublicEventUsageValidator.EventTargets(syntax).Count > 0;
        return walker.Diagnostics;
    }

    sealed class CommandProductionAdmissionWalker : ScreenplaySyntaxWalker
    {
        internal List<Diagnostic> Diagnostics { get; } = [];

        /// <summary>Gets whether the model declares a public event, an origin, a direction or an events source.</summary>
        internal bool UsesPublicEvents { get; private set; }

        public override void VisitNode(SyntaxNode node)
        {
            if (node is EventSyntax { Visibility: not EventVisibility.Private } or EventSyntax { Origin: not null })
            {
                UsesPublicEvents = true;
            }

            // An import names a declaration in another file; it carries no shape of its own, so a public or foreign import has
            // nothing to execute. The shape of a foreign public event is the local 'event X from "store"' declaration.
            if (node is ImportSyntax { Visibility: not EventVisibility.Private } or ImportSyntax { Origin: not null })
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "A public or foreign import has no local shape to execute. Declare the foreign public event with its own fields as 'event <Name> from \"<store>\"' instead (#481).",
                    node.Location));
            }

            if (node is SliceSyntax { Direction: not null })
            {
                UsesPublicEvents = true;
            }

            if (node is CaptureSourceSyntax source && CaptureEventsSource.IsEvents(source))
            {
                UsesPublicEvents = true;
            }

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

            if (!SpecificationRoutesJoin && (node is SpecificationStreamSyntax or SpecificationNoStreamSyntax))
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Specification event routes are not admitted by any supported executable model (ESM) version yet (#457).",
                    node.Location));
            }
            if (!CompositeStreamIdsJoin && ((node is EventStreamSyntax declaration && declaration.StreamIdParts.Any()) ||
                (node is CommandStreamSyntax route && route.StreamIdParts.Any()) ||
                (node is SpecificationStreamSyntax fixture && fixture.StreamIdParts.Any())))
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    "Composite stream ids are not admitted by any supported executable model (ESM) version yet (#462).",
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
