// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_DesignSystem.given;

/// <summary>
/// A complete, warning-free application with a design system, a theme that implements it and a profile, to mutate one line at a time.
/// </summary>
public static class a_design_system_document
{
    /// <summary>
    /// The complete document.
    /// </summary>
    public const string Source =
        """
        design system D
          modes light, dark
          default theme T
          tokens
            color
              surface    "page background"
              text       "body text"
              primary    "primary action"
              on-primary "text on a primary action"
            space
              sm
            radius
              control
          components
            Button
              parts text, background, radius
              variant primary
                text       = color.on-primary
                background = color.primary
                radius     = radius.control
                state hover
                  background = color.primary
                state disabled
                  text       = color.text
                  background = color.surface
          words
            voice
              tone = "Plain"
            naming
              workspace = "Space"
          rules
            contrast text on background at least 4.5

        theme T
          implements D
          compatible with core
          palette
            white = "#FFFFFF"
            ink   = "#111827"
            blue  = "#1D4ED8"
          values
            space.sm       = "8px"
            radius.control = "6px"
          light
            color.surface    = palette.white
            color.text       = palette.ink
            color.primary    = palette.blue
            color.on-primary = palette.white
          dark
            color.surface    = palette.ink
            color.text       = palette.white
            color.primary    = palette.blue
            color.on-primary = palette.white

        ui profile P
          appearance
            color scheme dark
        """;
}
