using System;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumStyles.AppStyleModels
{
    /// <summary>
    /// Type of theme used.
    /// </summary>
    internal enum ThemeTypes
    {
        DARK_COLORS = 0,
        LIGHT_COLORS = 1,
    }
    /// <summary>
    /// Type of color options
    /// </summary>
    [Flags]
    internal enum ColorTypes : uint
    {
        // Color Type Bases
        PRIMARY_COLOR = 0x001000,
        SECONDARY_COLOR = 0x002000,
        BASE_TEXT_COLOR = 0x003000,

        // Shade operation status
        DARKER_COLOR = 0x000100,
        SHADE_35_PCT = 0x000035,
        SHADE_65_PCT = 0x000065,

        // Predefined Colors
        PREDEFINED_COLOR = 0x100000,
        PRIMARY_COLOR_BASE = 0x101000,
        PRIMARY_DARKER_35 = 0x101135,
        PRIMARY_DARKER_65 = 0x101165,
        PRIMARY_LIGHTER_35 = 0x101035,
        PRIMARY_LIGHTER_65 = 0x101065,
        SECONDARY_COLOR_BASE = 0x102000,
        SECONDARY_DARKER_35 = 0x102135,
        SECONDARY_DARKER_65 = 0x102165,
        SECONDARY_LIGHTER_35 = 0x102035,
        SECONDARY_LIGHTER_65 = 0x102065,
    }

}
