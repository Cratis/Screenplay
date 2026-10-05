// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Text;

// Unicode White_Space, matching the existing C# hint rule. Keep the explicit set in
// TypeScript Text/ImplementationHintText.ts in sync; ECMAScript trim differs at NEL and BOM.
internal static class ImplementationHintText
{
    internal static bool IsBlank(string? text) => text?.All(character => character is
        (>= '\u0009' and <= '\u000d') or '\u0020' or '\u0085' or '\u00a0' or '\u1680' or
        (>= '\u2000' and <= '\u200a') or '\u2028' or '\u2029' or '\u202f' or '\u205f' or '\u3000') ?? true;
}
