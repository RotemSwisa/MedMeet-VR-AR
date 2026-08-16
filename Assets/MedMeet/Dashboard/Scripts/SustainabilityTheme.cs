using UnityEngine;

/// <summary>
/// Centralised colour + size tokens for the Sustainability Showcase.
/// Mirrors the CSS variables in the Claude Design styles.css so every
/// canvas, panel and label stays visually consistent.
/// </summary>
public static class SustainabilityTheme
{
    // Canvas reference size — every showcase canvas uses this so the three
    // screens are visually identical in scale.
    public const float CanvasW = 1920f;
    public const float CanvasH = 1080f;

    // ── Dark surfaces ───────────────────────────────────────────────────────
    public static readonly Color Bg0        = new Color(0.039f, 0.094f, 0.125f, 1f);   // #0a1820
    public static readonly Color Bg1        = new Color(0.055f, 0.133f, 0.188f, 1f);
    public static readonly Color Bg2        = new Color(0.070f, 0.166f, 0.227f, 1f);
    public static readonly Color Card       = new Color(0.078f, 0.172f, 0.235f, 0.92f);
    public static readonly Color CardSolid  = new Color(0.066f, 0.149f, 0.204f, 1f);
    public static readonly Color CardSoft   = new Color(0.110f, 0.219f, 0.298f, 0.85f);
    public static readonly Color Line       = new Color(0.549f, 0.745f, 0.823f, 0.16f);
    public static readonly Color LineSoft   = new Color(0.549f, 0.745f, 0.823f, 0.08f);

    // ── Text ────────────────────────────────────────────────────────────────
    public static readonly Color Ink        = new Color(0.917f, 0.957f, 0.969f, 1f);   // #eaf4f7
    public static readonly Color InkSoft    = new Color(0.661f, 0.760f, 0.800f, 1f);
    public static readonly Color InkFaint   = new Color(0.435f, 0.549f, 0.600f, 1f);

    // ── Brand accents ───────────────────────────────────────────────────────
    public static readonly Color Teal       = new Color(0.219f, 0.839f, 0.812f, 1f);   // #38d6cf
    public static readonly Color TealSoft   = new Color(0.498f, 0.901f, 0.878f, 1f);   // #7fe6e0
    public static readonly Color TealWash   = new Color(0.219f, 0.839f, 0.812f, 0.14f);
    public static readonly Color TealDeep   = new Color(0.168f, 0.713f, 0.690f, 1f);

    public static readonly Color Mint       = new Color(0.372f, 0.878f, 0.659f, 1f);   // #5fe0a8
    public static readonly Color MintSoft   = new Color(0.592f, 0.925f, 0.776f, 1f);
    public static readonly Color MintWash   = new Color(0.372f, 0.878f, 0.659f, 0.14f);

    public static readonly Color Sky        = new Color(0.474f, 0.725f, 1.000f, 1f);   // #79b9ff
    public static readonly Color SkyWash    = new Color(0.474f, 0.725f, 1.000f, 0.14f);

    public static readonly Color Clay       = new Color(0.941f, 0.690f, 0.447f, 1f);   // #f0b072
    public static readonly Color ClayWash   = new Color(0.941f, 0.690f, 0.447f, 0.14f);

    // ── Panel tint helpers ──────────────────────────────────────────────────
    public static Color Tint(Color c, float a) => new Color(c.r, c.g, c.b, a);

    // ── Sprite paths (where the user already stored them) ───────────────────
    public const string PathLogo     = "Assets/MedMeet/Dashboard/Sprites/logo.png";
    public const string PathGlobe    = "Assets/MedMeet/Dashboard/Sprites/globe.png";
    public const string PathAirplane = "Assets/MedMeet/Dashboard/Sprites/airplane.png";
    public const string PathBgDark   = "Assets/MedMeet/Dashboard/Sprites/bg-dark.png";

    // ── Icons (whitened by IconWhitenTool, tinted at runtime via Image.color)
    public const string IconsFolder = "Assets/MedMeet/Dashboard/Sprites/icons/";
    public const string IconTrash   = IconsFolder + "trash.png";
    public const string IconUsers   = IconsFolder + "users.png";
    public const string IconPin     = IconsFolder + "pin.png";
    public const string IconGloves  = IconsFolder + "gloves.png";
    public const string IconCar     = IconsFolder + "car.png";
    public const string IconDrop    = IconsFolder + "drop.png";
    public const string IconSpark   = IconsFolder + "spark.png";
    public const string IconRoute   = IconsFolder + "route.png";
    public const string IconRoad    = IconsFolder + "road.png";
    public const string IconClock   = IconsFolder + "clock.png";
    public const string IconLeaf    = IconsFolder + "leaf.png";
}
