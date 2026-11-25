using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace GamblingMod;

internal static class CharacterRegistrationPatch
{
    private static bool _installed;
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["VisualsPath"] = "scenes/creature_visuals/ironclad.tscn",
        ["IconTexturePath"] = "images/ui/top_panel/character_icon_ironclad.png",
        ["IconOutlineTexturePath"] = "images/ui/top_panel/character_icon_ironclad_outline.png",
        ["EnergyCounterPath"] = "scenes/combat/energy_counters/ironclad_energy_counter.tscn",
        ["MerchantAnimPath"] = "scenes/merchant/characters/ironclad_merchant.tscn",
        ["RestSiteAnimPath"] = "scenes/rest_site/characters/ironclad_rest_site.tscn",
        ["ArmPointingTexturePath"] = "images/ui/hands/multiplayer_hand_ironclad_point.png",
        ["ArmRockTexturePath"] = "images/ui/hands/multiplayer_hand_ironclad_rock.png",
        ["ArmPaperTexturePath"] = "images/ui/hands/multiplayer_hand_ironclad_paper.png",
        ["ArmScissorsTexturePath"] = "images/ui/hands/multiplayer_hand_ironclad_scissors.png",
        ["CharacterSelectBg"] = "scenes/screens/char_select/char_select_bg_ironclad.tscn",
        ["CharacterSelectTransitionPath"] = "materials/transitions/ironclad_transition_mat.tres",
        ["TrailPath"] = "scenes/vfx/card_trail_ironclad.tscn"
    };

    public static void Install()
    {
        if (_installed) return;
        var harmony = new Harmony("GamblingMod.TalentedGambler");
        // ModelDb normally discovers mod subtypes itself. Inject as a fallback
        // before InitIds so canonical IDs can be assigned by the game.
        harmony.Patch(AccessTools.Method(typeof(ModelDb), nameof(ModelDb.InitIds)),
            prefix: PatchMethod(nameof(BeforeInitIds)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters)),
            postfix: PatchMethod(nameof(AppendCharacter)));
        foreach (string property in Paths.Keys)
            PatchGetter(harmony, property, nameof(ResourcePathPrefix));
        foreach (string property in new[] { "AttackSfx", "CastSfx", "PowerUpSfx" })
            PatchGetter(harmony, property, nameof(SoundPrefix));

        // Also intercept the callers of private resource getters to avoid
        // relying on Harmony patches for tiny, potentially inlined methods.
        PatchGetter(harmony, "AssetPaths", nameof(RunAssetsPrefix));
        PatchGetter(harmony, "AssetPathsCharacterSelect", nameof(SelectAssetsPrefix));
        harmony.Patch(AccessTools.Method(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals)),
            prefix: PatchMethod(nameof(CreateVisualsPrefix)));
        PatchGetter(harmony, "IconTexture", nameof(TexturePrefix));
        PatchGetter(harmony, "IconOutlineTexture", nameof(TexturePrefix));
        PatchGetter(harmony, "ArmPointingTexture", nameof(TexturePrefix));
        PatchGetter(harmony, "ArmRockTexture", nameof(TexturePrefix));
        PatchGetter(harmony, "ArmPaperTexture", nameof(TexturePrefix));
        PatchGetter(harmony, "ArmScissorsTexture", nameof(TexturePrefix));

        GamblerCharacterSelectArt.Install(harmony);
        _installed = true;
        GD.Print("TalentedGambler: character patches installed.");
    }

    private static HarmonyMethod PatchMethod(string method) =>
        new HarmonyMethod(AccessTools.Method(typeof(CharacterRegistrationPatch), method));

    private static void PatchGetter(Harmony harmony, string property, string prefix)
    {
        var getter = AccessTools.PropertyGetter(typeof(CharacterModel), property)
            ?? throw new MissingMethodException(typeof(CharacterModel).FullName, "get_" + property);
        harmony.Patch(getter, prefix: PatchMethod(prefix));
    }

    private static void BeforeInitIds()
    {
        ModelDb.Inject(typeof(TalentedGambler));
        ModelDb.Inject(typeof(TalentedGamblerCardPool));
    }

    private static void AppendCharacter(ref IEnumerable<CharacterModel> __result)
    {
        if (!ModelDb.Contains(typeof(TalentedGambler))) return;
        var characters = __result.ToList();
        if (!characters.Any(c => c is TalentedGambler))
            characters.Add(ModelDb.Character<TalentedGambler>());
        __result = characters;
    }

    private static bool ResourcePathPrefix(CharacterModel __instance, MethodBase __originalMethod, ref string __result)
    {
        if (__instance is not TalentedGambler) return true;
        __result = GamblerAssets.Resolve(Paths[__originalMethod.Name.Substring(4)]);
        return false;
    }

    private static bool SoundPrefix(CharacterModel __instance, MethodBase __originalMethod, ref string __result)
    {
        if (__instance is not TalentedGambler) return true;
        var ironclad = ModelDb.Character<Ironclad>();
        __result = __originalMethod.Name == "get_AttackSfx" ? ironclad.AttackSfx : ironclad.CastSfx;
        return false;
    }

    private static bool RunAssetsPrefix(CharacterModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is not TalentedGambler) return true;
        __result = new[] { "VisualsPath", "IconTexturePath", "EnergyCounterPath", "RestSiteAnimPath", "MerchantAnimPath", "CharacterSelectTransitionPath", "TrailPath" }
            .Select(p => GamblerAssets.Resolve(Paths[p]))
            .Concat(new[] { GamblerAssets.Resolve("scenes/ui/character_icons/ironclad_icon.tscn"), GamblerAssets.Resolve("images/packed/map/icons/map_marker_ironclad.png") }).ToArray();
        return false;
    }

    private static bool SelectAssetsPrefix(CharacterModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is not TalentedGambler) return true;
        __result = new[] { GamblerAssets.Resolve(Paths["CharacterSelectBg"]), GamblerAssets.Resolve("images/packed/character_select/char_select_ironclad.png"), GamblerAssets.Resolve(Paths["IconTexturePath"]), GamblerAssets.Resolve("images/packed/character_select/char_select_ironclad_locked.png"), GamblerAssets.Resolve(Paths["CharacterSelectTransitionPath"]) };
        return false;
    }

    private static bool CreateVisualsPrefix(CharacterModel __instance, ref NCreatureVisuals __result)
    {
        if (__instance is not TalentedGambler) return true;
        __result = PreloadManager.Cache.GetScene(GamblerAssets.Resolve(Paths["VisualsPath"]))
            .Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
        GamblerRuntimeArt.Apply(__result);
        return false;
    }

    private static bool TexturePrefix(CharacterModel __instance, MethodBase __originalMethod, ref Texture2D __result)
    {
        if (__instance is not TalentedGambler) return true;
        __result = PreloadManager.Cache.GetTexture2D(GamblerAssets.Resolve(Paths[__originalMethod.Name.Substring(4) + "Path"]));
        return false;
    }
}
