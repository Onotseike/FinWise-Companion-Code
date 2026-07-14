using System.Globalization;

namespace FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;

internal class ModuleHelpers
{
    public static string GetDefaultFromDate()
    {
        DateTime date = DateTime.Now;
        DateTime firstDayOfMonth = new(date.Year, date.Month, 1);
        return firstDayOfMonth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string GetDefaultToDate()
    {
        DateTime date = DateTime.Now;
        int lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        DateTime lastDayOfMonth = new(date.Year, date.Month, lastDay);
        return lastDayOfMonth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string? NormalizeDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        if (DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly isoDate))
            return isoDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        CultureInfo[] cultures =
        [
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("en-GB"),
            CultureInfo.CurrentCulture
        ];

        foreach (CultureInfo culture in cultures)
        {
            if (DateTimeOffset.TryParse(date, culture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset offsetDate))
                return offsetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (DateTime.TryParse(date, culture, DateTimeStyles.AllowWhiteSpaces, out DateTime localDate))
                return localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return null;
    }

    public static (string From, string To) NormalizeDateRange(string? from, string? to)
    {
        string normalizedFrom = NormalizeDate(from) ?? GetDefaultFromDate();
        string normalizedTo = NormalizeDate(to) ?? GetDefaultToDate();
        return (normalizedFrom, normalizedTo);
    }
}
