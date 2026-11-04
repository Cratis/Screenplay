// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        void RefuseApplicationContext()
        {
            if (syntax.Context is null) return;
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Application context members are not yet admitted (decision 0132) (#824).", syntax.Context.Location);
        }
    }
}
