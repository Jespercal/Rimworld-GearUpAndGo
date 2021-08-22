using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.Sound;
using Verse.AI;

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

			if (!Event.current.alt && Settings.Get().useBetterPawnControl)
				Current.Game.GetComponent<GearUpPolicyComp>().Set(policy);

			foreach (Pawn p in Find.Selector.SelectedObjects
				.Where(o => o is Pawn p && p.IsColonistPlayerControlled).Cast<Pawn>())
			{
				if (!Event.current.alt && !Settings.Get().useBetterPawnControl)
					Current.Game.GetComponent<GearUpPolicyComp>().Set(policy, p);

				p.jobs.TryTakeOrderedJob(new Job(GearUpAndGoJobDefOf.GearUpAndGo, target), JobTag.DraftedOrder);
			}
		}

		public static void End()
		{
			Current.Game.GetComponent<GearUpPolicyComp>().Revert();
		}

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			if (ev.shift && Current.Game.GetComponent<GearUpPolicyComp>().IsOn())
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

	//Backwardcompat
	public class GearUpMapComponent : MapComponent
	{
		public GearUpMapComponent(Map map) : base(map) { }
	}

	public class GearUpPolicyComp : GameComponent
	{
		public string lastPolicy = "";
		public Dictionary<string, int> lastPoliciesOnPawns = new Dictionary<string, int>();

		public GearUpPolicyComp(Game game) { }

		public override void ExposeData()
		{
			Scribe_Values.Look(ref lastPolicy, "lastPolicy", "");
			Scribe_Collections.Look(ref lastPoliciesOnPawns, "lastPoliciesOnPawns", LookMode.Value, LookMode.Value);
		}
		public void Set(string policy)
		{
			if (lastPolicy == "")
			{
				lastPolicy = SetBetterPawnControl.CurrentPolicy();
			}
			SetBetterPawnControl.SetPawnControlPolicy(policy ?? Settings.Get().betterPawnControlBattlePolicy);
		}
	public void Set(string policy, Pawn pawn)
	{
	  if (lastPolicy == "")
	  {
			lastPolicy = SetBetterPawnControl.CurrentPolicy();
	  }
	  if (lastPoliciesOnPawns == null || lastPoliciesOnPawns.GetType() != typeof(Dictionary<string, int>)) lastPoliciesOnPawns = new Dictionary<string, int>();

	  Log.Message($"{lastPolicy.ToString() ?? "Null"} - {lastPoliciesOnPawns.ToString() ?? "Null"} - {pawn.ToString() ?? "Null"} - {pawn.ThingID ?? "Null"}");
	  if (!lastPoliciesOnPawns.ContainsKey(pawn.ThingID))
	  {
			lastPoliciesOnPawns[pawn.ThingID] = Current.Game.outfitDatabase.AllOutfits.IndexOf(pawn.outfits.CurrentOutfit);
			pawn.outfits.CurrentOutfit = Current.Game.outfitDatabase.AllOutfits.Find(dat => dat.label == Settings.Get().betterPawnControlBattlePolicy);
	  }
	}
		public void Revert()
		{
			if (lastPolicy == "") return;

			if(Settings.Get().useBetterPawnControl)
			{
				SetBetterPawnControl.SetPawnControlPolicy(lastPolicy);
			}
			else
			{
				if(lastPoliciesOnPawns == null || lastPoliciesOnPawns.GetType() != typeof(Dictionary<string,int>)) lastPoliciesOnPawns = new Dictionary<string, int>();

				foreach (KeyValuePair<string, int> value in lastPoliciesOnPawns.ToList())
				{
					Find.CurrentMap.mapPawns.FreeColonists.Find(dat => dat.ThingID == value.Key).outfits.CurrentOutfit = Current.Game.outfitDatabase.AllOutfits[value.Value];
					lastPoliciesOnPawns.Remove(value.Key);
				}
			}

			lastPolicy = "";
		}

		public bool IsOn()
		{
			return lastPolicy != "";
		}
	}

	public class CompGearUpAndGizGo : ThingComp
	{
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (this.parent is Pawn gizmoPawn)
			{
				GearUpPolicyComp component = Current.Game.GetComponent<GearUpPolicyComp>();
				yield return new Command_GearUpAndGo()
				{
					icon = component.lastPolicy != "" ? TexGearUpAndGo.guagIconActive : TexGearUpAndGo.guagIcon
				};
			}
		}
	}
}
