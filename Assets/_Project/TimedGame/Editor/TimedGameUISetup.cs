using System;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameUISetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private static Font uiFont;

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08d - Install Menu and HUD")]
    public static void Install()
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
                "Stage 08d",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d",
                "Exit Play Mode before installing the menu.",
                "OK");

            return;
        }

        if (GameObject.Find("TimedGameSystem") != null)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d",
                "TimedGameSystem already exists.\n\n" +
                "No duplicate was created.",
                "OK");

            return;
        }

        GameObject centerEye =
            GameObject.Find(
                "CenterEyeAnchor");

        if (centerEye == null)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d",
                "CenterEyeAnchor was not found.",
                "OK");

            return;
        }

        try
        {
            uiFont =
                FindExistingFont();

            EnsureEventSystem();

            GameObject systemObject =
                new GameObject(
                    "TimedGameSystem");

            Undo.RegisterCreatedObjectUndo(
                systemObject,
                "Install Timed Game UI");

            TimedRoundController roundController =
                systemObject.AddComponent<TimedRoundController>();

            TimedGameMenuController menuController =
                systemObject.AddComponent<TimedGameMenuController>();

            GameObject canvasObject =
                new GameObject(
                    "TimedGameCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Install Timed Game Canvas");

            canvasObject.transform.SetParent(
                centerEye.transform,
                false);

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();

            canvasRect.sizeDelta =
                new Vector2(
                    1000f,
                    800f);

            canvasRect.localPosition =
                new Vector3(
                    0f,
                    -0.04f,
                    1.45f);

            canvasRect.localRotation =
                Quaternion.identity;

            canvasRect.localScale =
                Vector3.one *
                0.00135f;

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.WorldSpace;

            canvas.worldCamera =
                centerEye.GetComponent<Camera>();

            CanvasScaler canvasScaler =
                canvasObject.GetComponent<CanvasScaler>();

            canvasScaler.uiScaleMode =
                CanvasScaler.ScaleMode.ConstantPixelSize;

            canvasScaler.dynamicPixelsPerUnit =
                12f;

            CanvasGroup mainPanel =
                CreatePanel(
                    canvasObject.transform,
                    "MainMenuPanel");

            Text mainTitle =
                CreateText(
                    mainPanel.transform,
                    "Title",
                    "FOREST ARCHERY",
                    62,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        310f),
                    new Vector2(
                        900f,
                        90f));

            Text mainPlayer =
                CreateText(
                    mainPanel.transform,
                    "CurrentPlayer",
                    "CURRENT PLAYER\nNONE",
                    30,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        220f),
                    new Vector2(
                        720f,
                        80f));

            Button mainTimed =
                CreateButton(
                    mainPanel.transform,
                    "TimedRoundButton",
                    "PLAY TIMED ROUND",
                    new Vector2(
                        0f,
                        105f),
                    new Vector2(
                        520f,
                        70f));

            Button mainPractice =
                CreateButton(
                    mainPanel.transform,
                    "PracticeButton",
                    "PRACTICE",
                    new Vector2(
                        0f,
                        20f),
                    new Vector2(
                        520f,
                        70f));

            Button mainPlayers =
                CreateButton(
                    mainPanel.transform,
                    "PlayersButton",
                    "SELECT PLAYER",
                    new Vector2(
                        0f,
                        -65f),
                    new Vector2(
                        520f,
                        70f));

            Button mainLeaderboard =
                CreateButton(
                    mainPanel.transform,
                    "LeaderboardButton",
                    "LEADERBOARD",
                    new Vector2(
                        0f,
                        -150f),
                    new Vector2(
                        520f,
                        70f));

            Button mainReset =
                CreateButton(
                    mainPanel.transform,
                    "ResetButton",
                    "RESET LOCAL DATA",
                    new Vector2(
                        0f,
                        -235f),
                    new Vector2(
                        520f,
                        70f));

            Text mainStatus =
                CreateText(
                    mainPanel.transform,
                    "Status",
                    string.Empty,
                    17,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -335f),
                    new Vector2(
                        900f,
                        70f));

            CanvasGroup playerPanel =
                CreatePanel(
                    canvasObject.transform,
                    "PlayerSelectionPanel");

            CreateText(
                playerPanel.transform,
                "Title",
                "SELECT PLAYER",
                52,
                TextAnchor.MiddleCenter,
                new Vector2(
                    0f,
                    310f),
                new Vector2(
                    800f,
                    80f));

            Text playerList =
                CreateText(
                    playerPanel.transform,
                    "PlayerList",
                    "NO LOCAL PLAYERS",
                    34,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        0f,
                        125f),
                    new Vector2(
                        650f,
                        250f));

            Text playerStatus =
                CreateText(
                    playerPanel.transform,
                    "Status",
                    string.Empty,
                    22,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -20f),
                    new Vector2(
                        760f,
                        55f));

            Button playerPrevious =
                CreateButton(
                    playerPanel.transform,
                    "PreviousButton",
                    "< PREVIOUS",
                    new Vector2(
                        -165f,
                        -95f),
                    new Vector2(
                        300f,
                        62f));

            Button playerNext =
                CreateButton(
                    playerPanel.transform,
                    "NextButton",
                    "NEXT >",
                    new Vector2(
                        165f,
                        -95f),
                    new Vector2(
                        300f,
                        62f));

            Button playerConfirm =
                CreateButton(
                    playerPanel.transform,
                    "ConfirmButton",
                    "SELECT",
                    new Vector2(
                        0f,
                        -175f),
                    new Vector2(
                        420f,
                        65f));

            Button playerCreate =
                CreateButton(
                    playerPanel.transform,
                    "CreateButton",
                    "CREATE NEW PLAYER",
                    new Vector2(
                        0f,
                        -255f),
                    new Vector2(
                        420f,
                        65f));

            Button playerBack =
                CreateButton(
                    playerPanel.transform,
                    "BackButton",
                    "BACK",
                    new Vector2(
                        0f,
                        -335f),
                    new Vector2(
                        300f,
                        58f));

            CanvasGroup createPanel =
                CreatePanel(
                    canvasObject.transform,
                    "CreatePlayerPanel");

            CreateText(
                createPanel.transform,
                "Title",
                "CREATE PLAYER",
                50,
                TextAnchor.MiddleCenter,
                new Vector2(
                    0f,
                    320f),
                new Vector2(
                    800f,
                    75f));

            Text createName =
                CreateText(
                    createPanel.transform,
                    "PlayerName",
                    "_",
                    42,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        245f),
                    new Vector2(
                        700f,
                        60f));

            Text createStatus =
                CreateText(
                    createPanel.transform,
                    "Status",
                    "ENTER A PLAYER NAME",
                    20,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        195f),
                    new Vector2(
                        760f,
                        45f));

            GameObject keyboardObject =
                new GameObject(
                    "VirtualKeyboard",
                    typeof(RectTransform),
                    typeof(GridLayoutGroup));

            keyboardObject.transform.SetParent(
                createPanel.transform,
                false);

            RectTransform keyboardRect =
                keyboardObject.GetComponent<RectTransform>();

            keyboardRect.sizeDelta =
                new Vector2(
                    870f,
                    390f);

            keyboardRect.anchoredPosition =
                new Vector2(
                    0f,
                    -25f);

            GridLayoutGroup grid =
                keyboardObject.GetComponent<GridLayoutGroup>();

            grid.cellSize =
                new Vector2(
                    74f,
                    52f);

            grid.spacing =
                new Vector2(
                    8f,
                    8f);

            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            grid.constraintCount =
                10;

            grid.childAlignment =
                TextAnchor.UpperCenter;

            string[] tokens =
            {
                "A","B","C","D","E","F","G","H","I","J",
                "K","L","M","N","O","P","Q","R","S","T",
                "U","V","W","X","Y","Z","0","1","2","3",
                "4","5","6","7","8","9","_","-","SPACE","BACKSPACE",
                "CLEAR","CONFIRM"
            };

            foreach (string token in tokens)
            {
                string label =
                    token == "SPACE"
                        ? "SPACE"
                        : token == "BACKSPACE"
                            ? "DEL"
                            : token;

                CreateGridButton(
                    keyboardObject.transform,
                    "Key_" + token,
                    label);
            }

            Button createBack =
                CreateButton(
                    createPanel.transform,
                    "BackButton",
                    "BACK",
                    new Vector2(
                        0f,
                        -348f),
                    new Vector2(
                        280f,
                        55f));

            CanvasGroup modePanel =
                CreatePanel(
                    canvasObject.transform,
                    "ModeSelectionPanel");

            CreateText(
                modePanel.transform,
                "Title",
                "SELECT INTERACTION MODE",
                48,
                TextAnchor.MiddleCenter,
                new Vector2(
                    0f,
                    300f),
                new Vector2(
                    850f,
                    80f));

            Text modePlayer =
                CreateText(
                    modePanel.transform,
                    "Player",
                    "PLAYER",
                    28,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        220f),
                    new Vector2(
                        700f,
                        55f));

            Text controllerBest =
                CreateText(
                    modePanel.transform,
                    "ControllerBest",
                    "CONTROLLER BEST\n0",
                    25,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        -250f,
                        95f),
                    new Vector2(
                        380f,
                        95f));

            Text handBest =
                CreateText(
                    modePanel.transform,
                    "HandBest",
                    "HAND TRACKING BEST\n0",
                    25,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        250f,
                        95f),
                    new Vector2(
                        380f,
                        95f));

            Button controllerMode =
                CreateButton(
                    modePanel.transform,
                    "ControllerModeButton",
                    "CONTROLLER MODE",
                    new Vector2(
                        -250f,
                        -15f),
                    new Vector2(
                        400f,
                        78f));

            Button handMode =
                CreateButton(
                    modePanel.transform,
                    "HandTrackingModeButton",
                    "HAND TRACKING\nEXPERIMENTAL",
                    new Vector2(
                        250f,
                        -15f),
                    new Vector2(
                        400f,
                        78f));

            CreateText(
                modePanel.transform,
                "Info",
                "Editor UI demo uses a short round.\nProduction rounds remain 5 minutes.",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(
                    0f,
                    -150f),
                new Vector2(
                    800f,
                    90f));

            Button modeBack =
                CreateButton(
                    modePanel.transform,
                    "BackButton",
                    "BACK",
                    new Vector2(
                        0f,
                        -300f),
                    new Vector2(
                        300f,
                        60f));

            CanvasGroup leaderboard =
                CreatePanel(
                    canvasObject.transform,
                    "LeaderboardPanel");

            Text leaderboardTitle =
                CreateText(
                    leaderboard.transform,
                    "Title",
                    "CONTROLLER LEADERBOARD",
                    48,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        315f),
                    new Vector2(
                        850f,
                        75f));

            Button controllerTab =
                CreateButton(
                    leaderboard.transform,
                    "ControllerTab",
                    "CONTROLLER",
                    new Vector2(
                        -210f,
                        240f),
                    new Vector2(
                        360f,
                        58f));

            Button handTab =
                CreateButton(
                    leaderboard.transform,
                    "HandTrackingTab",
                    "HAND TRACKING",
                    new Vector2(
                        210f,
                        240f),
                    new Vector2(
                        360f,
                        58f));

            Text leaderboardEntries =
                CreateText(
                    leaderboard.transform,
                    "Entries",
                    "NO COMPLETED ROUNDS YET",
                    28,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        0f,
                        -10f),
                    new Vector2(
                        760f,
                        410f));

            Button leaderboardBack =
                CreateButton(
                    leaderboard.transform,
                    "BackButton",
                    "BACK",
                    new Vector2(
                        0f,
                        -320f),
                    new Vector2(
                        300f,
                        58f));

            CanvasGroup results =
                CreatePanel(
                    canvasObject.transform,
                    "ResultsPanel");

            Text resultsSummary =
                CreateText(
                    results.transform,
                    "Summary",
                    "ROUND COMPLETE",
                    28,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        90f),
                    new Vector2(
                        800f,
                        470f));

            Button playAgain =
                CreateButton(
                    results.transform,
                    "PlayAgainButton",
                    "PLAY AGAIN",
                    new Vector2(
                        -260f,
                        -250f),
                    new Vector2(
                        300f,
                        65f));

            Button resultsLeaderboard =
                CreateButton(
                    results.transform,
                    "LeaderboardButton",
                    "LEADERBOARD",
                    new Vector2(
                        0f,
                        -250f),
                    new Vector2(
                        300f,
                        65f));

            Button resultsMain =
                CreateButton(
                    results.transform,
                    "MainMenuButton",
                    "MAIN MENU",
                    new Vector2(
                        260f,
                        -250f),
                    new Vector2(
                        300f,
                        65f));

            CanvasGroup reset =
                CreatePanel(
                    canvasObject.transform,
                    "ResetConfirmationPanel");

            CreateText(
                reset.transform,
                "Title",
                "RESET LOCAL DATA",
                48,
                TextAnchor.MiddleCenter,
                new Vector2(
                    0f,
                    280f),
                new Vector2(
                    800f,
                    80f));

            Text resetWarning =
                CreateText(
                    reset.transform,
                    "Warning",
                    string.Empty,
                    27,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        30f),
                    new Vector2(
                        760f,
                        360f));

            Button resetCancel =
                CreateButton(
                    reset.transform,
                    "CancelButton",
                    "CANCEL",
                    new Vector2(
                        -190f,
                        -245f),
                    new Vector2(
                        330f,
                        70f));

            Button resetDelete =
                CreateButton(
                    reset.transform,
                    "DeleteButton",
                    "DELETE EVERYTHING",
                    new Vector2(
                        190f,
                        -245f),
                    new Vector2(
                        330f,
                        70f));

            CanvasGroup hud =
                CreateTransparentPanel(
                    canvasObject.transform,
                    "RoundHUD");

            Text timer =
                CreateText(
                    hud.transform,
                    "Timer",
                    "05:00",
                    60,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        325f),
                    new Vector2(
                        420f,
                        85f));

            Text roundMessage =
                CreateText(
                    hud.transform,
                    "Message",
                    string.Empty,
                    78,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        850f,
                        160f));

            AssignReferences(
                menuController,
                roundController,
                mainPanel,
                playerPanel,
                createPanel,
                modePanel,
                leaderboard,
                results,
                reset,
                hud,
                mainPlayer,
                mainStatus,
                mainTimed,
                mainPractice,
                mainPlayers,
                mainLeaderboard,
                mainReset,
                playerList,
                playerStatus,
                playerPrevious,
                playerNext,
                playerConfirm,
                playerCreate,
                playerBack,
                createName,
                createStatus,
                keyboardObject.transform,
                createBack,
                modePlayer,
                controllerBest,
                handBest,
                controllerMode,
                handMode,
                modeBack,
                leaderboardTitle,
                leaderboardEntries,
                controllerTab,
                handTab,
                leaderboardBack,
                resultsSummary,
                playAgain,
                resultsLeaderboard,
                resultsMain,
                resetWarning,
                resetCancel,
                resetDelete,
                timer,
                roundMessage);

            SetInitialPanelState(
                mainPanel,
                playerPanel,
                createPanel,
                modePanel,
                leaderboard,
                results,
                reset,
                hud);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            Selection.activeGameObject =
                systemObject;

            Debug.Log(
                "[TIMED GAME UI] Stage 08d installed." +
                " | canvas=" +
                GetHierarchyPath(
                    canvasObject.transform));

            EditorUtility.DisplayDialog(
                "Stage 08d Installed",
                "TimedGameSystem and TimedGameCanvas were added.\n\n" +
                "The scene was saved.\n\n" +
                "Next: verify the hierarchy and test the UI in Play Mode.\n" +
                "No existing bow, arrow, wildlife, score or HUD script was modified.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08d Failed",
                exception.Message,
                "OK");
        }
    }

    private static Font FindExistingFont()
    {
        Text existingText =
            UnityEngine.Object.FindFirstObjectByType<Text>();

        if (
            existingText != null &&
            existingText.font != null
        )
        {
            return existingText.font;
        }

        return
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            UnityEngine.Object.FindFirstObjectByType<EventSystem>();

        if (existing != null)
        {
            return;
        }

        GameObject eventSystemObject =
            new GameObject(
                "TimedGameEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

        Undo.RegisterCreatedObjectUndo(
            eventSystemObject,
            "Create Timed Game Event System");
    }

    private static CanvasGroup CreatePanel(
        Transform parent,
        string name)
    {
        GameObject panelObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));

        panelObject.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            panelObject.GetComponent<RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            panelObject.GetComponent<Image>();

        image.color =
            new Color(
                0.025f,
                0.045f,
                0.035f,
                0.94f);

        return
            panelObject.GetComponent<CanvasGroup>();
    }

    private static CanvasGroup CreateTransparentPanel(
        Transform parent,
        string name)
    {
        GameObject panelObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasGroup));

        panelObject.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            panelObject.GetComponent<RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        return
            panelObject.GetComponent<CanvasGroup>();
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        TextAnchor alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

        textObject.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        Text text =
            textObject.GetComponent<Text>();

        text.font =
            uiFont;

        text.text =
            value;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        text.horizontalOverflow =
            HorizontalWrapMode.Wrap;

        text.verticalOverflow =
            VerticalWrapMode.Truncate;

        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size)
    {
        GameObject buttonObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

        buttonObject.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color(
                0.12f,
                0.28f,
                0.18f,
                0.98f);

        Button button =
            buttonObject.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            Color.white;

        colors.highlightedColor =
            new Color(
                0.78f,
                1f,
                0.82f,
                1f);

        colors.pressedColor =
            new Color(
                0.55f,
                0.86f,
                0.62f,
                1f);

        colors.selectedColor =
            colors.highlightedColor;

        button.colors =
            colors;

        Text text =
            CreateText(
                buttonObject.transform,
                "Label",
                label,
                25,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                size);

        text.raycastTarget =
            false;

        return button;
    }

    private static Button CreateGridButton(
        Transform parent,
        string name,
        string label)
    {
        GameObject buttonObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

        buttonObject.transform.SetParent(
            parent,
            false);

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color(
                0.14f,
                0.30f,
                0.20f,
                0.98f);

        Button button =
            buttonObject.GetComponent<Button>();

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        Text text =
            CreateText(
                buttonObject.transform,
                "Label",
                label,
                18,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(
                    74f,
                    52f));

        text.raycastTarget =
            false;

        return button;
    }

    private static void AssignReferences(
        TimedGameMenuController controller,
        TimedRoundController roundController,
        CanvasGroup mainPanel,
        CanvasGroup playerPanel,
        CanvasGroup createPanel,
        CanvasGroup modePanel,
        CanvasGroup leaderboard,
        CanvasGroup results,
        CanvasGroup reset,
        CanvasGroup hud,
        Text mainPlayer,
        Text mainStatus,
        Button mainTimed,
        Button mainPractice,
        Button mainPlayers,
        Button mainLeaderboard,
        Button mainReset,
        Text playerList,
        Text playerStatus,
        Button playerPrevious,
        Button playerNext,
        Button playerConfirm,
        Button playerCreate,
        Button playerBack,
        Text createName,
        Text createStatus,
        Transform keyboardRoot,
        Button createBack,
        Text modePlayer,
        Text controllerBest,
        Text handBest,
        Button controllerMode,
        Button handMode,
        Button modeBack,
        Text leaderboardTitle,
        Text leaderboardEntries,
        Button controllerTab,
        Button handTab,
        Button leaderboardBack,
        Text resultsSummary,
        Button playAgain,
        Button resultsLeaderboard,
        Button resultsMain,
        Text resetWarning,
        Button resetCancel,
        Button resetDelete,
        Text timer,
        Text roundMessage)
    {
        SerializedObject serialized =
            new SerializedObject(
                controller);

        Set(
            serialized,
            "roundController",
            roundController);

        Set(
            serialized,
            "mainMenuPanel",
            mainPanel);

        Set(
            serialized,
            "playerSelectionPanel",
            playerPanel);

        Set(
            serialized,
            "createPlayerPanel",
            createPanel);

        Set(
            serialized,
            "modeSelectionPanel",
            modePanel);

        Set(
            serialized,
            "leaderboardPanel",
            leaderboard);

        Set(
            serialized,
            "resultsPanel",
            results);

        Set(
            serialized,
            "resetConfirmationPanel",
            reset);

        Set(
            serialized,
            "roundHud",
            hud);

        Set(
            serialized,
            "mainCurrentPlayerText",
            mainPlayer);

        Set(
            serialized,
            "mainStatusText",
            mainStatus);

        Set(
            serialized,
            "mainTimedRoundButton",
            mainTimed);

        Set(
            serialized,
            "mainPracticeButton",
            mainPractice);

        Set(
            serialized,
            "mainPlayersButton",
            mainPlayers);

        Set(
            serialized,
            "mainLeaderboardButton",
            mainLeaderboard);

        Set(
            serialized,
            "mainResetButton",
            mainReset);

        Set(
            serialized,
            "playerListText",
            playerList);

        Set(
            serialized,
            "playerSelectionStatusText",
            playerStatus);

        Set(
            serialized,
            "playerPreviousButton",
            playerPrevious);

        Set(
            serialized,
            "playerNextButton",
            playerNext);

        Set(
            serialized,
            "playerConfirmButton",
            playerConfirm);

        Set(
            serialized,
            "playerCreateButton",
            playerCreate);

        Set(
            serialized,
            "playerBackButton",
            playerBack);

        Set(
            serialized,
            "createPlayerNameText",
            createName);

        Set(
            serialized,
            "createPlayerStatusText",
            createStatus);

        Set(
            serialized,
            "virtualKeyboardRoot",
            keyboardRoot);

        Set(
            serialized,
            "createPlayerBackButton",
            createBack);

        Set(
            serialized,
            "modePlayerText",
            modePlayer);

        Set(
            serialized,
            "controllerBestText",
            controllerBest);

        Set(
            serialized,
            "handTrackingBestText",
            handBest);

        Set(
            serialized,
            "controllerModeButton",
            controllerMode);

        Set(
            serialized,
            "handTrackingModeButton",
            handMode);

        Set(
            serialized,
            "modeBackButton",
            modeBack);

        Set(
            serialized,
            "leaderboardTitleText",
            leaderboardTitle);

        Set(
            serialized,
            "leaderboardEntriesText",
            leaderboardEntries);

        Set(
            serialized,
            "controllerLeaderboardButton",
            controllerTab);

        Set(
            serialized,
            "handTrackingLeaderboardButton",
            handTab);

        Set(
            serialized,
            "leaderboardBackButton",
            leaderboardBack);

        Set(
            serialized,
            "resultsSummaryText",
            resultsSummary);

        Set(
            serialized,
            "resultsPlayAgainButton",
            playAgain);

        Set(
            serialized,
            "resultsLeaderboardButton",
            resultsLeaderboard);

        Set(
            serialized,
            "resultsMainMenuButton",
            resultsMain);

        Set(
            serialized,
            "resetWarningText",
            resetWarning);

        Set(
            serialized,
            "resetCancelButton",
            resetCancel);

        Set(
            serialized,
            "resetDeleteButton",
            resetDelete);

        Set(
            serialized,
            "timerText",
            timer);

        Set(
            serialized,
            "roundMessageText",
            roundMessage);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing serialized property: " +
                propertyName);
        }

        property.objectReferenceValue =
            value;
    }

    private static void SetInitialPanelState(
        params CanvasGroup[] panels)
    {
        foreach (CanvasGroup panel in panels)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        string path =
            transform.name;

        while (transform.parent != null)
        {
            transform =
                transform.parent;

            path =
                transform.name +
                "/" +
                path;
        }

        return path;
    }
}
