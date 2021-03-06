﻿using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logfile.Structured.Elements
{
    /// <summary>
    /// Represents a general log event.
    /// </summary>
    public class Event
    {
        #region Constants

        /// <summary>
        /// Gets the event identification.
        /// </summary>
        public const string Identification = "EVENT";

        /// <summary>
        /// Gets the separator for multiple records within an event.
        /// </summary>
        public const string RecordSeparator = "\x1f";

        /// <summary>
        /// Gets the visual separator for multiple records within an event.
        /// </summary>
        public const string VisualRecordSeparator = " == ";

        /// <summary>
        /// Gets a string with characters to be ignored after record separators
        /// when reading an event.
        /// </summary>
        public const string IgnoredAfterRecordSeparators = "-=#*";

        /// <summary>
        /// Gets the character for quoting values.
        /// </summary>
        public const char QuotationMark = '`';

        /// <summary>
        /// Gets the text signalling developer log events.
        /// </summary>
        public const string DeveloperFlag = "Dev";

        #endregion
    }

    /// <summary>
    /// Represents a structure element for an event.
    /// </summary>
    /// <typeparam name="TLoglevel">The loglevel type.</typeparam>
    public class Event<TLoglevel> : Event, IElement<TLoglevel>
        where TLoglevel : Enum
    {
        #region Properties

        /// <summary>
        /// Gets the log event.
        /// </summary>
        public LogEvent<TLoglevel> LogEvent { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        /// <exception cref="ArgumentNullException">Thrown if
        ///		<paramref name="logEvent"/> is null.</exception>
        public Event(LogEvent<TLoglevel> logEvent)
        {
            this.LogEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
        }

        #endregion

        #region IElement implementation

        public string Serialize(StructuredLogfileConfiguration<TLoglevel> configuration)
        {
            var eventIDs = this.LogEvent.Details.OfType<EventID>();
            var eventID = eventIDs.FirstOrDefault();
            var logfileHierarchy = this.LogEvent.Details.OfType<LogfileHierarchy>().FirstOrDefault();

            var sb = new StringBuilder();

            // Use all details. Treat event ID separately if only a single event ID is mentioned.
            var detailsEnu = this.LogEvent.Details.Where(d => !(d is EventID) || d != eventID || (d == eventID && eventID?.StringArguments?.Any() == true));
            detailsEnu = detailsEnu.Where(d => !(d is LogfileHierarchy));
            var details = detailsEnu.ToList();

            sb.Append(subSerialize(configuration, details, true));

            sb.Insert(0, $"{RecordSeparator}{(this.LogEvent.IsDeveloper ? $"{VisualRecordSeparator}{DeveloperFlag}" : "")}");
            sb.Insert(0, $"{RecordSeparator}{(eventID == null ? "" : $"{VisualRecordSeparator}{getEventIDString(eventID)}")}");
            sb.Insert(0, $"{RecordSeparator}{(logfileHierarchy == null ? "" : $"{VisualRecordSeparator}{string.Join(".", logfileHierarchy.Hierarchy.Select(h => ContentEncoding.Encode(h)))}")}");
            sb.Insert(0, $"{RecordSeparator}{VisualRecordSeparator}{this.LogEvent.Loglevel.ToString()}");
            sb.Insert(0, $"{RecordSeparator} {this.LogEvent.Time.ToIso8601String()}");
            sb.Insert(0, $"{Identification}");
            sb.Append(Constants.EntitySeparator);
            return sb.ToString();
        }

        static string getEventIDString(EventID eventID)
        {
            if (eventID == null) return null;

            var arguments = new StringBuilder();
            if (eventID.StringArguments?.Any() == true)
            {
                for (int i = 0; i < eventID.StringArguments.Count(); i++)
                {
                    if (i > 0) arguments.Append(", ");
                    var name = eventID.ParameterNames?.Count() >= i + 1 ? eventID.ParameterNames.ElementAt(i) : null;
                    var value = eventID.StringArguments.ElementAt(i);
                    if (value != null) value = ContentEncoding.Encode(value);
                    if (!string.IsNullOrWhiteSpace(name))
                        arguments.Append($"{name}=");
                    arguments.Append($"{Constants.QuotationMark}{value}{Constants.QuotationMark}");
                }

                arguments.Insert(0, " {");
                arguments.Append("}");
            }

            return $"{string.Join(".", eventID.NumberChain)} {string.Join(".", eventID.TextChain)}{arguments.ToString()}";
        }

        /// <summary>
        /// Get serialized strings for elements like sensitive data which need to be serialized
        /// separately.
        /// </summary>
        /// <param name="configuration">The structured logfile configuration.</param>
        /// <param name="details">The details to sub-serialize.</param>
        /// <param name="firstLogEventDetailToCome">Whether there had already an event
        ///		be put out.</param>
        /// <returns>The serialized elements.</returns>
        static string subSerialize(StructuredLogfileConfiguration<TLoglevel> configuration, List<object> details, bool firstLogEventDetailToCome)
        {
            var sb = new StringBuilder();
            if (details.Any())
            {
                while (details.Any())
                {
                    var detail = details.First();
                    details.RemoveAt(0);

                    // Initialise with fall-back output.
                    var id = detail.GetType().ToString();
                    var content = detail.ToString();

                    // Handle special case for sensitive/encrypting log event details.
                    if ((detail is Sensitive sensitive) && (sensitive.IsSensitive))
                    {
                        // Collect details to be treated by a sub-call. Find matching end
                        // of sensitive data block indicator and allow nested sensitive
                        // data blocks.
                        var subDetails = new List<object>();
                        var nestedSensitiveBlocks = 0;
                        foreach (var subDetail in details)
                        {
                            if (subDetail is Sensitive subDetailSensitive)
                            {
                                if (!subDetailSensitive.IsSensitive)
                                {
                                    // Remove end of sensitive data block indicator from
                                    // details handled by this call.
                                    details.Remove(subDetail);

                                    if (nestedSensitiveBlocks == 0)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        --nestedSensitiveBlocks;
                                    }
                                }
                                else
                                {
                                    ++nestedSensitiveBlocks;
                                }
                            }

                            subDetails.Add(subDetail);
                        }

                        // Remove all details handled by the following call from this call.
                        foreach (var subDetail in subDetails)
                        {
                            details.Remove(subDetail);
                        }

                        // Have sub-details handled by a sub-call.
                        try
                        {
                            content = sensitive.Serialize(sensitive.Encrypt<TLoglevel>(configuration.SensitiveSettings, Encoding.UTF8.GetBytes(subSerialize(configuration, subDetails, false))));
                        }
                        catch
                        {
                            // Encrytion failed. Ignore that details and continue with next insensitive data.
                            continue;
                        }
                    }

                    // Determine formatter and overwrite content and ID.
                    var formatter = configuration.LogEventDetailFormatters.Values
                                        .Where(f => f.SupportedLogEventDetailsTypes.Contains(detail.GetType())
                                                        || (detail.GetType().IsGenericType
                                                            && f.SupportedLogEventDetailsTypes.Contains(detail.GetType().BaseType))).FirstOrDefault();
                    if (formatter != null)
                    {
                        content = formatter.Format(detail);
                        id = formatter.ID;
                    }

                    // Generate final output.
                    sb.Append($"{RecordSeparator}{(firstLogEventDetailToCome ? VisualRecordSeparator : Constants.Indent)}{QuotationMark}{ContentEncoding.Encode(id)}{QuotationMark}={QuotationMark}{ContentEncoding.Encode(content)}{QuotationMark}");
                    sb.Append(Constants.NewLine);

                    // All other log event details must be written in new lines.
                    firstLogEventDetailToCome = false;
                }
            }
            else
            {
                sb.Append(Constants.NewLine);
            }

            return sb.ToString();
        }

        #endregion
    }
}
