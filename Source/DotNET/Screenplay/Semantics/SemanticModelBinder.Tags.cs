// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ImmutableArray<string> BindTags(IEnumerable<TagSyntax>? tags)
        {
            var result = ImmutableArray.CreateBuilder<string>();
            foreach (var tag in tags ?? [])
            {
                if (tag.Value is LiteralExpressionSyntax { Value: string value } && !string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
                else
                {
                    var reason = tag.Value is ContextExpressionSyntax ? "$context tag values require ESM v2 (#226)" : "only nonempty literal text tags are admitted";
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Tag is not admitted: {reason}.", tag.Location);
                }
            }

            return result.ToImmutable();
        }
    }
}
