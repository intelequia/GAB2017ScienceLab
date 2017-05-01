using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GAB.BatchServer.API.Exceptions;

namespace GAB.BatchServer.API.Common.Lab
{
    /// <summary>
    /// Lab outputs parser
    /// </summary>
    public static class OutputParser
    {
        /// <summary>
        /// Parses the content of an output file into a SeligaParsedOutput
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task<SeligaParsedOutput> ParseAsync(string content)
        {
            var reader = new StringReader(content);
            var header = await reader.ReadLineAsync();
            // Split by whitespace
            // http://stackoverflow.com/questions/6111298/best-way-to-specify-whitespace-in-a-string-split-operation
            var columns = header.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); 

            if (columns.Length != SeligaOutput.SELIGA_COLUMNS)
            {
                throw new OutputParsingException(
                    $"The output doesn't contain the expected format (column count {columns.Length})");
            }

            var result = new SeligaParsedOutput();
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line))
            {
                throw new OutputParsingException("The output file has no results");
            }
            while (!string.IsNullOrEmpty(line))
            {
                var values = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != SeligaOutput.SELIGA_COLUMNS)
                {
                    throw new OutputParsingException(
                        $"The output doesn't contain the expected format (row {result.Outputs.Count + 1}, columns {line.Length})");
                }
                var output = new SeligaOutput()
                {
                    FunObs = values[0],
                    FunTest = values[1],
                    FunResp = values[2],
                    Score = double.Parse(values[SeligaOutput.SELIGA_COLUMNS - 1], CultureInfo.InvariantCulture)
                };
                if (output.Score < 0.0 || output.Score > 100.0)
                {
                    throw new OutputParsingException($"The output score is out of range: {output.Score}");
                }
                result.Outputs.Add(output);
                line = await reader.ReadLineAsync();
            }

            result.MaxScore = result.Outputs.Max(x => x.Score);
            result.AvgScore = Math.Round(result.Outputs.Average(x => x.Score), 4);
            result.TotalScore = Math.Round(result.Outputs.Sum(x => x.Score), 4);

            return result;
        }
    }
}
