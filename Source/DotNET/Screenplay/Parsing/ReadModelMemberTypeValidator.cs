// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks that read-model members reference reusable value types, not read-model identities.
/// </summary>
internal static class ReadModelMemberTypeValidator
{
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var slices = ScreenplayValidator.ScopedSlices(application).Select(entry => entry.Slice).ToArray();
        var names = DocumentNames.Of(application, slices);
        foreach (var model in slices.SelectMany(slice => slice.ReadModels ?? []))
        {
            foreach (var property in model.Properties.Where(property => !names.Types.Contains(property.Type.Name)))
            {
                context.Error(
                    DiagnosticCodes.InvalidSemanticBinding,
                    $"Read model '{model.Name}' member '{property.Name}' references unsupported type '{property.Type.Name}'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.",
                    property.Type.Location);
            }
        }
    }
}
