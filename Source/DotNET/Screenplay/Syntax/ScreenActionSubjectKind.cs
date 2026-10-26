// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Identifies the data subject used by a screen action.
/// </summary>
public enum ScreenActionSubjectKind
{
    /// <summary>The item supplied by a table or record context.</summary>
    Item,

    /// <summary>The nearest single-record screen data context.</summary>
    Screen
}
