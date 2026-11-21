using KleeneStar.Model;
using KleeneStar.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KleeneStar.Seeder
{
    /// <summary>
    /// Seeds the demonstration and test data of KleeneStar.
    /// </summary>
    public static partial class DemoSeeder
    {
        /// <summary>
        /// The field types that hold a date or a span of dates.
        /// </summary>
        private static readonly FieldType[] DateTypes = [FieldType.Date, FieldType.Calendar, FieldType.DateRange, FieldType.CalendarRange];

        /// <summary>
        /// Returns a seed value for a date-typed field: a single date in the round-trip format for
        /// <see cref="FieldType.Date"/> and <see cref="FieldType.Calendar"/>, a span
        /// <c>yyyy-MM-dd - yyyy-MM-dd</c> for <see cref="FieldType.DateRange"/> and
        /// <see cref="FieldType.CalendarRange"/> (the format <c>ControlFormInputValueDateRange</c>
        /// reads).
        /// </summary>
        /// <param name="type">The field type.</param>
        /// <param name="created">The creation time of the object the value belongs to.</param>
        /// <param name="sequence">A per-class sequence index that varies the dates; the dates stay
        /// within about six weeks of the creation.</param>
        /// <returns>The value, or <see langword="null"/> for a type that holds no date.</returns>
        private static string DateSample(FieldType type, DateTime created, int sequence)
        {
            var day = created.Date.AddDays(sequence % 21);

            return type switch
            {
                FieldType.Date or FieldType.Calendar => created.AddDays(sequence % 45).ToString("o", CultureInfo.InvariantCulture),
                FieldType.DateRange or FieldType.CalendarRange => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    + " - " + day.AddDays(3 + sequence % 12).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => null
            };
        }

        /// <summary>
        /// Replaces the text placeholders an earlier seed wrote into date-typed fields with dates.
        /// </summary>
        /// <remarks>
        /// Before 2026-11-18 the seed derived values of the calendar and date range types from the
        /// text fallback, <c>"{field} for {key}"</c>, which no date control can read. This runs on
        /// every start and touches only a value that is exactly that placeholder, so nothing a user
        /// entered changes. The commit history is corrected alongside: every change of the same
        /// object and field that carries the placeholder carries the date instead, so a replay
        /// reproduces the stored value.
        /// </remarks>
        /// <param name="db">The database context.</param>
        private static void RepairDateValues(KleeneStarDbContext db)
        {
            var fields = db.Fields
                .Where(x => DateTypes.Contains(x.FieldType))
                .Select(x => new { x.Id, x.Name, x.FieldType })
                .ToList()
                .ToDictionary(x => x.Id);

            if (fields.Count == 0)
            {
                return;
            }

            var fieldIds = fields.Keys.ToList();
            var candidates = db.Values
                .Where(x => fieldIds.Contains(x.FieldId) && x.Data != null && x.Data.Contains(" for "))
                .ToList();

            if (candidates.Count == 0)
            {
                return;
            }

            var objectIds = candidates.Select(x => x.ObjectId).Distinct().ToList();
            var objects = db.Objects
                .Where(x => objectIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Key, x.Created })
                .ToList()
                .ToDictionary(x => x.Id);

            // the replaced placeholders by (object, field), for the history below
            var replaced = new Dictionary<(Guid ObjectId, Guid FieldId), (string Placeholder, string Date)>();

            foreach (var value in candidates)
            {
                if (!fields.TryGetValue(value.FieldId, out var field) || !objects.TryGetValue(value.ObjectId, out var entity))
                {
                    continue;
                }

                var placeholder = $"{field.Name} for {entity.Key}";

                if (!string.Equals(value.Data, placeholder, StringComparison.Ordinal))
                {
                    continue;
                }

                var date = DateSample(field.FieldType, entity.Created, KeyNumber(entity.Key));

                value.Data = date;
                replaced[(value.ObjectId, value.FieldId)] = (placeholder, date);
            }

            if (replaced.Count == 0)
            {
                return;
            }

            var touched = replaced.Keys.Select(x => x.ObjectId).Distinct().ToList();
            var commits = db.Commits
                .Where(x => touched.Contains(x.ObjectId))
                .Select(x => new { x.Id, x.ObjectId })
                .ToList()
                .ToDictionary(x => x.Id, x => x.ObjectId);
            var commitIds = commits.Keys.ToList();

            foreach (var change in db.Changes.Where(x => commitIds.Contains(x.CommitId) && x.FieldId != null && fieldIds.Contains(x.FieldId.Value)).ToList())
            {
                if (!replaced.TryGetValue((commits[change.CommitId], change.FieldId!.Value), out var swap))
                {
                    continue;
                }

                if (change.OldValue == swap.Placeholder)
                {
                    change.OldValue = swap.Date;
                }

                if (change.NewValue == swap.Placeholder)
                {
                    change.NewValue = swap.Date;
                }
            }
        }

        /// <summary>
        /// Returns the number of an object key (<c>DEV-8000</c> is 8000), used to vary the dates
        /// of a repaired value the way a sequence varies the seeded ones.
        /// </summary>
        /// <param name="key">The object key.</param>
        /// <returns>The number, or 0 without one.</returns>
        private static int KeyNumber(string key)
        {
            var digits = new string((key ?? string.Empty).Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());

            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;
        }
    }
}
