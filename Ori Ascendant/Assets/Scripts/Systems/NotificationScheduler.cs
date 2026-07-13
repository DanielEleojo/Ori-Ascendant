using System;
using System.Collections;
using OriAscendant.Save;
using UnityEngine;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace OriAscendant.Systems
{
    /// <summary>
    /// Local push notification scheduling (offline-progress-full reminder). Self-bootstraps
    /// via <see cref="Bootstrap"/> after scene load, mirroring the ProceduralAmbience
    /// pattern — no scene wiring required, inert in EditMode/PlayMode test scenes that carry
    /// no main canvas.
    ///
    /// On backgrounding (<see cref="OnApplicationPause"/> with paused=true): requests the
    /// iOS notification authorization once, lifetime (tracked via
    /// <see cref="NotificationPrefs.PermissionRequested"/>), then schedules a single local
    /// notification timed to the offline-progress cap so the player is nudged back once their
    /// Àṣẹ vessel is full. On resume: cancels it, since the player is already back.
    ///
    /// CLOUD-BUILD-VERIFY-ONLY: the Unity.Notifications.iOS API below is #if UNITY_IOS gated
    /// and cannot be exercised on the Linux dev box (no UNITY_IOS define, no local iOS
    /// compile) — verify authorization + scheduling behavior on the first Cloud Build /
    /// TestFlight run, matching IosBuildPostProcessor.cs's own verification caveat.
    /// </summary>
    public sealed class NotificationScheduler : MonoBehaviour
    {
        private const string BootstrapCanvasName = "MainCanvas";
        private const string OfflineReadyNotificationId = "ori_offline_ready";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Only activate in scenes that carry the main canvas — keeps this inert
            // in EditMode/PlayMode test scenes with no game infrastructure.
            if (FindMainCanvas() == null) return;
            if (FindObjectsByType<NotificationScheduler>(FindObjectsSortMode.None).Length > 0) return;
            var go = new GameObject(nameof(NotificationScheduler));
            go.AddComponent<NotificationScheduler>();
            DontDestroyOnLoad(go);
        }

        private static Canvas FindMainCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
                if (c.name == BootstrapCanvasName) return c;
            return null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                if (!NotificationPrefs.Enabled) return;

                if (!NotificationPrefs.PermissionRequested)
                {
                    NotificationPrefs.PermissionRequested = true; // one-time ask, regardless of outcome
#if UNITY_IOS
                    StartCoroutine(RequestAuthorizationThenSchedule());
                    return;
#endif
                }

#if UNITY_IOS
                ScheduleOfflineReadyNotification();
#endif
            }
            else
            {
#if UNITY_IOS
                iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
            }
        }

#if UNITY_IOS
        private IEnumerator RequestAuthorizationThenSchedule()
        {
            using var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, true);
            while (!request.IsFinished) yield return null;

            ScheduleOfflineReadyNotification();
        }

        private void ScheduleOfflineReadyNotification()
        {
            // Matching Identifier replaces rather than stacks (Unity Mobile Notifications
            // behavior) — no explicit remove-before-schedule needed.
            var notification = new iOSNotification
            {
                Identifier = OfflineReadyNotificationId,
                // placeholder copy pending native-speaker cultural review (§7.10)
                Title = "Ori Ascendant",
                Body = "Your cultivator's vessel is full — Àṣẹ awaits.",
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = TimeSpan.FromSeconds(OfflineProgressCalculator.MaxOfflineSeconds),
                    Repeats = false,
                },
            };

            iOSNotificationCenter.ScheduleNotification(notification);
        }
#endif
    }
}
