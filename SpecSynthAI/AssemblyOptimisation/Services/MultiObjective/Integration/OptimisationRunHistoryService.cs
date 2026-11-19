using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration
{
    /// <summary>
    /// Scans the integration output directory for complete request/result pairs.
    /// It never modifies, deletes, or overwrites saved run files.
    /// </summary>
    public sealed class OptimisationRunHistoryService
    {
        private const string ProjectRoot =
            @"D:\02_LEARNING\02_Masters_IAAC_AI_Architecture\10-Term_01\03-Research Studio\SpecSynth_AI";

        private readonly string _outputDirectory;
        private readonly OptimisationJsonService _jsonService;
        private readonly List<string> _lastScanWarnings = new();

        public OptimisationRunHistoryService()
            : this(Path.Combine(
                ProjectRoot,
                "05_Research_Experiments",
                "04_Integration",
                "output"))
        {
        }

        // Internal/test-friendly constructor while keeping the production path fixed.
        public OptimisationRunHistoryService(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

            _outputDirectory = outputDirectory;
            _jsonService = new OptimisationJsonService();
        }

        public IReadOnlyList<string> LastScanWarnings => _lastScanWarnings;

        public string LastScanSummary { get; private set; } =
            "History has not been scanned yet.";

        public List<OptimisationRunHistoryItem> GetHistory()
        {
            _lastScanWarnings.Clear();
            var history = new List<OptimisationRunHistoryItem>();

            if (!Directory.Exists(_outputDirectory))
            {
                LastScanSummary = "History output directory does not exist.";
                _lastScanWarnings.Add(_outputDirectory);
                return history;
            }

            string[] requestFiles = Directory.GetFiles(
                _outputDirectory,
                "request_*.json",
                SearchOption.TopDirectoryOnly);

            int missingResultCount = 0;
            int invalidPairCount = 0;

            foreach (string requestPath in requestFiles)
            {
                string fileName = Path.GetFileName(requestPath);

                try
                {
                    string requestJson = File.ReadAllText(requestPath);
                    OptimisationRequest request =
                        _jsonService.DeserializeRequest(requestJson);

                    string requestId = request.RequestId?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        invalidPairCount++;
                        _lastScanWarnings.Add($"{fileName}: request ID is empty.");
                        continue;
                    }

                    string expectedRequestFileName = $"request_{requestId}.json";
                    if (!string.Equals(fileName, expectedRequestFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        invalidPairCount++;
                        _lastScanWarnings.Add($"{fileName}: filename does not match request ID '{requestId}'.");
                        continue;
                    }

                    string resultPath = Path.Combine(
                        _outputDirectory,
                        $"result_{requestId}.json");

                    if (!File.Exists(resultPath))
                    {
                        missingResultCount++;
                        _lastScanWarnings.Add($"{requestId}: matching result file is missing.");
                        continue;
                    }

                    OptimisationResult result = _jsonService.ReadResult(resultPath);

                    if (!string.Equals(requestId, result.RequestId, StringComparison.Ordinal))
                    {
                        invalidPairCount++;
                        _lastScanWarnings.Add($"{requestId}: request/result IDs do not match.");
                        continue;
                    }

                    if (result.Summary == null || result.Solutions == null)
                    {
                        invalidPairCount++;
                        _lastScanWarnings.Add($"{requestId}: result summary or solutions are missing.");
                        continue;
                    }

                    DateTime runDateTime = File.GetLastWriteTime(requestPath);
                    const string timestampFormat = "'SSAI-OPT-'yyyyMMdd-HHmmss";

                    if (DateTime.TryParseExact(
                        requestId,
                        timestampFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedDateTime))
                    {
                        runDateTime = parsedDateTime;
                    }

                    history.Add(new OptimisationRunHistoryItem
                    {
                        RequestId = requestId,
                        RunDateTime = runDateTime,
                        Status = string.IsNullOrWhiteSpace(result.Status) ? "Unknown" : result.Status,
                        CandidateCount = result.Summary.CandidateCount,
                        ParetoSolutionCount = result.Summary.ParetoSolutionCount,
                        MatchesExhaustiveReference = result.Summary.MatchesExhaustiveReference,
                        RequestFilePath = requestPath,
                        ResultFilePath = resultPath
                    });
                }
                catch (Exception ex)
                {
                    invalidPairCount++;
                    _lastScanWarnings.Add($"{fileName}: {ex.Message}");
                }
            }

            // Detect result files that do not have a corresponding request file.
            string[] resultFiles = Directory.GetFiles(
                _outputDirectory,
                "result_*.json",
                SearchOption.TopDirectoryOnly);

            foreach (string resultPath in resultFiles)
            {
                string resultFileName = Path.GetFileName(resultPath);
                string requestId = Path.GetFileNameWithoutExtension(resultFileName)
                    .Substring("result_".Length);
                string requestPath = Path.Combine(_outputDirectory, $"request_{requestId}.json");

                if (!File.Exists(requestPath))
                    _lastScanWarnings.Add($"{resultFileName}: matching request file is missing.");
            }

            var orderedHistory = history
                .OrderByDescending(item => item.RunDateTime)
                .ThenByDescending(item => item.RequestId, StringComparer.Ordinal)
                .ToList();

            LastScanSummary =
                $"{orderedHistory.Count} complete run(s) found. " +
                $"{requestFiles.Length} request file(s) scanned; " +
                $"{missingResultCount} missing result pair(s); " +
                $"{invalidPairCount} invalid pair(s).";

            return orderedHistory;
        }

        public OptimisationResult LoadResult(OptimisationRunHistoryItem historyItem)
        {
            if (historyItem == null)
                throw new ArgumentNullException(nameof(historyItem));

            if (string.IsNullOrWhiteSpace(historyItem.ResultFilePath))
                throw new InvalidDataException("Result file path is missing.");

            string fullOutputDirectory = Path.GetFullPath(_outputDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string fullResultPath = Path.GetFullPath(historyItem.ResultFilePath);

            if (!fullResultPath.StartsWith(fullOutputDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Selected result file is outside the configured history directory.");

            OptimisationResult result = _jsonService.ReadResult(fullResultPath);

            if (!string.Equals(result.RequestId, historyItem.RequestId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Selected history record does not match its result file.");
            }

            return result;
        }
    }
}
