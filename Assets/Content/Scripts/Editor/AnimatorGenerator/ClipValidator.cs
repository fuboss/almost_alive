using System.Collections.Generic;
using System.Linq;

namespace Content.Scripts.Editor.AnimatorGenerator {
  public class ClipValidationResult {
    public int Found { get; }
    public int Total { get; }
    public List<string> Missing { get; }
    public List<string> FoundClips { get; }
    public Dictionary<string, List<ClipInfo>> ByCategory { get; }
    public float Progress => Total > 0 ? (Found / (float)Total) * 100f : 0f;

    public ClipValidationResult(int found, int total, List<string> missing, List<string> foundClips, Dictionary<string, List<ClipInfo>> byCategory) {
      Found = found;
      Total = total;
      Missing = missing;
      FoundClips = foundClips;
      ByCategory = byCategory;
    }
  }

  public class ClipInfo {
    public string Folder { get; }
    public string Name { get; }
    public bool Exists { get; }
    public string UsedIn { get; }

    public ClipInfo(string folder, string name, bool exists, string usedIn) {
      Folder = folder;
      Name = name;
      Exists = exists;
      UsedIn = usedIn;
    }

    public override string ToString() => $"{Folder}/{Name}";
  }

  public class ClipValidator {
    private readonly AnimationClipProvider _clipProvider;

    // (folder, clip, category, usedIn)
    private static readonly (string folder, string clip, string category, string usedIn)[] AllClips = {
      // === General ===
      ("General", "Idle_A", "General", "Base Layer - Idle"),
      ("General", "Idle_B", "General", "Base Layer - Idle variant"),
      ("General", "Death_A", "General", "Base Layer - Death"),
      ("General", "Death_A_Pose", "General", "Base Layer - Death Pose"),
      ("General", "Death_B", "General", "Base Layer - Death variant"),
      ("General", "Death_B_Pose", "General", "Base Layer - Death Pose variant"),
      ("General", "Hit_A", "General", "Additive Layer - Hit reaction"),
      ("General", "Hit_B", "General", "Additive Layer - Hit variant"),
      ("General", "Interact", "General", "UpperBody Layer - Interact"),
      ("General", "Use_Item", "General", "UpperBody Layer - Use Item"),
      ("General", "Throw", "General", "UpperBody Layer - Throw"),
      ("General", "Spawn_Air", "General", "Base Layer - Spawn"),
      ("General", "Spawn_Ground", "General", "Base Layer - Spawn"),

      // === Movement Basic ===
      ("MovementBasic", "Walking_A", "Movement", "Base Layer - Locomotion BlendTree"),
      ("MovementBasic", "Walking_B", "Movement", "Base Layer - Locomotion variant"),
      ("MovementBasic", "Walking_C", "Movement", "Base Layer - Locomotion variant"),
      ("MovementBasic", "Running_A", "Movement", "Base Layer - Locomotion BlendTree"),
      ("MovementBasic", "Running_B", "Movement", "Base Layer - Locomotion variant"),
      ("MovementBasic", "Jump_Start", "Movement", "Base Layer - Jump"),
      ("MovementBasic", "Jump_Idle", "Movement", "Base Layer - Jump Air"),
      ("MovementBasic", "Jump_Land", "Movement", "Base Layer - Jump Land"),
      ("MovementBasic", "Jump_Full_Short", "Movement", "Base Layer - Jump Full"),
      ("MovementBasic", "Jump_Full_Long", "Movement", "Base Layer - Jump Full"),

      // === Movement Advanced ===
      ("MovementAdvanced", "Walking_Backwards", "Movement", "Base Layer - Locomotion BlendTree"),
      ("MovementAdvanced", "Running_Strafe_Left", "Movement", "Base Layer - Locomotion BlendTree"),
      ("MovementAdvanced", "Running_Strafe_Right", "Movement", "Base Layer - Locomotion BlendTree"),
      ("MovementAdvanced", "Crouching", "Movement", "Base Layer - Crouch BlendTree"),
      ("MovementAdvanced", "Sneaking", "Movement", "Base Layer - Crouch BlendTree"),
      ("MovementAdvanced", "Crawling", "Movement", "Base Layer - Crouch BlendTree"),
      ("MovementAdvanced", "Dodge_Forward", "Movement", "Base Layer - Dodge BlendTree"),
      ("MovementAdvanced", "Dodge_Backward", "Movement", "Base Layer - Dodge BlendTree"),
      ("MovementAdvanced", "Dodge_Left", "Movement", "Base Layer - Dodge BlendTree"),
      ("MovementAdvanced", "Dodge_Right", "Movement", "Base Layer - Dodge BlendTree"),
      ("MovementAdvanced", "Running_HoldingBow", "Movement", "Future - Locomotion with weapon"),
      ("MovementAdvanced", "Running_HoldingRifle", "Movement", "Future - Locomotion with weapon"),

      // === Combat Melee ===
      ("CombatMelee", "Melee_Unarmed_Idle", "Combat", "Combat Layer - Unarmed Idle"),
      ("CombatMelee", "Melee_Unarmed_Attack_Punch_A", "Combat", "Combat Layer - Unarmed Attack"),
      ("CombatMelee", "Melee_Unarmed_Attack_Kick", "Combat", "Combat Layer - Unarmed Attack"),
      ("CombatMelee", "Melee_1H_Attack_Chop", "Combat", "Combat Layer - 1H Attack"),
      ("CombatMelee", "Melee_1H_Attack_Jump_Chop", "Combat", "Combat Layer - 1H Jump Attack"),
      ("CombatMelee", "Melee_1H_Attack_Slice_Diagonal", "Combat", "Combat Layer - 1H Attack"),
      ("CombatMelee", "Melee_1H_Attack_Slice_Horizontal", "Combat", "Combat Layer - 1H Attack"),
      ("CombatMelee", "Melee_1H_Attack_Stab", "Combat", "Combat Layer - 1H Attack"),
      ("CombatMelee", "Melee_2H_Idle", "Combat", "Combat Layer - 2H Idle"),
      ("CombatMelee", "Melee_2H_Attack_Chop", "Combat", "Combat Layer - 2H Attack"),
      ("CombatMelee", "Melee_2H_Attack_Slice", "Combat", "Combat Layer - 2H Attack"),
      ("CombatMelee", "Melee_2H_Attack_Spin", "Combat", "Combat Layer - 2H Attack"),
      ("CombatMelee", "Melee_2H_Attack_Spinning", "Combat", "Combat Layer - 2H Attack Loop"),
      ("CombatMelee", "Melee_2H_Attack_Stab", "Combat", "Combat Layer - 2H Attack"),
      ("CombatMelee", "Melee_Dualwield_Attack_Chop", "Combat", "Combat Layer - Dual Attack"),
      ("CombatMelee", "Melee_Dualwield_Attack_Slice", "Combat", "Combat Layer - Dual Attack"),
      ("CombatMelee", "Melee_Dualwield_Attack_Stab", "Combat", "Combat Layer - Dual Attack"),
      ("CombatMelee", "Melee_Block", "Combat", "Combat Layer - Block Enter"),
      ("CombatMelee", "Melee_Blocking", "Combat", "Combat Layer - Blocking Loop"),
      ("CombatMelee", "Melee_Block_Attack", "Combat", "Combat Layer - Counter Attack"),
      ("CombatMelee", "Melee_Block_Hit", "Combat", "Combat Layer - Block Hit"),

      // === Combat Ranged ===
      ("CombatRanged", "Ranged_1H_Aiming", "Combat", "Combat Layer - Pistol Aim"),
      ("CombatRanged", "Ranged_1H_Reload", "Combat", "Combat Layer - Pistol Reload"),
      ("CombatRanged", "Ranged_1H_Shoot", "Combat", "Combat Layer - Pistol Shoot"),
      ("CombatRanged", "Ranged_1H_Shooting", "Combat", "Combat Layer - Pistol Shooting Loop"),
      ("CombatRanged", "Ranged_2H_Aiming", "Combat", "Combat Layer - Rifle Aim"),
      ("CombatRanged", "Ranged_2H_Reload", "Combat", "Combat Layer - Rifle Reload"),
      ("CombatRanged", "Ranged_2H_Shoot", "Combat", "Combat Layer - Rifle Shoot"),
      ("CombatRanged", "Ranged_2H_Shooting", "Combat", "Combat Layer - Rifle Shooting Loop"),
      ("CombatRanged", "Ranged_Bow_Idle", "Combat", "Combat Layer - Bow Idle"),
      ("CombatRanged", "Ranged_Bow_Aiming_Idle", "Combat", "Combat Layer - Bow Aim"),
      ("CombatRanged", "Ranged_Bow_Draw", "Combat", "Combat Layer - Bow Draw"),
      ("CombatRanged", "Ranged_Bow_Draw_Up", "Combat", "Combat Layer - Bow Draw Up"),
      ("CombatRanged", "Ranged_Bow_Release", "Combat", "Combat Layer - Bow Release"),
      ("CombatRanged", "Ranged_Bow_Release_Up", "Combat", "Combat Layer - Bow Release Up"),
      ("CombatRanged", "Ranged_Magic_Raise", "Combat", "Combat Layer - Magic Raise"),
      ("CombatRanged", "Ranged_Magic_Shoot", "Combat", "Combat Layer - Magic Shoot"),
      ("CombatRanged", "Ranged_Magic_Spellcasting", "Combat", "Combat Layer - Spellcasting"),
      ("CombatRanged", "Ranged_Magic_Spellcasting_Long", "Combat", "Combat Layer - Long Spellcast"),
      ("CombatRanged", "Ranged_Magic_Summon", "Combat", "Combat Layer - Summon"),

      // === Tools ===
      ("Tools", "Chop", "Tools", "UpperBody Layer - Single Chop"),
      ("Tools", "Chopping", "Tools", "UpperBody Layer - Work BlendTree (Axe)"),
      ("Tools", "Dig", "Tools", "UpperBody Layer - Single Dig"),
      ("Tools", "Digging", "Tools", "UpperBody Layer - Work BlendTree (Shovel)"),
      ("Tools", "Hammer", "Tools", "UpperBody Layer - Single Hammer"),
      ("Tools", "Hammering", "Tools", "UpperBody Layer - Work BlendTree (Hammer)"),
      ("Tools", "Pickaxe", "Tools", "UpperBody Layer - Single Pickaxe"),
      ("Tools", "Pickaxing", "Tools", "UpperBody Layer - Work BlendTree (Pickaxe)"),
      ("Tools", "Saw", "Tools", "UpperBody Layer - Single Saw"),
      ("Tools", "Sawing", "Tools", "UpperBody Layer - Work BlendTree (Saw)"),
      ("Tools", "Lockpick", "Tools", "UpperBody Layer - Single Lockpick"),
      ("Tools", "Lockpicking", "Tools", "UpperBody Layer - Lockpicking Loop"),
      ("Tools", "Fishing_Cast", "Tools", "UpperBody Layer - Fishing Cast"),
      ("Tools", "Fishing_Idle", "Tools", "UpperBody Layer - Work BlendTree (FishingRod)"),
      ("Tools", "Fishing_Bite", "Tools", "UpperBody Layer - Fishing Bite"),
      ("Tools", "Fishing_Reeling", "Tools", "UpperBody Layer - Fishing Reel"),
      ("Tools", "Fishing_Struggling", "Tools", "UpperBody Layer - Fishing Struggle"),
      ("Tools", "Fishing_Tug", "Tools", "UpperBody Layer - Fishing Tug"),
      ("Tools", "Fishing_Catch", "Tools", "UpperBody Layer - Fishing Catch"),
      ("Tools", "Holding_A", "Tools", "UpperBody Layer - Holding variant"),
      ("Tools", "Holding_B", "Tools", "UpperBody Layer - Holding variant"),
      ("Tools", "Holding_C", "Tools", "UpperBody Layer - Holding variant"),
      ("Tools", "Work_A", "Tools", "UpperBody Layer - Generic Work"),
      ("Tools", "Work_B", "Tools", "UpperBody Layer - Generic Work"),
      ("Tools", "Work_C", "Tools", "UpperBody Layer - Generic Work"),
      ("Tools", "Working_A", "Tools", "UpperBody Layer - Working Loop"),
      ("Tools", "Working_B", "Tools", "UpperBody Layer - Working Loop"),
      ("Tools", "Working_C", "Tools", "UpperBody Layer - Working Loop"),

      // === Simulation ===
      ("Simulation", "Waving", "Social", "UpperBody Layer - Wave"),
      ("Simulation", "Cheering", "Social", "UpperBody Layer - Cheer"),
      ("Simulation", "Lie_Down", "Social", "Base Layer - Lie Down"),
      ("Simulation", "Lie_Idle", "Social", "Base Layer - Lie Idle"),
      ("Simulation", "Lie_StandUp", "Social", "Base Layer - Lie Stand Up"),
      ("Simulation", "Sit_Chair_Down", "Social", "Base Layer - Sit Chair"),
      ("Simulation", "Sit_Chair_Idle", "Social", "Base Layer - Sit Chair Idle"),
      ("Simulation", "Sit_Chair_StandUp", "Social", "Base Layer - Sit Chair Stand"),
      ("Simulation", "Sit_Floor_Down", "Social", "Base Layer - Sit Floor"),
      ("Simulation", "Sit_Floor_Idle", "Social", "Base Layer - Sit Floor Idle"),
      ("Simulation", "Sit_Floor_StandUp", "Social", "Base Layer - Sit Floor Stand"),
      ("Simulation", "Push_Ups", "Social", "Base Layer - Push Ups"),
      ("Simulation", "Sit_Ups", "Social", "Base Layer - Sit Ups"),
    };

    public static IReadOnlyList<(string folder, string clip, string category, string usedIn)> GetAllClipDefinitions() => AllClips;

    public ClipValidator(AnimationClipProvider clipProvider) {
      _clipProvider = clipProvider;
    }

    public ClipValidationResult Validate() {
      var found = 0;
      var missing = new List<string>();
      var foundClips = new List<string>();
      var byCategory = new Dictionary<string, List<ClipInfo>>();

      foreach (var (folder, clip, category, usedIn) in AllClips) {
        var exists = _clipProvider.Exists(folder, clip);
        var info = new ClipInfo(folder, clip, exists, usedIn);

        if (!byCategory.ContainsKey(category)) {
          byCategory[category] = new List<ClipInfo>();
        }
        byCategory[category].Add(info);

        if (exists) {
          found++;
          foundClips.Add($"{folder}/{clip}");
        } else {
          missing.Add($"{folder}/{clip}");
        }
      }

      return new ClipValidationResult(found, AllClips.Length, missing, foundClips, byCategory);
    }
  }
}

