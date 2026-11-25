using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace GamblingMod;

internal static class GamblerCharacterSelectArt
{
    public static bool Enabled = true;
    private static Resource? _customData;
    private static bool _reported;
    private const string AtlasName = "characterselect_ironclad.atlas";
    private const string PageName = "characterselect_ironclad.png";

    public static void Install(Harmony harmony)
    {
        var patch = new HarmonyMethod(AccessTools.Method(typeof(GamblerCharacterSelectArt), nameof(Transpile)));
        TryPatch(harmony, AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter)), patch);
        var random = AccessTools.Method(typeof(NCharacterSelectScreen), "DoRandomCharacterChange");
        var stateMachine = random?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        if (stateMachine != null)
            TryPatch(harmony, AccessTools.Method(stateMachine, "MoveNext"), patch);
        else
            GD.PushWarning("TalentedGambler SelectArt: random selection state machine not found.");
    }

    private static void TryPatch(Harmony harmony, MethodBase? method, HarmonyMethod patch)
    {
        if (method == null) return;
        try { harmony.Patch(method, transpiler: patch); }
        catch (Exception ex) { GD.PushWarning("TalentedGambler SelectArt: patch skipped: " + ex.Message); }
    }

    // Save the character used for the original background path, then pass the
    // newly instantiated Control through Apply before the game adds it to the tree.
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        var getter = AccessTools.PropertyGetter(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg));
        var getters = Enumerable.Range(0, list.Count).Where(i => Equals(list[i].operand, getter)).ToArray();
        if (getters.Length != 1) { GD.PushWarning("TalentedGambler SelectArt: background getter pattern changed."); return list; }
        int start = getters[0];
        int end = -1;
        for (int i = start + 1; i < Math.Min(start + 12, list.Count); i++)
        {
            if (list[i].operand is MethodInfo m && m.Name == "Instantiate" && m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 && m.GetGenericArguments()[0] == typeof(Control) &&
                m.DeclaringType == typeof(PackedScene)) { end = i; break; }
        }
        if (end < 0 || list[start].blocks.Count != 0 || list[end].blocks.Count != 0)
        { GD.PushWarning("TalentedGambler SelectArt: scene instantiation pattern changed."); return list; }
        var character = generator.DeclareLocal(typeof(CharacterModel));
        var dup = new CodeInstruction(OpCodes.Dup);
        dup.labels.AddRange(list[start].labels);
        list[start].labels.Clear();
        list.InsertRange(end + 1, new[] {
            new CodeInstruction(OpCodes.Ldloc, character),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamblerCharacterSelectArt), nameof(Apply)))
        });
        list.InsertRange(start, new[] { dup, new CodeInstruction(OpCodes.Stloc, character) });
        return list;
    }

    private static Control Apply(Control background, CharacterModel character)
    {
        if (!Enabled || character is not TalentedGambler) return background;
        try
        {
            if (background.IsInsideTree()) throw new InvalidOperationException("Background already in tree.");
            var sprites = FindSpineSprites(background).ToList();
            // The original selection atlas has exactly one page. Select the sprite
            // by its original skeleton resource path, not its node name.
            var matches = sprites.Select(node => (node, data: node.Call("get_skeleton_data_res").AsGodotObject() as Resource))
                .Where(x => x.data != null && x.data.ResourcePath.Contains("characterselect_ironclad_skel_data", StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0 && sprites.Count == 1)
            {
                var node = sprites[0];
                var data = node.Call("get_skeleton_data_res").AsGodotObject() as Resource;
                if (data != null) matches.Add((node, data));
            }
            if (matches.Count != 1) throw new InvalidOperationException("Expected one selection SpineSprite; found " + matches.Count);
            var match = matches[0];
            _customData ??= CreateData(match.data!);
            match.node.Call("set_skeleton_data_res", _customData);
            if (!_reported) { GD.Print("TalentedGambler SelectArt: character selection atlas applied."); _reported = true; }
        }
        catch (Exception ex) { GD.PushWarning("TalentedGambler SelectArt: using original art. " + ex.Message); }
        return background;
    }

    private static IEnumerable<Node> FindSpineSprites(Node root)
    {
        if (root.HasMethod("get_skeleton_data_res") && root.HasMethod("set_skeleton_data_res")) yield return root;
        foreach (Node child in root.GetChildren())
            foreach (Node sprite in FindSpineSprites(child)) yield return sprite;
    }

    private static Resource CreateData(Resource original)
    {
        string directory = Path.Combine(Path.GetDirectoryName(typeof(GamblerCharacterSelectArt).Assembly.Location)!, "RuntimeArt", "character_select");
        if (!File.Exists(Path.Combine(directory, AtlasName)))
            directory = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mods", "GamblingMod", "RuntimeArt", "character_select");
        string atlasPath = Path.Combine(directory, AtlasName).Replace('\\', '/');
        if (!File.Exists(atlasPath) || !File.Exists(Path.Combine(directory, PageName)))
            throw new FileNotFoundException("Selection atlas or PNG missing: " + directory);
        if (!ClassDB.ClassExists("SpineAtlasResource") || !ClassDB.ClassExists("SpineSkeletonDataResource"))
            throw new InvalidOperationException("Spine runtime not available.");
        var atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Atlas creation failed.");
        int error = atlas.Call("load_from_atlas_file", atlasPath).AsInt32();
        if (error != 0) throw new InvalidOperationException("Atlas load error " + error);
        var originalAtlas = original.Call("get_atlas_res").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Original atlas missing.");
        var oldTextures = originalAtlas.Call("get_textures").AsGodotArray();
        var newTextures = atlas.Call("get_textures").AsGodotArray();
        if (oldTextures.Count != 1 || newTextures.Count != 1)
            throw new InvalidOperationException("Selection atlas page count changed.");
        var before = oldTextures[0].AsGodotObject() as Texture2D;
        var after = newTextures[0].AsGodotObject() as Texture2D;
        if (before == null || after == null || before.GetSize() != after.GetSize())
            throw new InvalidOperationException("Selection texture dimensions differ from original.");
        var data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject() as Resource
            ?? throw new InvalidOperationException("Skeleton resource creation failed.");
        data.Call("set_skeleton_file_res", original.Call("get_skeleton_file_res"));
        data.Call("set_atlas_res", atlas);
        data.Call("set_default_mix", original.Call("get_default_mix"));
        data.Call("set_animation_mixes", original.Call("get_animation_mixes"));
        if (!data.Call("is_skeleton_data_loaded").AsBool())
            throw new InvalidOperationException("Selection skeleton could not be loaded.");
        return data;
    }
}