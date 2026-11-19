using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpecSynthAI.Models;

namespace SpecSynthAI.Services
{
    /// <summary>
    /// Loads and maintains document-to-product evidence links.
    /// This service does not verify document contents and does not approve Revit imports.
    /// </summary>
    public sealed class ProductEvidenceRegistryService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public EvidenceRegistryDocument Load(string registryPath)
        {
            if (string.IsNullOrWhiteSpace(registryPath))
                throw new ArgumentException("A registry path is required.", nameof(registryPath));

            if (!File.Exists(registryPath))
                return CreateEmptyRegistry();

            string json = File.ReadAllText(registryPath);
            var document = JsonSerializer.Deserialize<EvidenceRegistryDocument>(json, JsonOptions);

            if (document == null)
                throw new InvalidDataException("The evidence registry JSON could not be read.");

            document.EvidenceRecords ??= new List<EvidenceRecord>();
            document.RegistryMetadata ??= new EvidenceRegistryMetadata();

            EvidenceValidationResult validation = Validate(document);
            if (!validation.IsStructurallyValid)
                throw new InvalidDataException(
                    "Evidence registry validation failed:" + Environment.NewLine +
                    string.Join(Environment.NewLine, validation.Errors));

            return document;
        }

        public void Save(string registryPath, EvidenceRegistryDocument document)
        {
            if (string.IsNullOrWhiteSpace(registryPath))
                throw new ArgumentException("A registry path is required.", nameof(registryPath));
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            EvidenceValidationResult validation = Validate(document);
            if (!validation.IsStructurallyValid)
                throw new InvalidDataException(
                    "Evidence registry validation failed:" + Environment.NewLine +
                    string.Join(Environment.NewLine, validation.Errors));

            string? directory = Path.GetDirectoryName(registryPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            document.RegistryMetadata.UpdatedAtUtc = DateTimeOffset.UtcNow;
            string json = JsonSerializer.Serialize(document, JsonOptions);

            string temporaryPath = registryPath + ".tmp";
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(registryPath))
                File.Replace(temporaryPath, registryPath, null);
            else
                File.Move(temporaryPath, registryPath);
        }

        public void AddOrUpdate(EvidenceRegistryDocument document, EvidenceRecord record)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(record.EvidenceId))
                throw new ArgumentException("EvidenceId is required.", nameof(record));

            int index = document.EvidenceRecords.FindIndex(
                x => string.Equals(x.EvidenceId, record.EvidenceId, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                document.EvidenceRecords[index] = record;
            else
                document.EvidenceRecords.Add(record);
        }

        public IReadOnlyList<EvidenceRecord> GetForProduct(
            EvidenceRegistryDocument document,
            string productId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(productId)) return Array.Empty<EvidenceRecord>();

            return document.EvidenceRecords
                .Where(x => string.Equals(x.ProductId, productId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DocumentType)
                .ThenBy(x => x.EvidenceId)
                .ToList();
        }

        public EvidenceValidationResult Validate(EvidenceRegistryDocument document)
        {
            var result = new EvidenceValidationResult();

            if (document == null)
            {
                result.Errors.Add("Registry document is null.");
                return result;
            }

            if (document.RegistryMetadata == null)
                result.Errors.Add("Registry metadata is missing.");

            if (document.EvidenceRecords == null)
            {
                result.Errors.Add("Evidence records collection is missing.");
                return result;
            }

            var duplicateIds = document.EvidenceRecords
                .Where(x => !string.IsNullOrWhiteSpace(x.EvidenceId))
                .GroupBy(x => x.EvidenceId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string id in duplicateIds)
                result.Errors.Add($"Duplicate evidence_id: {id}");

            for (int i = 0; i < document.EvidenceRecords.Count; i++)
            {
                EvidenceRecord? record = document.EvidenceRecords[i];
                string prefix = $"Evidence record #{i + 1}";

                if (record == null)
                {
                    result.Errors.Add($"{prefix} is null.");
                    continue;
                }

                Require(record.EvidenceId, "evidence_id", prefix, result);
                Require(record.ProductId, "product_id", prefix, result);
                Require(record.Manufacturer, "manufacturer", prefix, result);

                string[] documentTypes =
                {
                    "technical_datasheet", "epd", "certificate",
                    "manufacturer_document", "test_report", "other"
                };
                if (!documentTypes.Contains(record.DocumentType))
                    result.Errors.Add($"{prefix}: unsupported document_type '{record.DocumentType}'.");

                string[] validityStatuses =
                    { "unknown", "current", "expired", "superseded", "not_applicable" };
                if (!validityStatuses.Contains(record.ValidityStatus))
                    result.Errors.Add($"{prefix}: invalid validity_status '{record.ValidityStatus}'.");

                string[] verificationStatuses =
                    { "unreviewed", "reviewed", "verified", "rejected" };
                if (!verificationStatuses.Contains(record.VerificationStatus))
                    result.Errors.Add($"{prefix}: invalid verification_status '{record.VerificationStatus}'.");

                if (record.ValidUntil.HasValue && record.IssueDate.HasValue &&
                    record.ValidUntil.Value < record.IssueDate.Value)
                    result.Errors.Add($"{prefix}: valid_until is earlier than issue_date.");

                if (string.IsNullOrWhiteSpace(record.DocumentFilename) &&
                    string.IsNullOrWhiteSpace(record.SourceUrl))
                    result.Warnings.Add($"{prefix}: neither a document filename nor source URL is recorded.");

                if (record.VerificationStatus == "verified" &&
                    string.IsNullOrWhiteSpace(record.ReviewNotes))
                    result.Warnings.Add($"{prefix}: marked verified without review notes.");

                if (record.SupportedProperties == null)
                    result.Errors.Add($"{prefix}: supported_properties collection is missing.");
            }

            return result;
        }

        public static EvidenceRegistryDocument CreateEmptyRegistry()
        {
            return new EvidenceRegistryDocument
            {
                RegistryMetadata = new EvidenceRegistryMetadata
                {
                    RegistryName = "SpecSynth AI Product Evidence Registry",
                    SchemaVersion = "1.0.0",
                    RegistryVersion = "1.0.0",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                EvidenceRecords = new List<EvidenceRecord>()
            };
        }

        private static void Require(
            string? value,
            string field,
            string prefix,
            EvidenceValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Errors.Add($"{prefix}: {field} is required.");
        }
    }
}
