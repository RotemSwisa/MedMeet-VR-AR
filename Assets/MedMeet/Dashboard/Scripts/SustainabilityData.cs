using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Sustainability Showcase — data engine.
///
/// Mirrors data.jsx from the Claude Design 1:1:
///   - 25 real cities with lat/lon
///   - Published travel factors (DEFRA / EEA averages)
///   - computeImpact() turns participants + demo count into the full
///     metrics object used by the dashboard.
/// </summary>
public static class SustainabilityData
{
    // ── Cities ──────────────────────────────────────────────────────────────
    [Serializable]
    public class City
    {
        public string id;
        public string name;
        public string country;
        public float lat;
        public float lon;

        public City(string id, string name, string country, float lat, float lon)
        { this.id = id; this.name = name; this.country = country; this.lat = lat; this.lon = lon; }
    }

    public static readonly List<City> Cities = new List<City>
    {
        new City("tlv", "Tel Aviv",      "IL",  32.0853f,  34.7818f),
        new City("jrs", "Jerusalem",     "IL",  31.7683f,  35.2137f),
        new City("hfa", "Haifa",         "IL",  32.7940f,  34.9896f),
        new City("bsv", "Beer Sheva",    "IL",  31.2520f,  34.7915f),
        new City("lon", "London",        "UK",  51.5072f,  -0.1276f),
        new City("par", "Paris",         "FR",  48.8566f,   2.3522f),
        new City("ber", "Berlin",        "DE",  52.5200f,  13.4050f),
        new City("mad", "Madrid",        "ES",  40.4168f,  -3.7038f),
        new City("rom", "Rome",          "IT",  41.9028f,  12.4964f),
        new City("ist", "Istanbul",      "TR",  41.0082f,  28.9784f),
        new City("ath", "Athens",        "GR",  37.9838f,  23.7275f),
        new City("zur", "Zurich",        "CH",  47.3769f,   8.5417f),
        new City("ams", "Amsterdam",     "NL",  52.3676f,   4.9041f),
        new City("dxb", "Dubai",         "AE",  25.2048f,  55.2708f),
        new City("nyc", "New York",      "US",  40.7128f, -74.0060f),
        new City("bos", "Boston",        "US",  42.3601f, -71.0589f),
        new City("chi", "Chicago",       "US",  41.8781f, -87.6298f),
        new City("sfo", "San Francisco", "US",  37.7749f,-122.4194f),
        new City("tor", "Toronto",       "CA",  43.6532f, -79.3832f),
        new City("mum", "Mumbai",        "IN",  19.0760f,  72.8777f),
        new City("sin", "Singapore",     "SG",   1.3521f, 103.8198f),
        new City("tyo", "Tokyo",         "JP",  35.6762f, 139.6503f),
        new City("syd", "Sydney",        "AU", -33.8688f, 151.2093f),
        new City("jhb", "Johannesburg",  "ZA", -26.2041f,  28.0473f),
        new City("sao", "Sao Paulo",     "BR", -23.5505f, -46.6333f),
    };

    public static City CityById(string id)        => Cities.FirstOrDefault(c => c.id == id);
    public static City CityByName(string name)    => Cities.FirstOrDefault(c => string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase));

    // ── Factors (mirrors FACTORS in data.jsx) ──────────────────────────────
    public const float Co2PerKmCar    = 0.171f;
    public const float Co2PerKmPlane  = 0.180f;
    public const float FuelPerKmCar   = 0.067f;
    public const float FuelPerKmPlane = 0.037f;
    public const float SpeedCar       = 80f;
    public const float SpeedPlane     = 520f;
    public const float CarThresholdKm = 600f;
    public const int   SheetsPerParticipant            = 14;
    public const int   SheetsPerDemoPerParticipant     = 4;
    public const int   SheetsPerTree                   = 8333;
    public const float LitresWaterPerSheet             = 10f;
    public const int   GlovePairsPerDemoPerParticipant = 2;
    public const float Co2PerGlovePair                 = 0.052f;
    public const float Co2PerTreeYear                  = 21f;
    public const float REarthKm                        = 6371f;

    // ── Distance ────────────────────────────────────────────────────────────
    public static float Haversine(City a, City b)
    {
        float dLat = Mathf.Deg2Rad * (b.lat - a.lat);
        float dLon = Mathf.Deg2Rad * (b.lon - a.lon);
        float s = Mathf.Sin(dLat / 2f) * Mathf.Sin(dLat / 2f) +
                  Mathf.Cos(Mathf.Deg2Rad * a.lat) * Mathf.Cos(Mathf.Deg2Rad * b.lat) *
                  Mathf.Sin(dLon / 2f) * Mathf.Sin(dLon / 2f);
        return 2f * REarthKm * Mathf.Asin(Mathf.Min(1f, Mathf.Sqrt(s)));
    }

    // ── Participant + leg ──────────────────────────────────────────────────
    [Serializable]
    public class Participant
    {
        public string name;
        public string cityId;
        public Participant(string name, string cityId) { this.name = name; this.cityId = cityId; }
    }

    [Serializable]
    public class Leg
    {
        public string participantName;
        public string cityName;
        public string cityId;
        public float oneWay;
        public float roundTrip;
        public bool  isPlane;     // false = car
        public float co2;         // kg
        public float fuel;        // L
        public float hours;
    }

    [Serializable]
    public class Impact
    {
        public City host;
        public int n;             // participant count
        public int demos;
        public List<Leg> legs = new List<Leg>();

        public float km;          // total round-trip km
        public float co2Travel;   // kg CO2 from travel only
        public float fuel;        // L
        public float hours;       // h
        public int   flights;
        public int   drives;

        public int   sheets;
        public float waterL;
        public float trees;       // sheets → mature-tree equivalents (paper)

        public int   glovePairs;
        public float gloveCo2;    // kg

        public float co2Total;    // travel + glove
        public float treesEquivYear;  // co2Total / co2PerTreeYear
    }

    /// <summary>
    /// Build the full Impact object. First participant is the host (others travel
    /// to host city). Returns Impact with host=null when fewer than 2 valid entries.
    /// </summary>
    public static Impact Compute(List<Participant> participants, int demos)
    {
        var impact = new Impact { demos = Mathf.Max(0, demos) };
        if (participants == null) return impact;

        var valid = participants
            .Where(p => p != null && !string.IsNullOrEmpty(p.cityId) && CityById(p.cityId) != null)
            .ToList();

        impact.n = valid.Count;
        if (valid.Count == 0) return impact;

        impact.host = CityById(valid[0].cityId);
        for (int i = 1; i < valid.Count; i++)
        {
            var c = CityById(valid[i].cityId);
            float oneWay = Haversine(c, impact.host);
            float round  = oneWay * 2f;
            bool  plane  = oneWay >= CarThresholdKm;

            var leg = new Leg
            {
                participantName = valid[i].name,
                cityName        = c.name,
                cityId          = c.id,
                oneWay          = oneWay,
                roundTrip       = round,
                isPlane         = plane,
                co2             = round * (plane ? Co2PerKmPlane  : Co2PerKmCar),
                fuel            = round * (plane ? Fuel(true)     : Fuel(false)),
                hours           = round / (plane ? SpeedPlane     : SpeedCar),
            };
            impact.legs.Add(leg);

            impact.km        += leg.roundTrip;
            impact.co2Travel += leg.co2;
            impact.fuel      += leg.fuel;
            impact.hours     += leg.hours;
            if (plane) impact.flights++; else impact.drives++;
        }

        impact.sheets     = impact.n * SheetsPerParticipant
                          + impact.demos * impact.n * SheetsPerDemoPerParticipant;
        impact.waterL     = impact.sheets * LitresWaterPerSheet;
        impact.trees      = impact.sheets / (float) SheetsPerTree;

        impact.glovePairs = impact.demos * impact.n * GlovePairsPerDemoPerParticipant;
        impact.gloveCo2   = impact.glovePairs * Co2PerGlovePair;

        impact.co2Total      = impact.co2Travel + impact.gloveCo2;
        impact.treesEquivYear = impact.co2Total / Co2PerTreeYear;

        return impact;
    }

    private static float Fuel(bool plane) => plane ? FuelPerKmPlane : FuelPerKmCar;

    // ── Formatting helpers ──────────────────────────────────────────────────
    public static string Fmt(float n, int dec = 0)
        => n.ToString("N" + dec, System.Globalization.CultureInfo.InvariantCulture);

    public static string FmtCompact(float n)
    {
        if (n >= 1000f) return Fmt(n / 1000f, n >= 10000f ? 0 : 1) + "k";
        return Fmt(n, 0);
    }
}
