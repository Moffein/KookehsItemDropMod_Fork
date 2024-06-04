using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace DropItems_Fork
{
	public class DropItemHandler : MonoBehaviour, IPointerClickHandler
	{
        private Func<CharacterMaster> getMaster;
        private Func<PickupIndex> getPickupIndex;

        public void SetData(Func<CharacterMaster> getMaster, Func<PickupIndex> getPickupIndex) {
            this.getMaster = getMaster;
            this.getPickupIndex = getPickupIndex;
        }

		public void OnPointerClick(PointerEventData eventData)
		{
			KookehsDropItemMod.Logger.LogDebug("KDI: Pointer click, trying to send network message");
			var master = getMaster();

			if (!master.inventory.hasAuthority)
			{
				return;
			}

			var pickupIndex = getPickupIndex();
			var identity = master.GetBody().gameObject.GetComponent<NetworkIdentity>();

			//Don't check on the client, so that Server settings take priority.
			//if (!VerifyIsDroppable(pickupIndex)) return;

			KookehsDropItemMod.Logger.LogDebug("KDI: Sending network message");

            DropItemMessage itemDropMessage = new DropItemMessage(identity.netId, pickupIndex);
			itemDropMessage.Send(NetworkDestination.Server);
		}

		public static void DropItem(Transform charTransform, Inventory inventory, PickupIndex pickupIndex)
		{
			//Verify Tier on the server so that clients can't change their config at-will.
			if (!VerifyIsDroppable(pickupIndex)) return;

            KookehsDropItemMod.Logger.LogDebug("Transform: " + charTransform.position.ToString());
			KookehsDropItemMod.Logger.LogDebug("Inventory: " + inventory.name);
			KookehsDropItemMod.Logger.LogDebug("Pickup Index: " + pickupIndex);


			if (PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex != EquipmentIndex.None)
			{
				if (inventory.GetEquipmentIndex() != PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex)
				{
					return;
				}

				inventory.SetEquipmentIndex(EquipmentIndex.None);
			}
			else
			{
				if (inventory.GetItemCount(PickupCatalog.GetPickupDef(pickupIndex).itemIndex) <= 0) 
				{
					return;
				}

				inventory.RemoveItem(PickupCatalog.GetPickupDef(pickupIndex).itemIndex, 1);
			}

			var pickupInfo = new GenericPickupController.CreatePickupInfo()
			{
				pickupIndex = pickupIndex,
				position = charTransform.position
            };
			CreatePickupDroplet(pickupInfo, Vector3.up * 20f + charTransform.forward * 10f);

        }

		//Based off of RoR2.PickupDropletController.CreatePickupDroplet
		//Removed the Command flag from this.
        private static void CreatePickupDroplet(GenericPickupController.CreatePickupInfo pickupInfo, Vector3 velocity)
		{
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(PickupDropletController.pickupDropletPrefab, pickupInfo.position, Quaternion.identity);

            PickupDropletController pickupDropletController = gameObject.GetComponent<PickupDropletController>();
            if (pickupDropletController)
            {
                pickupDropletController.createPickupInfo = pickupInfo;
                pickupDropletController.NetworkpickupIndex = pickupInfo.pickupIndex;
            }

			GenericPickupController genericPickupController = gameObject.GetComponent<GenericPickupController>();
			if (genericPickupController)
			{
				genericPickupController.NetworkRecycled = true;
			}

            Rigidbody rigidBody = gameObject.GetComponent<Rigidbody>();
            rigidBody.velocity = velocity;
            rigidBody.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);

            NetworkServer.Spawn(gameObject);
        }


        private static bool VerifyIsDroppable(PickupIndex pickupIndex)
        {

            bool canDrop = true;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef != null)
            {
                ItemTierDef tier = ItemTierCatalog.GetItemTierDef(pickupDef.itemTier);

                if (tier != null)
                {
					//Check this here so that it defaults to allowing it if it can't find the tier.
                    if (!KookehsDropItemMod.allowInBazaar.Value)
                    {
                        SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
                        if (currentScene && currentScene.baseSceneName == "bazaar")
                        {
                            return false;
                        }
                    }

                    canDrop = tier.isDroppable;

                    if (KookehsDropItemMod.allowDropLunar.Value && tier.tier == ItemTier.Lunar)
                    {
                        canDrop = true;
                    }
                    else if (KookehsDropItemMod.allowDropVoid.Value
                        && (tier.tier == ItemTier.VoidBoss || tier.tier == ItemTier.VoidTier1 || tier.tier == ItemTier.VoidTier2 || tier.tier == ItemTier.VoidTier3))
                    {
                        canDrop = true;
                    }
                }
            }

			return canDrop;
		}

		public static void CreateNotification(CharacterBody character, Transform transform, PickupIndex pickupIndex)
		{
			if (PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex != EquipmentIndex.None)
			{
				CreateNotification(character, transform, PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
			} 
			else
			{
				CreateNotification(character, transform, PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
			}
		}

		private static void CreateNotification(CharacterBody character, Transform transform, EquipmentIndex equipmentIndex)
		{
			var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			const string title = "Equipment dropped";
			var description = Language.GetString(equipmentDef.nameToken);
			var texture = equipmentDef.pickupIconTexture;

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, ItemIndex itemIndex)
		{
			var itemDef = ItemCatalog.GetItemDef(itemIndex);
			const string title = "Item dropped";
			var description = Language.GetString(itemDef.nameToken);
			var texture = itemDef.pickupIconTexture;

			CreateNotification(character, transform, title, description, texture);
		}

		private static void CreateNotification(CharacterBody character, Transform transform, string title, string description, Texture texture)
        {
            var notification = character.gameObject.GetComponent<DropItemNotification>();
            if (notification == null)
            {
                notification = character.gameObject.AddComponent<DropItemNotification>();
                notification.transform.SetParent(transform);
			}
            notification.SetNotification(title, description, texture);
        }
	}
}