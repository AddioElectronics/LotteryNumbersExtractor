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
        /// Gets an array of <see cref="PropertyInfoExtensions"/> from the header of the CSV string(<paramref name="line1"/>).
        /// These represent the values that were serialized, and are used during deserialization to set the values to the correct property.
        /// </summary>
        /// <param name="line1">The header of the CSV file. Line 1</param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns>An array of <see cref="PropertyInfo"/> whos names were present in the header.</returns>
        private static PropertyInfo[] ParsePropertiesExistingInHeader<T>(string line1, CsvSerializerOptions.Deserializing options)
        {
            Type type = typeof(T);
            string[] propertyNames = SplitLine(line1, options);
            List<PropertyInfo> properties = new List<PropertyInfo>();

            foreach (string name in propertyNames)
            {
                PropertyInfo property = type.GetProperty(name);

                if (property == null) continue;

                properties.Add(property);
            }

            if (properties.Count == 0) return null;

            return properties.ToArray();
        }


        /// <summary>
        /// Parse <see cref="DateTime"/> using <paramref name="options"/> during <see cref="Deserialize{T}(string)"/>.
        /// </summary>
        /// <param name="s">The <see cref="DateTime"/> represented as a <see cref="string"/>.</param>
        /// <param name="options">The <see cref="CsvSerializerOptions"/> that contain the format used to serialize the <see cref="DateTime"/>.</param>
        /// <returns>The <see cref="DateTime"/> that was parsed from <paramref name="s"/>.</returns>
        private static DateTime ParseDateTime(string s, CsvSerializerOptions options)
        {
            switch (options.datetimeFormat)
            {
                case CsvSerializerOptions.Serializing.DateTimeFormat.DateTime:
                case CsvSerializerOptions.Serializing.DateTimeFormat.ShortDate:
                case CsvSerializerOptions.Serializing.DateTimeFormat.ShortTime:
                case CsvSerializerOptions.Serializing.DateTimeFormat.LongDate:
                case CsvSerializerOptions.Serializing.DateTimeFormat.LongTime:
                    return DateTime.Parse(s, options.customDateTimeFormatProvider);
                case CsvSerializerOptions.Serializing.DateTimeFormat.BasicDate:
                    return DateTime.ParseExact(s, "MMM dd, yyyy", options.customDateTimeFormatProvider);
                case CsvSerializerOptions.Serializing.DateTimeFormat.Custom:
                    return DateTime.ParseExact(s,
                        options.customDateTimeFormat ?? (options.customDateTimeFormat == "" ? "MMM dd, yyyy" : options.customDateTimeFormat),
                        options.customDateTimeFormatProvider);
            }

            throw new Exception("Could not parse DateTime");
        }


    }
}
