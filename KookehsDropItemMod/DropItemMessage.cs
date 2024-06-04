using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DropItems_Fork
{
	public class DropItemMessage : INetMessage
	{
		private NetworkInstanceId netId;
		private PickupIndex pickupIndex;

        public DropItemMessage() {
        }

		public DropItemMessage(NetworkInstanceId netId, PickupIndex pickupIndex)
		{
			this.netId = netId;
			this.pickupIndex = pickupIndex;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(netId);
			PickupIndex.WriteToNetworkWriter(writer, pickupIndex);
		}

		public void Deserialize(NetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			pickupIndex = reader.ReadPickupIndex();
		}

		public override string ToString() => $"DropItemMessage: {pickupIndex}";

		private void Log(string message) {
			KookehsDropItemMod.Logger.LogDebug(message);
		}

        public void OnReceived() {
			if (!NetworkServer.active) {
				return;
            }

			Log("Received kookeh drop message");
			Log("NetworkID" + netId.ToString());
            Log("PickupIndex" + this.pickupIndex.ToString());

			var bodyObject = Util.FindNetworkObject(netId);
			if (!bodyObject) return;

			var body = bodyObject.GetComponent<CharacterBody>();
			Log("Body is null: " + (body == null).ToString());
			if (!body) return;

			var inventory = body.master.inventory;
			var charTransform = body.transform;

			Vector3 position = body.corePosition;
			Vector3 direction = body.characterDirection ? body.characterDirection.forward : body.transform.forward;

			DropItemHandler.DropItem(position, direction, inventory, pickupIndex);

			if (KookehsDropItemMod.enableNotifications.Value) DropItemHandler.CreateNotification(body, pickupIndex);
		}
    }
}