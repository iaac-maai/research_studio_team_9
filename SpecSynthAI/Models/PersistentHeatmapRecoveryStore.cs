using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SpecSynthAI.Models
{
    /// <summary>
    /// Stores persistent recovery information inside the Revit document.
    /// A duplicated view acts as the source of the original graphic overrides.
    /// </summary>
    public static class PersistentHeatmapRecoveryStore
    {
        private static readonly Guid SchemaGuid =
            new Guid("B7A5E2F4-8C31-4D92-A671-5E3F9B2C1048");

        private const string SchemaName =
            "SpecSynthAI_HeatmapRecovery";

        private const string FieldName =
            "RecoveryData";

        public sealed class RecoveryRecord
        {
            public ElementId OriginalViewId { get; }
            public ElementId RecoveryViewId { get; }
            public ElementId StorageId { get; }
            public IReadOnlyList<ElementId> ElementIds { get; }

            public RecoveryRecord(
                ElementId originalViewId,
                ElementId recoveryViewId,
                ElementId storageId,
                IReadOnlyList<ElementId> elementIds)
            {
                OriginalViewId = originalViewId;
                RecoveryViewId = recoveryViewId;
                StorageId = storageId;
                ElementIds = elementIds;
            }
        }

        private sealed class RecoveryData
        {
            public long OriginalViewId { get; set; }
            public long RecoveryViewId { get; set; }
            public List<long> ElementIds { get; set; } = new();
        }

        /// <summary>
        /// Creates a duplicate of the original view before heatmap changes.
        /// Stores its ID and affected element IDs inside the Revit document.
        ///
        /// MUST be called inside an active Revit transaction.
        /// </summary>
        public static RecoveryRecord CreateRecoverySnapshot(
            Document doc,
            View originalView,
            IEnumerable<ElementId> elementIds)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (originalView == null)
                throw new ArgumentNullException(nameof(originalView));

            if (!doc.IsModifiable)
                throw new InvalidOperationException(
                    "CreateRecoverySnapshot must run inside a transaction.");

            if (originalView.IsTemplate)
                throw new InvalidOperationException(
                    "A view template cannot be used for heatmap recovery.");

            if (TryGetRecoveryRecord(doc, out _))
                throw new InvalidOperationException(
                    "A persistent heatmap recovery record already exists.");

            List<ElementId> ids = elementIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                throw new InvalidOperationException(
                    "No elements were supplied for recovery.");

            // Duplicate the view before applying any heatmap overrides.
            ElementId recoveryViewId =
                originalView.Duplicate(ViewDuplicateOption.Duplicate);

            View? recoveryView =
                doc.GetElement(recoveryViewId) as View;

            if (recoveryView == null)
                throw new InvalidOperationException(
                    "Could not create the recovery view.");

            RecoveryData data = new RecoveryData
            {
                OriginalViewId = originalView.Id.Value,
                RecoveryViewId = recoveryView.Id.Value,
                ElementIds = ids.Select(id => id.Value).ToList()
            };

            string serializedData =
                JsonSerializer.Serialize(data);

            Schema schema = GetOrCreateSchema();

            DataStorage storage = DataStorage.Create(doc);

            Entity entity = new Entity(schema);

            Field field = schema.GetField(FieldName);

            entity.Set(field, serializedData);

            storage.SetEntity(entity);

            return new RecoveryRecord(
                originalView.Id,
                recoveryView.Id,
                storage.Id,
                ids);
        }

        /// <summary>
        /// Retrieves persistent recovery information from the document.
        /// </summary>
        public static bool TryGetRecoveryRecord(
            Document doc,
            out RecoveryRecord? record)
        {
            record = null;

            if (doc == null)
                return false;

            Schema? schema = Schema.Lookup(SchemaGuid);

            if (schema == null)
                return false;

            Field? field = schema.GetField(FieldName);

            if (field == null)
                return false;

            List<DataStorage> storageElements =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(DataStorage))
                    .Cast<DataStorage>()
                    .ToList();

            foreach (DataStorage storage in storageElements)
            {
                Entity entity = storage.GetEntity(schema);

                if (!entity.IsValid())
                    continue;

                try
                {
                    string serializedData =
                        entity.Get<string>(field);

                    RecoveryData? data =
                        JsonSerializer.Deserialize<RecoveryData>(
                            serializedData);

                    if (data == null)
                        continue;

                    List<ElementId> ids = data.ElementIds
                        .Select(id => new ElementId(id))
                        .ToList();

                    record = new RecoveryRecord(
                        new ElementId(data.OriginalViewId),
                        new ElementId(data.RecoveryViewId),
                        storage.Id,
                        ids);

                    return true;
                }
                catch
                {
                    // Continue searching if a record is invalid.
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the recovery metadata and temporary recovery view.
        ///
        /// MUST be called inside an active Revit transaction.
        /// Call only after successful restoration.
        /// </summary>
        public static void DeleteRecoverySnapshot(
            Document doc,
            RecoveryRecord record)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (!doc.IsModifiable)
                throw new InvalidOperationException(
                    "DeleteRecoverySnapshot must run inside a transaction.");

            if (doc.GetElement(record.StorageId) != null)
                doc.Delete(record.StorageId);

            if (doc.GetElement(record.RecoveryViewId) != null)
                doc.Delete(record.RecoveryViewId);
        }

        private static Schema GetOrCreateSchema()
        {
            Schema? existingSchema = Schema.Lookup(SchemaGuid);

            if (existingSchema != null)
                return existingSchema;

            SchemaBuilder builder = new SchemaBuilder(SchemaGuid);

            builder.SetSchemaName(SchemaName);
            builder.SetDocumentation(
                "Stores SpecSynth AI compliance heatmap recovery information.");

            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);

            builder.AddSimpleField(
                FieldName,
                typeof(string));

            return builder.Finish();
        }
    }
}