// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        void ReportUnsupportedSliceMembers(SliceSyntax slice)
        {
            foreach (var reducer in slice.Reducers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reducer '{reducer.Name}' requires a portable reducer contract.", reducer.Location);
            }

            foreach (var reaction in slice.Reactions)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' requires portable occurrence and effect semantics.", reaction.Location);
            }

            foreach (var capture in slice.Captures)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture.Name}' requires a portable compiled CDL plan.", capture.Location);
            }

            foreach (var constraint in slice.Constraints)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Constraint '{constraint.Name}' requires a portable constraint contract.", constraint.Location);
            }

            foreach (var screen in slice.Screens)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen '{screen.Name}' is explicitly deferred from the backend ESM v1 profile.", screen.Location);
            }
        }
    }
}
