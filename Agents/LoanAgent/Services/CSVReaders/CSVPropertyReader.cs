using FinWise.LoanAgent.Models;

namespace FinWise.LoanAgent.Services.CSVReaders;

public abstract class PropertyPriceCsvReader<T> where T : PropertyPriceBaseRecord
{
    public IEnumerable<T> ReadFromCsv(string path)
    {
        using StreamReader reader = new(path);
        bool isHeader = true;
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (isHeader) { isHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(line)) continue;
            yield return ParseLine(line);
        }
    }

    protected abstract T ParseLine(string line);
}
