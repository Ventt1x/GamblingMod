using System.Collections.Generic;
using Godot;

namespace GamblingMod;

internal static class GamblerAssets
{
    public const string Root =
        "res://GamblingMod/Assets/Characters/TalentedGambler/";

    public static bool UseCopiedArt = false;
    public static bool UseCustomSelectionIcons = true;

    private static readonly HashSet<string> Warned = new();

    public static string Resolve(string relative)
    {
        string custom = Root + relative;

        bool useCustom =
            UseCopiedArt ||
            (
                UseCustomSelectionIcons &&
                relative.StartsWith(
                    "images/packed/character_select/"
                )
            );

        if (useCustom && ResourceLoader.Exists(custom))
            return custom;

        if (useCustom && Warned.Add(relative))
            GD.PushWarning(
                "Missing custom resource: " + custom
            );

        return "res://" + relative;
    }
}