// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    static SemanticValidationSeverity ParseSeverity(string value) => value switch
    {
        "error" => SemanticValidationSeverity.Error,
        "information" => SemanticValidationSeverity.Information,
        "warning" => SemanticValidationSeverity.Warning,
        _ => throw new InvalidSemanticContract($"Unknown validation severity '{value}'.")
    };
}
