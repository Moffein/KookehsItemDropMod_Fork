using RoR2;
using UnityEngine;

namespace KookehsDropItemMod_Fork
{
    public class MarkNonRecyclableComponent : MonoBehaviour
    {
        public void Start()
        {
            GenericPickupController gpc = base.GetComponent<GenericPickupController>();
            if (gpc)
            {
                gpc.Recycled = true;
                Destroy(this);
            }
        }
    }
}
