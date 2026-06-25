using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ForestArchery.TimedGame
{
    [DisallowMultipleComponent]
    public sealed class RemoveSpecificMenuRows : MonoBehaviour
    {
        private const float ScanIntervalSeconds = 0.25f;

        private static readonly string[] TargetPhrases =
        {
            "60-SECOND ROUND",
            "60 SECOND ROUND",
            "PAUSE / QUIT / ROUND",
            "PAUSE/QUIT/ROUND"
        };

        private Coroutine cleanupRoutine;

        private void Awake()
        {
            ApplyNow();
        }

        private void OnEnable()
        {
            ApplyNow();

            if (cleanupRoutine == null)
            {
                cleanupRoutine = StartCoroutine(PeriodicCleanup());
            }
        }

        private void OnDisable()
        {
            if (cleanupRoutine != null)
            {
                StopCoroutine(cleanupRoutine);
                cleanupRoutine = null;
            }
        }

        public void ApplyNow()
        {
            CleanupLegacyText();
            CleanupTextMeshPro();
        }

        private IEnumerator PeriodicCleanup()
        {
            WaitForSecondsRealtime wait =
                new WaitForSecondsRealtime(ScanIntervalSeconds);

            while (true)
            {
                ApplyNow();
                yield return wait;
            }
        }

        private static void CleanupLegacyText()
        {
            Text[] texts =
                FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Text textComponent in texts)
            {
                if (textComponent == null) {
                    continue;
                }

                string value = textComponent.text;

                if (!MatchesTarget(value)) {
                    continue;
                }

                GameObject target =
                    FindBestRemovalTarget(textComponent.transform);

                DisableTarget(target, textComponent);
            }
        }

        private static void CleanupTextMeshPro()
        {
            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null) {
                    continue;
                }

                Type type = behaviour.GetType();
                string fullName = type.FullName ?? string.Empty;

                if (
                    fullName != "TMPro.TextMeshProUGUI" &&
                    fullName != "TMPro.TextMeshPro"
                ) {
                    continue;
                }

                PropertyInfo textProperty =
                    type.GetProperty(
                        "text",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (
                    textProperty == null ||
                    !textProperty.CanRead ||
                    textProperty.PropertyType != typeof(string)
                ) {
                    continue;
                }

                string value =
                    textProperty.GetValue(behaviour) as string;

                if (!MatchesTarget(value)) {
                    continue;
                }

                GameObject target =
                    FindBestRemovalTarget(behaviour.transform);

                DisableTarget(target, behaviour);
            }
        }

        private static void DisableTarget(
            GameObject target,
            Component originalComponent)
        {
            if (originalComponent == null) {
                return;
            }

            if (target == null) {
                target = originalComponent.gameObject;
            }

            if (!target.activeSelf) {
                return;
            }

            target.SetActive(false);
        }

        private static GameObject FindBestRemovalTarget(
            Transform source)
        {
            if (source == null) {
                return null;
            }

            Transform parent = source.parent;

            if (
                parent != null &&
                IsTextOnlyContainer(parent)
            ) {
                return parent.gameObject;
            }

            if (
                parent != null &&
                parent.parent != null &&
                IsTextOnlyContainer(parent.parent)
            ) {
                return parent.parent.gameObject;
            }

            return source.gameObject;
        }

        private static bool IsTextOnlyContainer(
            Transform root)
        {
            if (root == null) {
                return false;
            }

            if (root.GetComponent<Button>() != null) {
                return false;
            }

            if (root.GetComponentInChildren<Button>(true) != null) {
                return false;
            }

            bool foundText = false;

            Text[] legacyTexts =
                root.GetComponentsInChildren<Text>(true);

            foreach (Text textComponent in legacyTexts)
            {
                if (
                    textComponent == null ||
                    string.IsNullOrWhiteSpace(textComponent.text)
                ) {
                    continue;
                }

                foundText = true;
            }

            MonoBehaviour[] behaviours =
                root.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null) {
                    continue;
                }

                Type type = behaviour.GetType();
                string fullName = type.FullName ?? string.Empty;

                if (
                    fullName != "TMPro.TextMeshProUGUI" &&
                    fullName != "TMPro.TextMeshPro"
                ) {
                    continue;
                }

                PropertyInfo textProperty =
                    type.GetProperty(
                        "text",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (
                    textProperty == null ||
                    !textProperty.CanRead ||
                    textProperty.PropertyType != typeof(string)
                ) {
                    continue;
                }

                string value =
                    textProperty.GetValue(behaviour) as string;

                if (string.IsNullOrWhiteSpace(value)) {
                    continue;
                }

                foundText = true;
            }

            return foundText;
        }

        private static bool MatchesTarget(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            foreach (string phrase in TargetPhrases)
            {
                if (
                    value.IndexOf(
                        phrase,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                ) {
                    return true;
                }
            }

            return false;
        }
    }
}