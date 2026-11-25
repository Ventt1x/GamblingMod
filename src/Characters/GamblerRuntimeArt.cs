using System;
using System.IO;
using Godot;

namespace GamblingMod;

internal static class GamblerRuntimeArt
{
    // Change to false to disable just the combat reskin.
    public static bool Enabled = true;
    private static Resource? _customData;
    private static bool _reported;
    private static readonly string[] Pages =
    {
        "ironclad.png", "ironclad_2.png", "ironclad_3.png", "ironclad_4.png"
    };

    public static void Apply(Node visuals)
    {
        if (!Enabled) return;
        try
        {
            // The caller only invokes this for TalentedGambler. The vanilla
            // scene is instantiated already, but has not entered the tree.
            if (visuals.IsInsideTree())
                throw new InvalidOperationException("Visuals are already in the scene tree.");
            Node? sprite = visuals.GetNodeOrNull<Node>("Visuals");
            if (sprite == null || !sprite.HasMethod("get_skeleton_data_res") ||
                !sprite.HasMethod("set_skeleton_data_res"))
                throw new InvalidOperationException("Spine Visuals node not found.");

            var original = sprite.Call("get_skeleton_data_res").AsGodotObject() as Resource
                ?? throw new InvalidOperationException("Original skeleton resource unavailable.");
            _customData ??= CreateData(original);
            // Only the new sprite receives the new resource. Never edit the
            // shared original skeleton resource or its atlas in place.
            sprite.Call("set_skeleton_data_res", _customData);
            if (!_reported)
            {
                GD.Print("TalentedGambler RuntimeArt: combat atlas applied.");
                _reported = true;
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning("TalentedGambler RuntimeArt: using original art. " + ex.Message);
        }
    }

    private static Resource CreateData(Resource original)
    {
        string directory = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!,
            "mods", "GamblingMod", "RuntimeArt", "combat");
        // The installer also writes the configured path, for game layouts
        // where the executable does not reside in the game root.
        if (!File.Exists(Path.Combine(directory, "ironclad.atlas")))
            directory = @"S:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\GamblingMod\RuntimeArt\combat";
        string atlasPath = Path.Combine(directory, "ironclad.atlas").Replace('\\', '/');
        if (!File.Exists(atlasPath)) throw new FileNotFoundException(atlasPath);
        foreach (string page in Pages)
            if (!File.Exists(Path.Combine(directory, page)))
                throw new FileNotFoundException(page);

        if (!ClassDB.ClassExists("SpineAtlasResource") ||
            !ClassDB.ClassExists("SpineSkeletonDataResource"))
            throw new InvalidOperationException("Spine runtime classes not available.");

        var atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Could not construct SpineAtlasResource.");
        int error = atlas.Call("load_from_atlas_file", atlasPath).AsInt32();
        if (error != 0) throw new InvalidOperationException("Atlas load error " + error);

        var originalAtlas = original.Call("get_atlas_res").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Original atlas missing.");
        var oldTextures = originalAtlas.Call("get_textures").AsGodotArray();
        var newTextures = atlas.Call("get_textures").AsGodotArray();
        if (oldTextures.Count != newTextures.Count || newTextures.Count != Pages.Length)
            throw new InvalidOperationException("Atlas page count differs from original.");
        for (int i = 0; i < oldTextures.Count; i++)
        {
            var before = oldTextures[i].AsGodotObject() as Texture2D;
            var after = newTextures[i].AsGodotObject() as Texture2D;
            if (before == null || after == null || before.GetSize() != after.GetSize())
                throw new InvalidOperationException("Atlas page dimensions differ at index " + i);
        }

        var data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Could not construct skeleton data.");
        // Use the ORIGINAL binary skeleton and the ORIGINAL animation mixes.
        // Only the atlas (same layout, different PNGs) is newly loaded.
        data.Call("set_skeleton_file_res", original.Call("get_skeleton_file_res"));
        data.Call("set_atlas_res", atlas);
        data.Call("set_default_mix", original.Call("get_default_mix"));
        data.Call("set_animation_mixes", original.Call("get_animation_mixes"));
        if (!data.Call("is_skeleton_data_loaded").AsBool())
            throw new InvalidOperationException("Spine skeleton could not be loaded with new atlas.");
        return data;
    }
}