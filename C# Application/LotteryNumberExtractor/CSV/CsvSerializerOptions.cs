using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LottoNumbers.CSV
{
    /// <summary>
    /// Provides options to be used with <see cref="CsvSerializer"/>.
    /// </summary>
    public abstract partial class CsvSerializerOptions
    {

        /// <summary>
        /// How the data is aligned.
        /// </summary>
        /// <remarks>Not implemented in <see cref="CsvSerializer.Serialize{T}(T[])"/> or <see cref="CsvSerializer.Deserialize{T}(string)"/>.
        /// At the moment it only works as if <see cref="Columns"/> was selected.</remarks>
        public enum DataAlignment
        {
            /// <summary>
            /// Each column contains data for an object.
            /// </summary>
            Columns,
            /// <summary>
            /// Each row contains data for an object.
            /// </summary>
            Rows
        };


        /// <summary>
        /// The characters that are used to determine the end of a cell.
        /// </summary>
        [Flags]
        public enum Separator {
            Comma = 1, 
            Space = 2, 
            Semicolon = 4, 
            Tab = 8, 
            Custom = 16 
        };

        /// <summary>
        /// The character that is used to determine a string.
        /// Any text wrapped with this, will not be separated by the <see cref="Separator"/> into multiple cells.
        /// </summary>
        public enum TextDelimeter { DoubleQuotes = '\"', SingleQuotes = '\'' };

        /// <summary>
        /// How <see cref="DateTime"/> is converted to a string during serialization.
        /// </summary>
        public enum DateTimeFormat
        {
            /// <summary>
            /// <see cref="DateTime.ToString"/>
            /// </summary>
            DateTime,
            /// <summary>
            /// <see cref="DateTime.ToShortDateString"/>
            /// </summary>
            ShortDate,
            /// <summary>
            /// <see cref="DateTime.ToShortTimeString"/>
            /// </summary>
            ShortTime,
            /// <summary>
            /// <see cref="DateTime.ToLongDateString"/>
            /// </summary>
            LongDate,
            /// <summary>
            /// <see cref="DateTime.ToLongTimeString"/>
            /// </summary>
            LongTime,
            /// <summary>
            /// Will display date like "Jan 1, 2021"
            /// </summary>
            BasicDate,
            /// <summary>
            /// Will pass <see cref="customSeparator"/> to <see cref="DateTime.ToString(string?)"/>. 
            /// If <see cref="customDateTimeFormatProvider"/> is not null, it will use <see cref="DateTime.ToString(string?, IFormatProvider?)"/>.
            /// </summary>
            Custom
        };

        /// <inheritdoc cref="TextDelimeter"/>
        public TextDelimeter textDelimeter { get; set; } = TextDelimeter.DoubleQuotes;


        /// <summary>
        /// Depending on <see cref="DataAlignment"/>, does the first row or column contain the names of the properties.
        /// </summary>
        public virtual bool headerContainsPropertyNames { get; set; }


        /// <summary>
        /// Gets the <see cref="TextDelimeter"/> as a <see cref="char"/>.
        /// </summary>
        /// <returns></returns>
        public char GetTextDelimeter()
        {
            if (textDelimeter == TextDelimeter.DoubleQuotes)
                return '\"';
            else
                return '\'';
        }

        /// <summary>
        /// Determines how a <see cref="DateTime"/> will be formatted during serialization.
        /// </summary>
        public DateTimeFormat datetimeFormat { get; set; }

        /// <summary>
        /// The custom format string for <see cref="DateTime.ToString(string?)"/> during serialization.
        /// </summary>
        public string customDateTimeFormat { get; set; }

        /// <summary>
        /// The custom format provider for <see cref="DateTime.ToString(string?, IFormatProvider?)"/> during serialization.
        /// </summary>
        public IFormatProvider customDateTimeFormatProvider { get; set; }




        /// <summary>
        /// Provides options to be used with <see cref="CsvSerializer"/>
        /// </summary>
        public class Deserializing : CsvSerializerOptions
        {
            /// <summary>
            /// The characters that determine the end of a cell.
            /// </summary>
            public Separator separators { get; set; } =  Separator.Space;


            /// <summary>
            /// The custom characters that determines the end of a cell.
            /// </summary>
            private char[] _customSeparators;

            /// <summary>
            /// The custom character that determines the end of a cell.
            /// </summary>
            public char customSeparator
            {
                get
                {
                    if (_customSeparators != null && _customSeparators.Length > 0)
                    {
                        return _customSeparators[0];
                    }

                    return '\0';
                }
                set
                {
                    _customSeparators = new char[] { value };
                }
            }

            /// <summary>
            /// The custom characters that determine the end of a cell.
            /// </summary>
            public char[] customSeparators
            {
                get
                {
                    return _customSeparators;
                }
                set
                {
                    _customSeparators = value;
                }
            }

            /// <summary>
            /// Depending on <see cref="DataAlignment"/>, does the first row or column contain the names of the properties.
            /// If false, it will capture the data in order, and store it in the property by index.
            /// If true, it will match the property to the name in the header, and store the data in the property with the name in the header.
            /// </summary>
            /// <remarks>At the moment, the only thing this does is remove the first text line during deserialization. It does not sort property values by name.</remarks>
            public override bool headerContainsPropertyNames { get; set; } = true;

            /// <summary>
            /// Converts the <see cref="separators"/> into a <see cref="char[]"/>.
            /// </summary>
            /// <returns>The <see cref="separators"/> converted into a <see cref="char[]"/>.</returns>
            public char[] GetSeparators()
            {
                List<char> chars = new List<char>();

                //if (separators.HasFlag(Separator.Comma))
                //    chars.Add(',');

                if (separators.HasFlag(Separator.Semicolon))
                    chars.Add(';');

                if (separators.HasFlag(Separator.Space))
                    chars.Add(' ');

                if (separators.HasFlag(Separator.Tab))
                    chars.Add('\t');

                if (separators.HasFlag(Separator.Custom))
                    chars.AddRange(customSeparators);

                return chars.ToArray();
            }

        }



        public class Serializing : CsvSerializerOptions
        {

            /// <summary>
            /// Select which properties will be serialized.
            /// Leave null to serialize all properties.
            /// </summary>
            public PropertyInfo[] properties { get; set; }


            /// <summary>
            /// The character that will be inserted into the string to determine where a cell stops.
            /// </summary>
            public Separator separator { get; set; } = Separator.Space;

            //private char _customSeparator;
            public char customSeparator { get; set; }


            /// <summary>
            /// Depending on <see cref="DataAlignment"/>, does the first row or column contain the names of the properties.
            /// If false, the property names will not be added as the header.
            /// If true, the property name will be added to the top of the file as a header.
            /// </summary>
            public override bool headerContainsPropertyNames { get; set; } = true;


            /// <summary>
            /// Converts the <see cref="separator"/> into the actual character.
            /// </summary>
            /// <returns>The <see cref="separator"/> converted to <see cref="char"/>.</returns>
            public char GetSeparator()
            {
                switch (separator)
                {
                    default:
                    //case Separator.Comma: return ',';
                    case Separator.Space: return ' ';
                    case Separator.Semicolon: return ';';
                    case Separator.Tab: return '\t';
                    case Separator.Custom: return customSeparator;
                }
            }

        }

        //public bool quotedFieldAsText;
        //public bool detectSpecialNumbers;
    }
}
