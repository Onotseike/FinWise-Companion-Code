using System.Globalization;

using FinWise.LoanAgent.Models;

namespace FinWise.LoanAgent.Services.CSVReaders;

public class UKPropertyPriceCsvReader : PropertyPriceCsvReader<UKPropertyPriceRecord>
{
    protected override UKPropertyPriceRecord ParseLine(string line)
    {
        List<string> fields = ParseCsvLine(line);
        return new UKPropertyPriceRecord(
            fields[0],
            fields[3],
            fields[4][0],
            fields[5][0],
            fields[6][0],
            fields[7],
            fields[8],
            fields[9],
            fields[10],
            fields[11],
            fields[12],
            fields[14],
            fields[15],
            DateTime.Parse(fields[2], CultureInfo.InvariantCulture),
            decimal.Parse(fields[1], CultureInfo.InvariantCulture),
            fields[13]
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
