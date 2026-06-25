using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ForestArchery.TimedGame
{
    [DisallowMultipleComponent]
    public sealed class FinalPlayerFacingUICleanup : MonoBehaviour
    {
        private const float ScanIntervalSeconds = 0.25f;

        private static readonly string[] ForbiddenRowFragments =
        {
            "TEST BUILD",
            "RESET TEST",
            "60 SECONDS",
            "60 SECOND",
            "TEST MODE",
            "RESET"
        };

        private Coroutine cleanupRoutine;

        private void Awake()
        {
            RemoveEntireTestRows();
        }

        private void OnEnable()
        {
            RemoveEntireTestRows();

            if (cleanupRoutine == null)
            {
                cleanupRoutine =
                    StartCoroutine(
                        PeriodicCleanup());
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

        private IEnumerator PeriodicCleanup()
        {
            WaitForSecondsRealtime wait =
                new WaitForSecondsRealtime(
                    ScanIntervalSeconds);

            while (true)
            {
                RemoveEntireTestRows();
                yield return wait;
            }
        }

        public void RemoveEntireTestRows()
        {
            RemoveLegacyTextRows();
            RemoveTextMeshProRows();
        }

        private static void RemoveLegacyTextRows()
        {
            Text[] textComponents =
                FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            HashSet<GameObject> rowsToDisable =
                new HashSet<GameObject>();

            foreach (Text textComponent in textComponents)
            {
                if (
                    textComponent == null ||
                    string.IsNullOrWhiteSpace(
                        textComponent.text) ||
                    !ContainsForbiddenFragment(
                        textComponent.text)
                )
                {
                    continue;
                }

                rowsToDisable.Add(
                    FindSafeRowRoot(
                        textComponent.transform));
            }

            DisableRows(
                rowsToDisable);
        }

        private static void RemoveTextMeshProRows()
        {
            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            HashSet<GameObject> rowsToDisable =
                new HashSet<GameObject>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                Type type =
                    behaviour.GetType();

                string fullName =
                    type.FullName ?? string.Empty;

                if (
                    fullName != "TMPro.TextMeshProUGUI" &&
                    fullName != "TMPro.TextMeshPro"
                )
                {
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
                    textProperty.PropertyType !=
                        typeof(string)
                )
                {
                    continue;
                }

                string currentText =
                    textProperty.GetValue(
                        behaviour) as string;

                if (
                    string.IsNullOrWhiteSpace(
                        currentText) ||
                    !ContainsForbiddenFragment(
                        currentText)
                )
                {
                    continue;
                }

                rowsToDisable.Add(
                    FindSafeRowRoot(
                        behaviour.transform));
            }

            DisableRows(
                rowsToDisable);
        }

        private static void DisableRows(
            HashSet<GameObject> rowsToDisable)
        {
            foreach (GameObject row in rowsToDisable)
            {
                if (
                    row != null &&
                    row.activeSelf
                )
                {
                    row.SetActive(false);
                }
            }
        }

        private static GameObject FindSafeRowRoot(
            Transform textTransform)
        {
            if (textTransform == null)
            {
                return null;
            }

            Transform current =
                textTransform;

            GameObject lastSafe =
                textTransform.gameObject;

            for (
                int depth = 0;
                depth < 5 &&
                current != null;
                depth++
            )
            {
                if (
                    current.GetComponent<TimedGameMenuController>() != null
                )
                {
                    break;
                }

                if (ContainsOnlyTestRowText(current))
                {
                    lastSafe =
                        current.gameObject;

                    current =
                        current.parent;

                    continue;
                }

                break;
            }

            Button button =
                textTransform.GetComponentInParent<Button>(
                    true);

            if (
                button != null &&
                ContainsOnlyTestRowText(
                    button.transform)
            )
            {
                lastSafe =
                    button.gameObject;
            }

            return lastSafe;
        }

        private static bool ContainsOnlyTestRowText(
            Transform root)
        {
            if (root == null)
            {
                return false;
            }

            bool foundAnyText =
                false;

            Text[] legacyTexts =
                root.GetComponentsInChildren<Text>(
                    true);

            foreach (Text textComponent in legacyTexts)
            {
                if (
                    textComponent == null ||
                    string.IsNullOrWhiteSpace(
                        textComponent.text)
                )
                {
                    continue;
                }

                foundAnyText =
                    true;

                if (
                    !ContainsForbiddenFragment(
                        textComponent.text)
                )
                {
                    return false;
                }
            }

            MonoBehaviour[] behaviours =
                root.GetComponentsInChildren<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                Type type =
                    behaviour.GetType();

                string fullName =
                    type.FullName ?? string.Empty;

                if (
                    fullName != "TMPro.TextMeshProUGUI" &&
                    fullName != "TMPro.TextMeshPro"
                )
                {
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
                    textProperty.PropertyType !=
                        typeof(string)
                )
                {
                    continue;
                }

                string currentText =
                    textProperty.GetValue(
                        behaviour) as string;

                if (
                    string.IsNullOrWhiteSpace(
                        currentText)
                )
                {
                    continue;
                }

                foundAnyText =
                    true;

                if (
                    !ContainsForbiddenFragment(
                        currentText)
                )
                {
                    return false;
                }
            }

            return foundAnyText;
        }

        private static bool ContainsForbiddenFragment(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string forbidden in ForbiddenRowFragments)
            {
                if (
                    value.IndexOf(
                        forbidden,
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }

            return false;
        }
    }
}