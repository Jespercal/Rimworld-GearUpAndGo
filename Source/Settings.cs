﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace GearUpAndGo
{
	class Settings : ModSettings
	{
		public bool useBetterPawnControl = true;
		public string betterPawnControlBattlePolicy = "";

		public static Settings Get()
		{
			return LoadedModManager.GetMod<GearUpAndGo.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("TD.SettingBetterPawnControlPolicy".Translate());
			options.CheckboxLabeled("TD.SettingUseBetterPawnControl".Translate(), ref useBetterPawnControl);
			betterPawnControlBattlePolicy = options.TextEntry(betterPawnControlBattlePolicy, 1);
			options.Label("TD.SettingBetterPawnControlRemembered".Translate());
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref useBetterPawnControl, "useBetterPawnControl", true);
			Scribe_Values.Look(ref betterPawnControlBattlePolicy, "betterPawnControlBattlePolicy", "");
		}
	}
}