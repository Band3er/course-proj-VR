using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    public sealed class LocalProfileRepository
    {
        public const int MaximumNameLength = 12;
        public const int MaximumRecentRounds = 40;

        private const string DataFolderName =
            "ForestArchery";

        private const string MainFileName =
            "local_profiles.json";

        private const string BackupFileName =
            "local_profiles.bak";

        private const string TemporaryFileName =
            "local_profiles.tmp";

        private readonly string directoryPath;
        private readonly string mainFilePath;
        private readonly string backupFilePath;
        private readonly string temporaryFilePath;

        public string DirectoryPath => directoryPath;
        public string MainFilePath => mainFilePath;
        public string BackupFilePath => backupFilePath;

        public LocalGameData Data { get; private set; }

        public LocalProfileRepository()
            : this(
                Path.Combine(
                    Application.persistentDataPath,
                    DataFolderName))
        {
        }

        public LocalProfileRepository(
            string customDirectoryPath)
        {
            if (
                string.IsNullOrWhiteSpace(
                    customDirectoryPath)
            )
            {
                throw new ArgumentException(
                    "A valid data directory is required.",
                    nameof(customDirectoryPath));
            }

            directoryPath =
                customDirectoryPath;

            mainFilePath =
                Path.Combine(
                    directoryPath,
                    MainFileName);

            backupFilePath =
                Path.Combine(
                    directoryPath,
                    BackupFileName);

            temporaryFilePath =
                Path.Combine(
                    directoryPath,
                    TemporaryFileName);

            Data =
                new LocalGameData();
        }

        public LocalGameData LoadOrCreate()
        {
            Directory.CreateDirectory(
                directoryPath);

            if (
                TryLoadFile(
                    mainFilePath,
                    out LocalGameData loadedMain)
            )
            {
                Data =
                    NormalizeData(
                        loadedMain);

                return Data;
            }

            if (
                TryLoadFile(
                    backupFilePath,
                    out LocalGameData loadedBackup)
            )
            {
                Data =
                    NormalizeData(
                        loadedBackup);

                Save();
                return Data;
            }

            Data =
                new LocalGameData();

            Save();
            return Data;
        }

        public PlayerProfileData CreateProfile(
            string requestedName)
        {
            EnsureLoaded();

            string normalizedName =
                NormalizeDisplayName(
                    requestedName);

            if (
                Data.profiles.Any(
                    profile =>
                        profile != null &&
                        string.Equals(
                            profile.displayName,
                            normalizedName,
                            StringComparison.OrdinalIgnoreCase))
            )
            {
                throw new InvalidOperationException(
                    "A local profile with this name already exists.");
            }

            PlayerProfileData profile =
                new PlayerProfileData
                {
                    profileId =
                        Guid.NewGuid().ToString("N"),
                    displayName =
                        normalizedName,
                    createdUtc =
                        GetUtcTimestamp()
                };

            Data.profiles.Add(
                profile);

            Data.selectedProfileId =
                profile.profileId;

            Save();

            return profile;
        }

        public PlayerProfileData SelectProfile(
            string profileId)
        {
            EnsureLoaded();

            PlayerProfileData profile =
                FindProfile(
                    profileId);

            if (profile == null)
            {
                throw new InvalidOperationException(
                    "The selected local profile does not exist.");
            }

            Data.selectedProfileId =
                profile.profileId;

            Save();

            return profile;
        }

        public PlayerProfileData GetSelectedProfile()
        {
            EnsureLoaded();

            if (
                string.IsNullOrWhiteSpace(
                    Data.selectedProfileId)
            )
            {
                return null;
            }

            return
                FindProfile(
                    Data.selectedProfileId);
        }

        public PlayerProfileData FindProfile(
            string profileId)
        {
            EnsureLoaded();

            if (
                string.IsNullOrWhiteSpace(
                    profileId)
            )
            {
                return null;
            }

            return
                Data.profiles.FirstOrDefault(
                    profile =>
                        profile != null &&
                        string.Equals(
                            profile.profileId,
                            profileId,
                            StringComparison.Ordinal));
        }

        public RoundRecord RecordRound(
            string profileId,
            TimedGameInteractionMode mode,
            int score,
            int hits,
            int arrowsLaunched,
            int durationSeconds)
        {
            EnsureLoaded();

            PlayerProfileData profile =
                FindProfile(
                    profileId);

            if (profile == null)
            {
                throw new InvalidOperationException(
                    "Cannot record a round for an unknown profile.");
            }

            int safeScore =
                Math.Max(
                    0,
                    score);

            int safeHits =
                Math.Max(
                    0,
                    hits);

            int safeArrows =
                Math.Max(
                    0,
                    arrowsLaunched);

            int safeMisses =
                Math.Max(
                    0,
                    safeArrows - safeHits);

            float accuracy =
                safeArrows > 0
                    ? safeHits * 100f / safeArrows
                    : 0f;

            float averageScore =
                safeArrows > 0
                    ? safeScore / (float)safeArrows
                    : 0f;

            RoundRecord round =
                new RoundRecord
                {
                    roundId =
                        Guid.NewGuid().ToString("N"),
                    profileId =
                        profile.profileId,
                    playerName =
                        profile.displayName,
                    interactionMode =
                        mode,
                    score =
                        safeScore,
                    hits =
                        safeHits,
                    arrowsLaunched =
                        safeArrows,
                    misses =
                        safeMisses,
                    accuracy =
                        accuracy,
                    averageScorePerArrow =
                        averageScore,
                    durationSeconds =
                        Math.Max(
                            0,
                            durationSeconds),
                    completedUtc =
                        GetUtcTimestamp()
                };

            profile.recentRounds.Insert(
                0,
                round);

            if (
                profile.recentRounds.Count >
                MaximumRecentRounds
            )
            {
                profile.recentRounds.RemoveRange(
                    MaximumRecentRounds,
                    profile.recentRounds.Count -
                    MaximumRecentRounds);
            }

            ModeRecord modeRecord =
                LeaderboardService.GetModeRecord(
                    profile,
                    mode);

            modeRecord.roundsPlayed++;

            if (
                ShouldReplacePersonalBest(
                    modeRecord,
                    round)
            )
            {
                modeRecord.personalBestScore =
                    round.score;

                modeRecord.bestHits =
                    round.hits;

                modeRecord.bestShots =
                    round.arrowsLaunched;

                modeRecord.bestAccuracy =
                    round.accuracy;

                modeRecord.achievedUtc =
                    round.completedUtc;
            }

            Save();

            return round;
        }

        public void Save()
        {
            EnsureLoaded();

            Directory.CreateDirectory(
                directoryPath);

            string json =
                JsonUtility.ToJson(
                    Data,
                    true);

            File.WriteAllText(
                temporaryFilePath,
                json,
                new UTF8Encoding(false));

            if (
                !TryLoadFile(
                    temporaryFilePath,
                    out LocalGameData verifiedData)
            )
            {
                throw new IOException(
                    "The temporary local profile file could not be verified.");
            }

            NormalizeData(
                verifiedData);

            if (
                File.Exists(
                    mainFilePath)
            )
            {
                File.Copy(
                    mainFilePath,
                    backupFilePath,
                    true);
            }

            File.Copy(
                temporaryFilePath,
                mainFilePath,
                true);

            File.Delete(
                temporaryFilePath);
        }

        public void DeleteAllData()
        {
            Data =
                new LocalGameData();

            DeleteFileIfExists(
                temporaryFilePath);

            DeleteFileIfExists(
                mainFilePath);

            DeleteFileIfExists(
                backupFilePath);
        }

        public static string NormalizeDisplayName(
            string requestedName)
        {
            if (requestedName == null)
            {
                requestedName =
                    string.Empty;
            }

            string trimmed =
                requestedName.Trim();

            StringBuilder builder =
                new StringBuilder();

            foreach (char character in trimmed)
            {
                bool accepted =
                    char.IsLetterOrDigit(
                        character) ||
                    character == ' ' ||
                    character == '_' ||
                    character == '-';

                if (accepted)
                {
                    builder.Append(
                        character);
                }

                if (
                    builder.Length >=
                    MaximumNameLength
                )
                {
                    break;
                }
            }

            string normalized =
                builder.ToString().Trim();

            if (
                string.IsNullOrWhiteSpace(
                    normalized)
            )
            {
                throw new ArgumentException(
                    "The player name must contain at least one supported character.");
            }

            return normalized;
        }

        private bool TryLoadFile(
            string path,
            out LocalGameData loadedData)
        {
            loadedData =
                null;

            if (
                !File.Exists(
                    path)
            )
            {
                return false;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        path);

                if (
                    string.IsNullOrWhiteSpace(
                        json)
                )
                {
                    return false;
                }

                loadedData =
                    JsonUtility.FromJson<LocalGameData>(
                        json);

                return
                    loadedData != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[TIMED GAME DATA] Failed to load " +
                    path +
                    " | " +
                    exception.Message);

                loadedData =
                    null;

                return false;
            }
        }

        private static LocalGameData NormalizeData(
            LocalGameData data)
        {
            if (data == null)
            {
                data =
                    new LocalGameData();
            }

            if (data.profiles == null)
            {
                data.profiles =
                    new List<PlayerProfileData>();
            }

            data.profiles =
                data.profiles
                    .Where(
                        profile =>
                            profile != null)
                    .ToList();

            foreach (PlayerProfileData profile in data.profiles)
            {
                if (profile.controllerRecord == null)
                {
                    profile.controllerRecord =
                        new ModeRecord();
                }

                if (profile.handTrackingRecord == null)
                {
                    profile.handTrackingRecord =
                        new ModeRecord();
                }

                if (profile.recentRounds == null)
                {
                    profile.recentRounds =
                        new List<RoundRecord>();
                }

                if (
                    string.IsNullOrWhiteSpace(
                        profile.profileId)
                )
                {
                    profile.profileId =
                        Guid.NewGuid().ToString("N");
                }

                if (
                    string.IsNullOrWhiteSpace(
                        profile.displayName)
                )
                {
                    profile.displayName =
                        "Player";
                }
            }

            if (
                !string.IsNullOrWhiteSpace(
                    data.selectedProfileId) &&
                !data.profiles.Any(
                    profile =>
                        string.Equals(
                            profile.profileId,
                            data.selectedProfileId,
                            StringComparison.Ordinal))
            )
            {
                data.selectedProfileId =
                    string.Empty;
            }

            return data;
        }

        private static bool ShouldReplacePersonalBest(
            ModeRecord current,
            RoundRecord candidate)
        {
            if (
                current == null ||
                current.roundsPlayed <= 1
            )
            {
                return true;
            }

            if (
                candidate.score !=
                current.personalBestScore
            )
            {
                return
                    candidate.score >
                    current.personalBestScore;
            }

            if (
                Math.Abs(
                    candidate.accuracy -
                    current.bestAccuracy) >
                0.0001f
            )
            {
                return
                    candidate.accuracy >
                    current.bestAccuracy;
            }

            return
                candidate.hits >
                current.bestHits;
        }

        private static string GetUtcTimestamp()
        {
            return
                DateTime.UtcNow.ToString(
                    "o",
                    CultureInfo.InvariantCulture);
        }

        private static void DeleteFileIfExists(
            string path)
        {
            if (
                File.Exists(
                    path)
            )
            {
                File.Delete(
                    path);
            }
        }

        private void EnsureLoaded()
        {
            if (Data == null)
            {
                Data =
                    new LocalGameData();
            }

            Data =
                NormalizeData(
                    Data);
        }
    }
}
