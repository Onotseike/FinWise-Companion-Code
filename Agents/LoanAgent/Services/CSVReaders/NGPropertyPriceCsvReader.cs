using System.Globalization;

using FinWise.LoanAgent.Models;

namespace FinWise.LoanAgent.Services.CSVReaders;

public class NGPropertyPriceCsvReader : PropertyPriceCsvReader<NGPropertyPriceRecord>
{
    protected override NGPropertyPriceRecord ParseLine(string line)
    {
        List<string> fields = ParseCsvLine(line);
        return new NGPropertyPriceRecord(
            fields[0],
            fields[1],
            fields[2],
            fields[3],
            DateTime.ParseExact(fields[4], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal.Parse(fields[5], CultureInfo.InvariantCulture),
            fields[6]
        );
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> fields = [];
        bool inQuotes = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
                inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes)
            {
                fields.Add(line[start..i].Trim('"'));
                start = i + 1;
            }
        }
        fields.Add(line[start..].Trim('"'));
        return fields;
    }
}
