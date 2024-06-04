﻿using R2API;
using RoR2;
using UnityEngine;

namespace DropItems_Fork
{
    public class DropItemNotification : MonoBehaviour
    {
        private Notification notification;
        private float startTime = 0;
        private float duration = 6.0f;

        void Update()
        {
            if (notification != null)
            {
                if (Run.instance.fixedTime - startTime > duration)
                {
                    Destroy(notification);
                    Destroy(this);
                }
            }

            var time = (Run.instance.fixedTime - startTime) / duration;
            notification.GenericNotification.SetNotificationT(time);
        }

        private void OnDestroy()
        {
            if (notification != null)
            {
                Destroy(notification);
            }
        }

        public void SetNotification(string title, string description, Texture texture)
        {
            if (notification == null)
            {
                notification = gameObject.AddComponent<Notification>();
                //notification.tag = "DropItem";    //This gives red text in console.
                notification.transform.SetParent(transform);
                notification.SetPosition(new Vector3((float)(Screen.width * 0.8), (float)(Screen.height * 0.25), 0));
            }
            startTime = Run.instance.fixedTime;
            notification.SetIcon(texture);
            notification.GetTitle = () => title;
            notification.GetDescription = () => description;
        }
    }
}
