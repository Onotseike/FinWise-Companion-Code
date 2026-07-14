using System.Globalization;

using FinWise.LoanAgent.Models;

namespace FinWise.LoanAgent.Services.CSVReaders;

public class IEPropertyPriceCsvReader : PropertyPriceCsvReader<IEPropertyPriceRecord>
{
    protected override IEPropertyPriceRecord ParseLine(string line)
    {
        List<string> fields = ParseCsvLine(line);
        return new IEPropertyPriceRecord(
            fields[1].Trim('"'),
            fields[3].Trim('"'),
            fields[5].Trim('"').Equals("Yes", StringComparison.OrdinalIgnoreCase),
            fields[6].Trim('"').Equals("Yes", StringComparison.OrdinalIgnoreCase),
            fields[7].Trim('"'),
            fields.Count > 8 ? fields[8].Trim('"') : "",
            DateTime.ParseExact(fields[0].Trim('"'), "dd/MM/yyyy", CultureInfo.InvariantCulture),
            decimal.Parse(fields[4].Replace("£", "").Replace(",", "").Replace("\"", ""), CultureInfo.InvariantCulture),
            fields[2].Trim('"')
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
                fields.Add(line[start..i]);
                start = i + 1;
            }
        }
        fields.Add(line[start..]);
        return fields;
    }
}
