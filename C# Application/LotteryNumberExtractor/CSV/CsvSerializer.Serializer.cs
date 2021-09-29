using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LottoNumbers.CSV
{
    public static partial class CsvSerializer
    {

        /// <summary>
        /// Serialize a <see cref="DateTime"/> using <see cref="CsvSerializerOptions.Serializing"/> <paramref name="options"/>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to serialize.</param>
        /// <param name="options">The options to use.</param>
        /// <returns>A string containing the serialized form of <see cref="DateTime"/>. Or null if the <paramref name="options"/> were invalid.</returns>
        private static string SerializeDateTime(DateTime dateTime, CsvSerializerOptions.Serializing options)
        {
            switch (options.datetimeFormat)
            {
                case CsvSerializerOptions.Serializing.DateTimeFormat.DateTime:
                    return dateTime.ToString();
                case CsvSerializerOptions.Serializing.DateTimeFormat.ShortDate:
                    return dateTime.ToShortDateString();
                case CsvSerializerOptions.Serializing.DateTimeFormat.ShortTime:
                    return dateTime.ToShortTimeString();
                case CsvSerializerOptions.Serializing.DateTimeFormat.LongDate:
                    return dateTime.ToLongDateString();
                case CsvSerializerOptions.Serializing.DateTimeFormat.LongTime:
                    return dateTime.ToLongTimeString();
                case CsvSerializerOptions.Serializing.DateTimeFormat.BasicDate:
                    return dateTime.ToString("MMM dd, yyyy");
                case CsvSerializerOptions.Serializing.DateTimeFormat.Custom:
                    if (options.customDateTimeFormatProvider != null)
                        return dateTime.ToString(options.customDateTimeFormat ?? (options.customDateTimeFormat == "" ? "MMM dd, yyyy" : options.customDateTimeFormat), options.customDateTimeFormatProvider);
                    else
                        return dateTime.ToString(options.customDateTimeFormat ?? (options.customDateTimeFormat == "" ? "MMM dd, yyyy" : options.customDateTimeFormat));
            }
            return null;
        }


        /// <summary>
        /// Remove the types we do not want to serialize from the <paramref name="properties"/>.
        /// The serializer will only serialize the properties in the array.
        /// </summary>
        /// <param name="properties">The properties to scrub.</param>
        /// <returns>Clean</returns>
        /// <remarks>This is some hacky bullshit because I don't have time to write a proper de/serializer.</remarks>
        private static PropertyInfo[] ScrubUnsupportedTypes(PropertyInfo[] properties)
        {
            List<PropertyInfo> accepted = new List<PropertyInfo>();

            foreach(PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(string) ||
                       property.PropertyType == typeof(DateTime) ||
                       property.PropertyType.IsArray ||
                       (property.PropertyType.IsValueType && (property.PropertyType == typeof(int) || property.PropertyType == typeof(decimal))))
                {
                    accepted.Add(property);
                }
            }
            return accepted.ToArray();
        }

    }
}
