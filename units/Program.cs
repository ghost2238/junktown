using System.Text.RegularExpressions;

// Some units and calculations used on https://www.thescaleoflife.com

// Convert from imperial units
string imperialResult = UnitConverter.FormatNumber(220, ImperialUnit.Pounds, UnitType.Metric);
Console.WriteLine(imperialResult);  // Output: "100 kg"

// Convert from metric units
string metricResult = UnitConverter.FormatNumberFromMetric(100, MetricUnit.Kilograms, UnitType.Imperial);
Console.WriteLine(metricResult);    // Output: "220 lbs."

string mixitupResult = UnitConverter.FormatNumberFromMetric(10, MetricUnit.Kilometers, UnitType.Mixitup);
Console.WriteLine(mixitupResult);   // Output: "0 trips around earth"

string arnold = UnitConverter.FormatNumberFromMetric(500, MetricUnit.Kilograms, UnitType.UnusualUnits);
Console.WriteLine(arnold);

public enum UnitType
{
    Imperial,
    Metric,
    Mixitup,
    Naturethings,
    UnusualUnits,
    BigUnits
}

public enum ImperialUnit
{
    Pounds,
    Gallons,
    Miles
}

public enum MetricUnit
{
    Kilograms,
    Liters,
    Kilometers
}

public record UnitConversion(
    string Imperial,
    string Metric,
    double Factor,
    string Mixitup,
    double MixitupFactor,
    string Naturethings,
    double NaturethingsFactor,
    string UnusualUnits,
    double UnusualUnitsFactor,
    string BigUnits,
    double BigUnitsFactor
);

public static class UnitConverter
{
    private static readonly Dictionary<ImperialUnit, UnitConversion> UnitConversions = new()
    {
        [ImperialUnit.Pounds] = new(
            Imperial: "lbs.",
            Metric: "kg",
            Factor: 0.45359237,
            Mixitup: "Ford Pintos",
            MixitupFactor: 1.0 / 2015,
            Naturethings: "African elephants",
            NaturethingsFactor: 1.0 / 11000,
            UnusualUnits: "Arnold Schwarzeneggers",
            UnusualUnitsFactor: 1.0 / 235,
            BigUnits: "Boeing 747 airplanes",
            BigUnitsFactor: 1.0 / 412300
        ),
        [ImperialUnit.Gallons] = new(
            Imperial: "gal.",
            Metric: "L",
            Factor: 3.785411784,
            Mixitup: "tanker trucks",
            MixitupFactor: 1.0 / 9000,
            Naturethings: "seconds of Nile river flow",
            NaturethingsFactor: 1.0 / 805000,
            UnusualUnits: "bathtubs",
            UnusualUnitsFactor: 1.0 / 65,
            BigUnits: "Goodyear blimps",
            BigUnitsFactor: 1.0 / 2225501
        ),
        [ImperialUnit.Miles] = new(
            Imperial: "mi.",
            Metric: "km",
            Factor: 1.609344,
            Mixitup: "trips around earth",
            MixitupFactor: 1.0 / 24901,
            Naturethings: "lengths of the Nile river",
            NaturethingsFactor: 1.0 / 4130,
            UnusualUnits: "marathons",
            UnusualUnitsFactor: 1.0 / 26.2,
            BigUnits: "trips to the moon",
            BigUnitsFactor: 1.0 / 238900
        )
    };

    private static readonly Dictionary<MetricUnit, ImperialUnit> MetricToImperialMap = new()
    {
        [MetricUnit.Kilograms] = ImperialUnit.Pounds,
        [MetricUnit.Liters] = ImperialUnit.Gallons,
        [MetricUnit.Kilometers] = ImperialUnit.Miles
    };

    public static string FormatNumber(double number, ImperialUnit unit, UnitType selectedUnitType)
    {
        if (!UnitConversions.TryGetValue(unit, out var conversion))
            return FormatNumberWithoutUnit(number);

        (number, string unitString) = selectedUnitType switch
        {
            UnitType.Metric => (number * conversion.Factor, conversion.Metric),
            UnitType.Mixitup => (number * conversion.MixitupFactor, conversion.Mixitup),
            UnitType.Naturethings => (number * conversion.NaturethingsFactor, conversion.Naturethings),
            UnitType.UnusualUnits => (number * conversion.UnusualUnitsFactor, conversion.UnusualUnits),
            UnitType.BigUnits => (number * conversion.BigUnitsFactor, conversion.BigUnits),
            _ => (number, conversion.Imperial)
        };

        return FormatNumberWithUnit(number, unitString);
    }

    public static string FormatNumberFromMetric(double number, MetricUnit unit, UnitType selectedUnitType)
    {
        if (!MetricToImperialMap.TryGetValue(unit, out var imperialUnit))
            return FormatNumberWithoutUnit(number);

        if (!UnitConversions.TryGetValue(imperialUnit, out var conversion))
            return FormatNumberWithoutUnit(number);

        double imperialNumber = number / conversion.Factor;

        (double convertedNumber, string unitString) = selectedUnitType switch
        {
            UnitType.Metric => (number, conversion.Metric),
            UnitType.Imperial => (imperialNumber, conversion.Imperial),
            UnitType.Mixitup => (imperialNumber * conversion.MixitupFactor, conversion.Mixitup),
            UnitType.Naturethings => (imperialNumber * conversion.NaturethingsFactor, conversion.Naturethings),
            UnitType.UnusualUnits => (imperialNumber * conversion.UnusualUnitsFactor, conversion.UnusualUnits),
            UnitType.BigUnits => (imperialNumber * conversion.BigUnitsFactor, conversion.BigUnits),
            _ => throw new ArgumentException("Invalid UnitType", nameof(selectedUnitType))
        };

        return FormatNumberWithUnit(convertedNumber, unitString);
    }

    public static string FormatMetricNumber(double number, ImperialUnit unit)
    {
        if (!UnitConversions.TryGetValue(unit, out var conversion))
            return FormatNumberWithoutUnit(number);

        number *= conversion.Factor;
        return FormatNumberWithUnit(Math.Round(number), conversion.Metric);
    }

    private static string FormatNumberWithoutUnit(double number) =>
        number >= 1e21 ? $"{number:e7}" : FormatLargeNumber(Math.Round(number));

    private static string FormatNumberWithUnit(double number, string unit) =>
        $"{FormatNumberWithoutUnit(number)} {unit}".Trim();

    private static string FormatLargeNumber(double number) =>
        Regex.Replace(number.ToString(), @"\B(?=(\d{3})+(?!\d))", ",");
}