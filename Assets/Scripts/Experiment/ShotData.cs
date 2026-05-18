using System;
using System.Globalization;

[Serializable]
public class ShotData
{
    public string participantId;
    public InputMode inputMode;
    public int shotNumber;
    public bool hit;
    public int score;
    public float timeToShoot;
    public float distanceFromCenter;
    public int failedGrabs;
    public int accidentalReleases;
    public int trackingLosses;
    public string notes;

    public static string CsvHeader()
    {
        return "timestamp,participant_id,input_mode,shot_number,hit,score,time_to_shoot,distance_from_center,failed_grabs,accidental_releases,tracking_losses,notes";
    }

    public string ToCsvLine()
    {
        return string.Join(",",
            Escape(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            Escape(participantId),
            Escape(inputMode.ToString()),
            shotNumber.ToString(CultureInfo.InvariantCulture),
            hit.ToString(),
            score.ToString(CultureInfo.InvariantCulture),
            timeToShoot.ToString("F3", CultureInfo.InvariantCulture),
            distanceFromCenter.ToString("F3", CultureInfo.InvariantCulture),
            failedGrabs.ToString(CultureInfo.InvariantCulture),
            accidentalReleases.ToString(CultureInfo.InvariantCulture),
            trackingLosses.ToString(CultureInfo.InvariantCulture),
            Escape(notes)
        );
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n");
        string escaped = value.Replace("\"", "\"\"");

        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}
