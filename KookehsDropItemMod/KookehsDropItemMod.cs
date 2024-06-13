using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KookehsDropItemMod_Fork;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Networking;
using R2API.Utils;
using RiskOfOptions;
using RoR2;
using RoR2.UI;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DropItems_Fork
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModGuid, ModName, ModVersion)]
	[R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(CommandHelper))]
	public class KookehsDropItemMod : BaseUnityPlugin
	{
		public static GameObject RootObject { get; set; }
		public static DropItemHandler DropItemHandler { get; set; }

		private const string ModGuid = "KookehsDropItemMod_Fork";
		private const string ModName = "Kookeh's Drop Item Mod (Fork)";
		private const string ModVersion = "2.4.3";

		public static event Action<ItemIcon> OnItemIconAdded;
		public static event Action<EquipmentIcon> OnEquipmentIconAdded;

        public static ConfigEntry<bool> allowDropLunar;
        public static ConfigEntry<bool> allowDropVoid;
        public static ConfigEntry<bool> enableNotifications;
        public static ConfigEntry<bool> allowInBazaar;
		public static ConfigEntry<bool> preventRecycle;

        internal new static ManualLogSource Logger { get; set; }

		public void Awake()
		{
			ReadConfig();

            Logger = base.Logger;
			NetworkingAPI.RegisterMessageType<DropItemMessage>();

			IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons += OnItemIconAddedHook;
			IL.RoR2.UI.ScoreboardStrip.SetMaster += OnEquipmentIconAddedHook;

			RootObject = new GameObject("DropItemsMod");
			DontDestroyOnLoad(RootObject);
			DropItemHandler = RootObject.AddComponent<DropItemHandler>();

			CommandHelper.AddToConsoleWhenReady();

			OnItemIconAdded += (itemIcon) => {
				if (itemIcon.GetComponent<DropItemHandler>() != null) return;

				Func<CharacterMaster> getCharacterMaster = () => itemIcon.rectTransform.parent.GetComponent<ItemInventoryDisplay>().GetFieldValue<Inventory>("inventory").GetComponent<CharacterMaster>();

				var dropItemHandler = itemIcon.transform.gameObject.AddComponent<DropItemHandler>();
				dropItemHandler.SetData(getCharacterMaster, () => PickupCatalog.FindPickupIndex(itemIcon.GetFieldValue<ItemIndex>("itemIndex")));
			};

			OnEquipmentIconAdded += (equipmentIcon) => {
				if (equipmentIcon.GetComponent<DropItemHandler>() != null) return;

				var dropItemHandler = equipmentIcon.transform.gameObject.AddComponent<DropItemHandler>();
				dropItemHandler.SetData(() => equipmentIcon.targetInventory.GetComponent<CharacterMaster>(), () => PickupCatalog.FindPickupIndex(equipmentIcon.targetInventory.GetEquipmentIndex()));
			};

            IL.RoR2.PickupDropletController.CreatePickup += PickupDropletController_CreatePickup;
		}

        private void PickupDropletController_CreatePickup(ILContext il)
        {
			ILCursor c = new ILCursor(il);
			c.GotoNext(MoveType.After,
				x => x.MatchCall(typeof(GenericPickupController), "CreatePickup"));
			c.Emit(OpCodes.Ldarg_0);	//self
			c.EmitDelegate<Func<GenericPickupController, PickupDropletController, GenericPickupController>>((pickup, self) =>
			{
				MarkNonRecyclableComponent mnrc = self.GetComponent<MarkNonRecyclableComponent>();
				if (mnrc) pickup.gameObject.AddComponent<MarkNonRecyclableComponent>();
				return pickup;
			});
        }

        private void ReadConfig()
		{
			enableNotifications = base.Config.Bind<bool>(new ConfigDefinition("General", "Enable Notifications"), true, new ConfigDescription("Display a notification when an item is dropped."));
			allowInBazaar = base.Config.Bind<bool>(new ConfigDefinition("General", "Allow in Bazaar (Server-Side)"), true, new ConfigDescription("Allow items to be dropped while in the Bazaar."));
			preventRecycle = base.Config.Bind<bool>(new ConfigDefinition("General", "Prevent Recycle (Server-Side)"), true, new ConfigDescription("Dropped items cannot be recycled."));

            allowDropLunar = base.Config.Bind<bool>(new ConfigDefinition("Tiers (Server-Side)", "Allow Lunar"), false, new ConfigDescription("Allow items of this tier to be dropped."));
            allowDropVoid = base.Config.Bind<bool>(new ConfigDefinition("Tiers (Server-Side)", "Allow Void"), false, new ConfigDescription("Allow items of this tier to be dropped."));

			if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) RiskOfOptionsCompat();
		}

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
		{
            ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableNotifications));
        }

		private static void OnItemIconAddedHook(ILContext il) {
			var cursor = new ILCursor(il).Goto(0);
			cursor.GotoNext(
				x => x.MatchStloc(out _),
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<ItemInventoryDisplay>("itemIcons")
			);
			cursor.Emit(OpCodes.Dup);
			cursor.EmitDelegate<Action<ItemIcon>>(i => OnItemIconAdded?.Invoke(i));
		}

		private static void OnEquipmentIconAddedHook(ILContext il) {
			var cursor = new ILCursor(il).Goto(0);
			var setSubscribedInventory = typeof(ItemInventoryDisplay).GetMethodCached("SetSubscribedInventory");
			cursor.GotoNext(x => x.MatchCallvirt(setSubscribedInventory));
			cursor.Index += 1;

			cursor.Emit(OpCodes.Ldarg_0);

			cursor.EmitDelegate<Action<ScoreboardStrip>>(eq => {
				if (eq != null && eq.equipmentIcon != null) {
					OnEquipmentIconAdded?.Invoke(eq.equipmentIcon);
				}
			});
		}
	}
}
