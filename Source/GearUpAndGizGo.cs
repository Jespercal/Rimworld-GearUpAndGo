using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.Sound;
using Verse.AI;
using HarmonyLib;
using static System.Collections.Specialized.BitVector32;
using BetterPawnControl;
using System.Reflection;

namespace GearUpAndGo
{
	public class Command_GearUpAndGo : Command
	{
		public Command_GearUpAndGo() : base()
		{
			defaultLabel = "TD.GearAndGo".Translate();
			defaultDesc = "TD.GearAndGoDesc".Translate();
			alsoClickIfOtherInGroupClicked = false;
		}

		public static void Target(string policy = null)
		{
			Find.Targeter.BeginTargeting(new TargetingParameters() { canTargetLocations = true },
				(LocalTargetInfo target) => Go(target, policy));
		}

		public static void Go(LocalTargetInfo target, string policy)
		{
			Log.Message($"GearUpAndGo to {target}, setting {policy}");

			if(Find.Selector.SelectedObjects.Count == 0)
				return;

			Pawn firstPawn = (Pawn)Find.Selector.SelectedObjects.First();

			int currentMapId = firstPawn.Map.uniqueID;

			IEnumerable<Pawn> pawns = Find.Selector.SelectedObjects
				.Where(o => o is Pawn p && p.IsColonistPlayerControlled && p.Map.uniqueID == currentMapId).Cast<Pawn>();

			if (!Event.current.alt)
				firstPawn.Map.GetComponent<GearUpPolicyComp>().Set(pawns.ToList(), policy);


			foreach (Pawn p in Find.Selector.SelectedObjects
				.Where(o => o is Pawn p && p.IsColonistPlayerControlled && p.Map.uniqueID == currentMapId).Cast<Pawn>())
			{
				p.jobs.TryTakeOrderedJob(new Job(GearUpAndGoJobDefOf.GearUpAndGo, target), JobTag.DraftedOrder);
			}
		}

		public static void End()
		{
			Current.Game.CurrentMap.GetComponent<GearUpPolicyComp>().Revert();
		}

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			if (ev.shift && Current.Game.CurrentMap.GetComponent<GearUpPolicyComp>().IsOn())
				End();
			else
				Target();
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				var list = SetBetterPawnControl.PolicyList();
				if (list == null) yield break;
				foreach (string policy in list)
					yield return new FloatMenuOption(policy, () => Target(policy));
			}
		}
	}

	[StaticConstructorOnStartup]
	public class TexGearUpAndGo
	{
		public static readonly Texture2D guagIcon = ContentFinder<Texture2D>.Get("CommandGearUpAndGo");
		public static readonly Texture2D guagIconActive = ContentFinder<Texture2D>.Get("CommandGearUpAndGoActive");
	}

	public class GearUpPolicyComp : MapComponent
	{
		public static GearUpPolicyComp comp;

		public string lastPolicy = "";

		public Dictionary<Pawn, bool> isGearedUp = new Dictionary<Pawn, bool>();

		public GearUpPolicyComp(Map map) : base(map) {
			comp = this;
			if (!BetterPawnControl.AssignManager.policies.Any(e => e.label == "Mixed"))
			{
				var labelId = BetterPawnControl.AssignManager.policies.Last().id + 1;
				BetterPawnControl.AssignManager.policies.Add(new BetterPawnControl.Policy(labelId, "Mixed"));
			}
			BetterPawnControl.AssignManager.SetActivePolicy(this.map.uniqueID, BetterPawnControl.AssignManager.policies.First(e => e.label == "Mixed"));
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref lastPolicy, "lastPolicy", "");

			List<Pawn> keys = new List<Pawn>();
			List<bool> values = new List<bool>();
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (KeyValuePair<Pawn, bool> entry in isGearedUp)
				{
					keys.Add(entry.Key);
					values.Add(entry.Value);
				}
				Scribe_Collections.Look(ref isGearedUp, "isGearedUp", LookMode.Reference, LookMode.Value, ref keys, ref values);
			}

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				Scribe_Collections.Look(ref isGearedUp, "isGearedUp", LookMode.Reference, LookMode.Value, ref keys, ref values);
				if(isGearedUp == null)
				{
					isGearedUp = new Dictionary<Pawn, bool>();
				}
			}
		}
		public void Set(string policy)
		{
			Log.Message($"Found geared:");
			foreach (KeyValuePair<Pawn, bool> pair in isGearedUp)
			{
				Log.Message($"- {pair.Key.Name}: {(pair.Value ? "Yes" : "No")}");
			}

			if (lastPolicy == "")
			{
				lastPolicy = SetBetterPawnControl.CurrentPolicy();
			}
			SetBetterPawnControl.SetSpecificPawnControlPolicy(this.map.mapPawns.FreeColonists, policy ?? Mod.settings.betterPawnControlBattlePolicy);
		}

		public void Set(List<Pawn> pawns, string policy)
		{
			pawns.ForEach(pawn =>
			{
				if (!isGearedUp.ContainsKey(pawn))
					isGearedUp.Add(pawn, true);
				else
					isGearedUp[pawn] = true;
			});

			Log.Message($"Found geared:");
			foreach (KeyValuePair<Pawn,bool> pair in isGearedUp)
			{
				Log.Message($"- {pair.Key.Name}: {(pair.Value ? "Yes" : "No")}");
			}

			if (Mod.settings.betterPawnControlNormalPolicy == "")
			{
				if (lastPolicy == "")
				{
					lastPolicy = SetBetterPawnControl.CurrentPolicy();
				}
				SetBetterPawnControl.SetSpecificPawnControlPolicy(this.map.mapPawns.FreeColonists, policy ?? Mod.settings.betterPawnControlBattlePolicy);
			}
			else
			{
				var mixedPolicy = AssignManager.policies.First(e => e.label == "Mixed");
				var policyObj = AssignManager.policies.First(e => e.label == (policy ?? Mod.settings.betterPawnControlBattlePolicy));

				foreach(Pawn pawn in pawns)
				{
					AssignManager.links.RemoveAll(e => e.colonist.Equals(pawn) && e.mapId == this.map.uniqueID && e.zone == mixedPolicy.id);

					foreach (AssignLink link in AssignManager.links.FindAll(e => e.mapId == pawn.Map.uniqueID))
					{
						if (link.zone != policyObj.id) continue;

						if (link.colonist != null && link.colonist.GetUniqueLoadID().Equals(pawn.GetUniqueLoadID()))
						{
							var newLink = new AssignLink(AssignManager.links.First(e => e.colonist.Equals(pawn) && e.zone == policyObj.id && e.mapId == this.map.uniqueID));
							newLink.zone = mixedPolicy.id;
							AssignManager.links.Add(newLink);

							pawn.outfits.CurrentApparelPolicy = AssignManager.OutfitExits(link.outfit) ? link.outfit : null;
							pawn.drugs.CurrentPolicy = AssignManager.DrugPolicyExits(link.drugPolicy) ? link.drugPolicy : null;
							pawn.foodRestriction.CurrentFoodPolicy = AssignManager.FoodPolicyExists(link.foodPolicy) ? link.foodPolicy : null;
							pawn.reading.CurrentPolicy = AssignManager.ReadingPolicyExits(link.readingPolicy) ? link.readingPolicy : null;
							pawn.playerSettings.hostilityResponse = link.hostilityResponse;

							if (Widget_CombatExtended.CombatExtendedAvailable)
							{
								Widget_CombatExtended.SetLoadoutById(pawn, link.loadoutId);
							}
						}
					}
				}

				AssignManager.SetActivePolicy(this.map.uniqueID, mixedPolicy);
			}
		}

		internal static bool OutfitExits(ApparelPolicy outfit)
		{
			foreach (ApparelPolicy current in Current.Game.outfitDatabase.AllOutfits)
			{
				if (current.Equals(outfit))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool DrugPolicyExits(DrugPolicy drugPolicy)
		{
			foreach (DrugPolicy drug in Current.Game.drugPolicyDatabase.AllPolicies)
			{
				if (drug.Equals(drugPolicy))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool ReadingPolicyExits(ReadingPolicy readingPolicy)
		{
			foreach (ReadingPolicy reading in Current.Game.readingPolicyDatabase.AllReadingPolicies)
			{
				if (reading.Equals(readingPolicy))
				{
					return true;
				}
			}
			return false;
		}

		public void Revert()
		{
			this.map.mapPawns.FreeColonists.ForEach(pawn =>
			{
				if (!isGearedUp.ContainsKey(pawn))
					isGearedUp.Add(pawn, false);
				else
					isGearedUp[pawn] = false;
			});

			Log.Message($"Found geared:");
			foreach (KeyValuePair<Pawn, bool> pair in isGearedUp)
			{
				Log.Message($"- {pair.Key.Name}: {(pair.Value ? "Yes" : "No")}");
			}


			if (Mod.settings.betterPawnControlNormalPolicy == "")
			{
				if (lastPolicy == "") return;

				SetBetterPawnControl.SetSpecificPawnControlPolicy(this.map.mapPawns.FreeColonists, lastPolicy);

				lastPolicy = "";
			}
			else
			{
				var policyObj = AssignManager.policies.First(e => e.label == Mod.settings.betterPawnControlNormalPolicy);
				var mixedPolicy = AssignManager.policies.First(e => e.label == "Mixed");

				foreach (Pawn pawn in this.map.mapPawns.FreeColonists)
				{
					AssignManager.links.RemoveAll(e => e.colonist.Equals(pawn) && e.mapId == this.map.uniqueID && e.zone == mixedPolicy.id);

					foreach (AssignLink link in AssignManager.links.FindAll(e => e.mapId == pawn.Map.uniqueID))
					{
						if (link.zone != policyObj.id) continue;

						if (link.colonist != null && link.colonist.GetUniqueLoadID().Equals(pawn.GetUniqueLoadID()))
						{
							var newLink = new AssignLink(AssignManager.links.First(e => e.colonist.Equals(pawn) && e.zone == policyObj.id && e.mapId == this.map.uniqueID));
							newLink.zone = mixedPolicy.id;
							AssignManager.links.Add(newLink);

							pawn.outfits.CurrentApparelPolicy = AssignManager.OutfitExits(link.outfit) ? link.outfit : null;
							pawn.drugs.CurrentPolicy = AssignManager.DrugPolicyExits(link.drugPolicy) ? link.drugPolicy : null;
							pawn.foodRestriction.CurrentFoodPolicy = AssignManager.FoodPolicyExists(link.foodPolicy) ? link.foodPolicy : null;
							pawn.reading.CurrentPolicy = AssignManager.ReadingPolicyExits(link.readingPolicy) ? link.readingPolicy : null;
							pawn.playerSettings.hostilityResponse = link.hostilityResponse;

							if (Widget_CombatExtended.CombatExtendedAvailable)
							{
								Widget_CombatExtended.SetLoadoutById(pawn, link.loadoutId);
							}
						}
					}
				}

				AssignManager.SetActivePolicy(this.map.uniqueID, mixedPolicy);
			}
		}

		public void Revert( List<Pawn> pawns )
		{
			pawns.ForEach(pawn =>
			{
				if (!isGearedUp.ContainsKey(pawn))
					isGearedUp.Add(pawn, false);
				else
					isGearedUp[pawn] = false;
			});

			Log.Message($"Found geared:");
			foreach (KeyValuePair<Pawn, bool> pair in isGearedUp)
			{
				Log.Message($"- {pair.Key.Name}: {(pair.Value ? "Yes" : "No")}");
			}


			if (Mod.settings.betterPawnControlNormalPolicy == "")
			{
				if (lastPolicy == "") return;

				SetBetterPawnControl.SetSpecificPawnControlPolicy(this.map.mapPawns.FreeColonists, lastPolicy);

				lastPolicy = "";
			}
			else
			{
				var policyObj = AssignManager.policies.First(e => e.label == Mod.settings.betterPawnControlNormalPolicy);
				var mixedPolicy = AssignManager.policies.First(e => e.label == "Mixed");

				foreach (Pawn pawn in pawns)
				{
					AssignManager.links.RemoveAll(e => e.colonist.Equals(pawn) && e.mapId == this.map.uniqueID && e.zone == mixedPolicy.id);

					foreach (AssignLink link in AssignManager.links.FindAll(e => e.mapId == pawn.Map.uniqueID))
					{
						if (link.zone != policyObj.id) continue;

						if (link.colonist != null && link.colonist.GetUniqueLoadID().Equals(pawn.GetUniqueLoadID()))
						{
							var newLink = new AssignLink(AssignManager.links.First(e => e.colonist.Equals(pawn) && e.zone == policyObj.id && e.mapId == this.map.uniqueID));
							newLink.zone = mixedPolicy.id;
							AssignManager.links.Add(newLink);

							pawn.outfits.CurrentApparelPolicy = AssignManager.OutfitExits(link.outfit) ? link.outfit : null;
							pawn.drugs.CurrentPolicy = AssignManager.DrugPolicyExits(link.drugPolicy) ? link.drugPolicy : null;
							pawn.foodRestriction.CurrentFoodPolicy = AssignManager.FoodPolicyExists(link.foodPolicy) ? link.foodPolicy : null;
							pawn.reading.CurrentPolicy = AssignManager.ReadingPolicyExits(link.readingPolicy) ? link.readingPolicy : null;
							pawn.playerSettings.hostilityResponse = link.hostilityResponse;

							if (Widget_CombatExtended.CombatExtendedAvailable)
							{
								Widget_CombatExtended.SetLoadoutById(pawn, link.loadoutId);
							}
						}
					}
				}

				AssignManager.SetActivePolicy(this.map.uniqueID, mixedPolicy);
			}
		}

		public bool IsOn()
		{
			return Mod.settings.betterPawnControlNormalPolicy == "" ? (lastPolicy != "") : (isGearedUp.Any(e => e.Value == true));
		}
	}


	[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
	public static class GearUpAndGizGo
	{
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
		{
			if (__instance.IsColonistPlayerControlled)
			{
				yield return new Command_GearUpAndGo()
				{
					icon = __instance.Map.GetComponent<GearUpPolicyComp>().lastPolicy != "" ? TexGearUpAndGo.guagIconActive : TexGearUpAndGo.guagIcon
				};
			}

			foreach (var r in __result)
				yield return r;
		}
	}

	// backcompat dummy so it doesn't log error loading old saves
	public class CompGearUpAndGizGo : ThingComp { }
}
