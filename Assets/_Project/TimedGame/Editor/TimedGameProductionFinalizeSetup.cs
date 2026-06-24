using System;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameProductionFinalizeSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string FinalCompanyName =
        "MRGV";

    private const string FinalProductName =
        "Forest Archery VR";

    private const string FinalApplicationIdentifier =
        "com.mrgv.forestarcheryvr";

    private const string FinalBundleVersion =
        "1.0.0";

    private const int FinalBundleVersionCode =
        1;

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08f - Apply Final Production Configuration")]
    public static void Apply()
    {
        if (!ValidateActiveScene())
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08f",
                "Exit Play Mode before applying the final production configuration.",
                "OK");

            return;
        }

        Scene scene =
            SceneManager.GetActiveScene();

        try
        {
            GameObject systemObject =
                GameObject.Find(
                    "TimedGameSystem");

            if (systemObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            TimedRoundController roundController =
                systemObject.GetComponent<TimedRoundController>();

            TimedGameMenuController menuController =
                systemObject.GetComponent<TimedGameMenuController>();

            TimedGameGameplayBridge gameplayBridge =
                systemObject.GetComponent<TimedGameGameplayBridge>();

            if (roundController == null)
            {
                throw new InvalidOperationException(
                    "TimedRoundController is missing.");
            }

            if (menuController == null)
            {
                throw new InvalidOperationException(
                    "TimedGameMenuController is missing.");
            }

            if (gameplayBridge == null)
            {
                throw new InvalidOperationException(
                    "TimedGameGameplayBridge is missing.");
            }

            SerializedObject roundSerialized =
                new SerializedObject(
                    roundController);

            SetFloat(
                roundSerialized,
                "roundDurationSeconds",
                300f);

            SetFloat(
                roundSerialized,
                "countdownSeconds",
                3f);

            SetBoolean(
                roundSerialized,
                "verboseLogging",
                false);

            roundSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            SerializedObject menuSerialized =
                new SerializedObject(
                    menuController);

            SetBoolean(
                menuSerialized,
                "useShortDeviceIntegrationRound",
                false);

            SetFloat(
                menuSerialized,
                "deviceIntegrationRoundDurationSeconds",
                60f);

            menuSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            SerializedObject bridgeSerialized =
                new SerializedObject(
                    gameplayBridge);

            SetBoolean(
                bridgeSerialized,
                "clearExistingArrowsAtRoundStart",
                true);

            SetBoolean(
                bridgeSerialized,
                "verboseLogging",
                false);

            bridgeSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            UpdateModeInformationText();

            PlayerSettings.companyName =
                FinalCompanyName;

            PlayerSettings.productName =
                FinalProductName;

            PlayerSettings.bundleVersion =
                FinalBundleVersion;

            PlayerSettings.Android.bundleVersionCode =
                FinalBundleVersionCode;

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android,
                FinalApplicationIdentifier);

            EditorUtility.SetDirty(
                systemObject);

            EditorUtility.SetDirty(
                roundController);

            EditorUtility.SetDirty(
                menuController);

            EditorUtility.SetDirty(
                gameplayBridge);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();

            ValidateConfigurationInternal();

            Selection.activeGameObject =
                systemObject;

            Debug.Log(
                "[TIMED GAME PRODUCTION] Stage 08f applied." +
                "\nRound duration: 300 seconds" +
                "\nCountdown: 3 seconds" +
                "\nShort device test: disabled" +
                "\nApplication identifier: " +
                FinalApplicationIdentifier +
                "\nProduct name: " +
                FinalProductName +
                "\nVersion: " +
                FinalBundleVersion +
                " (" +
                FinalBundleVersionCode +
                ")");

            EditorUtility.DisplayDialog(
                "Stage 08f Applied",
                "Final production configuration was applied.\n\n" +
                "Round duration: 05:00\n" +
                "Countdown: 3 seconds\n" +
                "Short device test: disabled\n" +
                "Android identifier:\n" +
                FinalApplicationIdentifier +
                "\n\n" +
                "The scene and project settings were saved.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08f Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08f - Validate Final Production Configuration")]
    public static void ValidateConfiguration()
    {
        if (!ValidateActiveScene())
        {
            return;
        }

        try
        {
            ValidateConfigurationInternal();

            Debug.Log(
                "[TIMED GAME PRODUCTION] Stage 08f validation passed." +
                "\n05:00 production round: configured" +
                "\nShort device test: disabled" +
                "\nGameplay bridge: present" +
                "\nFinal Android identifier: configured");

            EditorUtility.DisplayDialog(
                "Stage 08f Validation Passed",
                "The final 5-minute production configuration is valid.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08f Validation Failed",
                exception.Message,
                "OK");
        }
    }

    private static void ValidateConfigurationInternal()
    {
        GameObject systemObject =
            GameObject.Find(
                "TimedGameSystem");

        if (systemObject == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        TimedRoundController roundController =
            systemObject.GetComponent<TimedRoundController>();

        TimedGameMenuController menuController =
            systemObject.GetComponent<TimedGameMenuController>();

        TimedGameGameplayBridge gameplayBridge =
            systemObject.GetComponent<TimedGameGameplayBridge>();

        if (
            roundController == null ||
            menuController == null ||
            gameplayBridge == null
        )
        {
            throw new InvalidOperationException(
                "One or more Timed Game components are missing.");
        }

        SerializedObject roundSerialized =
            new SerializedObject(
                roundController);

        float duration =
            roundSerialized
                .FindProperty(
                    "roundDurationSeconds")
                .floatValue;

        float countdown =
            roundSerialized
                .FindProperty(
                    "countdownSeconds")
                .floatValue;

        if (
            Mathf.Abs(
                duration -
                300f) >
            0.01f
        )
        {
            throw new InvalidOperationException(
                "Round duration is not 300 seconds.");
        }

        if (
            Mathf.Abs(
                countdown -
                3f) >
            0.01f
        )
        {
            throw new InvalidOperationException(
                "Countdown is not 3 seconds.");
        }

        SerializedObject menuSerialized =
            new SerializedObject(
                menuController);

        bool shortDeviceTest =
            menuSerialized
                .FindProperty(
                    "useShortDeviceIntegrationRound")
                .boolValue;

        if (shortDeviceTest)
        {
            throw new InvalidOperationException(
                "Short device integration-test mode is still enabled.");
        }

        string currentIdentifier =
            PlayerSettings.GetApplicationIdentifier(
                BuildTargetGroup.Android);

        if (
            !string.Equals(
                currentIdentifier,
                FinalApplicationIdentifier,
                StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                "Android identifier is incorrect: " +
                currentIdentifier);
        }

        if (
            !string.Equals(
                PlayerSettings.productName,
                FinalProductName,
                StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                "Product name is incorrect.");
        }

        if (
            !string.Equals(
                PlayerSettings.bundleVersion,
                FinalBundleVersion,
                StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                "Bundle version is incorrect.");
        }

        if (
            PlayerSettings.Android.bundleVersionCode !=
            FinalBundleVersionCode
        )
        {
            throw new InvalidOperationException(
                "Android bundle version code is incorrect.");
        }
    }

    private static void UpdateModeInformationText()
    {
        GameObject modePanel =
            GameObject.Find(
                "ModeSelectionPanel");

        if (modePanel == null)
        {
            return;
        }

        Transform infoTransform =
            modePanel.transform.Find(
                "Info");

        if (infoTransform == null)
        {
            return;
        }

        Text infoText =
            infoTransform.GetComponent<Text>();

        if (infoText == null)
        {
            return;
        }

        infoText.text =
            "TIMED ROUND: 5 MINUTES\n" +
            "HAND TRACKING MODE: EXPERIMENTAL";

        EditorUtility.SetDirty(
            infoText);
    }

    private static bool ValidateActiveScene()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (
            !scene.IsValid() ||
            !scene.isLoaded ||
            !string.Equals(
                scene.path,
                RequiredScenePath,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            EditorUtility.DisplayDialog(
                "Stage 08f",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return false;
        }

        return true;
    }

    private static void SetBoolean(
        SerializedObject serialized,
        string propertyName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.boolValue =
            value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string propertyName,
        float value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.floatValue =
            value;
    }
}
