﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace GearUpAndGo
{
	public class Settings : ModSettings
	{
		public string betterPawnControlBattlePolicy = "";
		public string betterPawnControlNormalPolicy = "";

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("TD.SettingBetterPawnControlPolicy".Translate());
			betterPawnControlBattlePolicy = options.TextEntry(betterPawnControlBattlePolicy, 1);
			options.Label("TD.SettingBetterPawnControlPolicyNormal".Translate());
			betterPawnControlNormalPolicy = options.TextEntry(betterPawnControlNormalPolicy, 1);
			options.Label("TD.SettingBetterPawnControlCustom".Translate());
			options.Label("TD.SettingBetterPawnControlRemembered".Translate());
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref betterPawnControlBattlePolicy, "betterPawnControlBattlePolicy", "");
			Scribe_Values.Look(ref betterPawnControlNormalPolicy, "betterPawnControlNormalPolicy", "");
		}
	}
}