using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// מחזיק את מיקומי המשתתפים ומחשב מרחקים ו-CO2.
/// כל משתתף מזין עיר/מדינה לפני הסשן.
/// </summary>
[Serializable]
public class ParticipantLocation
{
    public string displayName;   // "Tel Aviv, IL"
    public float latitude;
    public float longitude;

    public ParticipantLocation(string name, float lat, float lon)
    {
        displayName = name;
        latitude = lat;
        longitude = lon;
    }
}

public static class SessionLocationData
{
    // -------------------------------------------------------------------
    // מילון ערים מוכרות — הרחב לפי הצורך
    // -------------------------------------------------------------------
    private static readonly Dictionary<string, ParticipantLocation> KnownCities =
        new Dictionary<string, ParticipantLocation>(StringComparer.OrdinalIgnoreCase)
    {
        // ישראל
        { "tel aviv",      new ParticipantLocation("Tel Aviv, IL",       32.08f,  34.78f) },
        { "jerusalem",     new ParticipantLocation("Jerusalem, IL",      31.78f,  35.22f) },
        { "haifa",         new ParticipantLocation("Haifa, IL",          32.82f,  34.99f) },
        { "beer sheva",    new ParticipantLocation("Beer Sheva, IL",     31.25f,  34.79f) },

        // ארה"ב
        { "new york",      new ParticipantLocation("New York, US",       40.71f, -74.01f) },
        { "los angeles",   new ParticipantLocation("Los Angeles, US",    34.05f,-118.24f) },
        { "chicago",       new ParticipantLocation("Chicago, US",        41.88f, -87.63f) },
        { "boston",        new ParticipantLocation("Boston, US",         42.36f, -71.06f) },
        { "miami",         new ParticipantLocation("Miami, US",          25.77f, -80.19f) },

        // אירופה
        { "london",        new ParticipantLocation("London, UK",         51.51f,  -0.13f) },
        { "paris",         new ParticipantLocation("Paris, FR",          48.86f,   2.35f) },
        { "berlin",        new ParticipantLocation("Berlin, DE",         52.52f,  13.40f) },
        { "amsterdam",     new ParticipantLocation("Amsterdam, NL",      52.37f,   4.90f) },
        { "zurich",        new ParticipantLocation("Zurich, CH",         47.38f,   8.54f) },
        { "rome",          new ParticipantLocation("Rome, IT",           41.90f,  12.50f) },
        { "madrid",        new ParticipantLocation("Madrid, ES",         40.42f,  -3.70f) },

        // אסיה
        { "tokyo",         new ParticipantLocation("Tokyo, JP",          35.68f, 139.69f) },
        { "beijing",       new ParticipantLocation("Beijing, CN",        39.91f, 116.39f) },
        { "dubai",         new ParticipantLocation("Dubai, AE",          25.20f,  55.27f) },
        { "singapore",     new ParticipantLocation("Singapore, SG",       1.35f, 103.82f) },

        // שאר
        { "sydney",        new ParticipantLocation("Sydney, AU",        -33.87f, 151.21f) },
        { "toronto",       new ParticipantLocation("Toronto, CA",        43.65f, -79.38f) },
        { "sao paulo",     new ParticipantLocation("São Paulo, BR",     -23.55f, -46.63f) },
    };

    // -------------------------------------------------------------------
    // קבועים לחישוב CO2
    // -------------------------------------------------------------------
    private const float EarthRadiusKm = 6371f;

    // ק"ג CO2 לנוסע-ק"מ (כולל גורם גובה טיסה x1.9)
    private const float CO2PerKmFlight = 0.255f;
    // ק"ג CO2 לנוסע-ק"מ ברכב
    private const float CO2PerKmCar = 0.192f;
    // סף ק"מ: מעל זה — מניחים טיסה
    private const float FlightThresholdKm = 500f;

    // -------------------------------------------------------------------
    // API ציבורי
    // -------------------------------------------------------------------

    /// <summary>
    /// מחפש עיר במילון. מחזיר null אם לא נמצאה.
    /// </summary>
    public static ParticipantLocation FindCity(string cityInput)
    {
        if (string.IsNullOrWhiteSpace(cityInput)) return null;
        string key = cityInput.Trim().ToLower();
        KnownCities.TryGetValue(key, out ParticipantLocation loc);
        return loc;
    }

    /// <summary>
    /// מחשב מרחק בין שתי נקודות (Haversine). מחזיר ק"מ.
    /// </summary>
    public static float DistanceKm(ParticipantLocation a, ParticipantLocation b)
    {
        float dLat = Mathf.Deg2Rad * (b.latitude - a.latitude);
        float dLon = Mathf.Deg2Rad * (b.longitude - a.longitude);

        float sinLat = Mathf.Sin(dLat / 2f);
        float sinLon = Mathf.Sin(dLon / 2f);

        float h = sinLat * sinLat +
                  Mathf.Cos(Mathf.Deg2Rad * a.latitude) *
                  Mathf.Cos(Mathf.Deg2Rad * b.latitude) *
                  sinLon * sinLon;

        return 2f * EarthRadiusKm * Mathf.Asin(Mathf.Sqrt(h));
    }

    /// <summary>
    /// מחשב סה"כ CO2 שנחסך עבור רשימת מיקומים (כל זוג מרחוק).
    /// מחזיר ק"ג.
    /// </summary>
    public static float TotalCO2SavedKg(List<ParticipantLocation> locations)
    {
        if (locations == null || locations.Count < 2) return 0f;

        float total = 0f;
        // כל זוג מיקומים — ה-"מרכז" הוא המארח (index 0)
        ParticipantLocation host = locations[0];
        for (int i = 1; i < locations.Count; i++)
        {
            float km = DistanceKm(host, locations[i]);
            // הלוך-חזור × 2
            float roundTripKm = km * 2f;
            float co2 = roundTripKm * (km > FlightThresholdKm ? CO2PerKmFlight : CO2PerKmCar);
            total += co2;
        }
        return total;
    }

    /// <summary>
    /// סה"כ ק"מ הלוך-חזור שנחסכו (כל משתתף חוץ מהמארח).
    /// </summary>
    public static float TotalKmSaved(List<ParticipantLocation> locations)
    {
        if (locations == null || locations.Count < 2) return 0f;

        float total = 0f;
        ParticipantLocation host = locations[0];
        for (int i = 1; i < locations.Count; i++)
            total += DistanceKm(host, locations[i]) * 2f;

        return total;
    }
}