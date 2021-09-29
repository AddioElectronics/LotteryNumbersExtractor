using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Data;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace LottoNumbers.CSV
{

    /*
     * Did not want to include third party libraries, and do not have time to write a full and proper CSV Serializer lib.
     * This is just a quick and dirty implementation to get enough functionality for it to work for this project.
     * Not recommended for use outside of this project.
     */

    /// <summary>
    /// Provides functionality to serialize objects or value types into a CSV format, and to deserialize into objects or value types.
    /// </summary>
    /// <remarks>This is a bunch of hacky bullshit, but its all I had time for. At the moment, only Object Properties will be serialized or deserialized.</remarks>
    public static partial class CsvSerializer
    {



        /*
        
        For this project it was easier to use T[]. 
        When I have the time to make a fully fledged CSV De/Serializer I will do it the "proper" way.
        There is no point wasting time when I can make something that works perfectly fine for the situation in one tenth the time.

        internal static T Deserialize<T>(string text)
        {

        }

        internal static string Serialize<T>(T value)
        {

        }
        */


        /// <summary>
        /// Deserialize a CSV formatted string into an array of objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">The string containing CSV formatted data.</param>
        /// <returns>An array of objects converted from the CSV data. </returns>
        public static T[] Deserialize<T>(string text)
        {
            return Deserialize<T>(text, new CsvSerializerOptions.Deserializing());
        }

        /// <summary>
        /// Deserialize a CSV formatted string into an array of objects, using <paramref name="options"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">The string containing CSV formatted data.</param>
        /// <param name="options">The options used to deserialize.</param>
        /// <returns>An array of objects converted from the CSV data. </returns>
        public static T[] Deserialize<T>(string text, CsvSerializerOptions.Deserializing options)
        {
            List<T> tList = new List<T>();
            object tList_Lock = new object();

            string[] lines = text.Split(Environment.NewLine);

            Type type = typeof(T);

            PropertyInfo[] properties = options.headerContainsPropertyNames ? ParsePropertiesExistingInHeader<T>(lines[0], options) : type.GetProperties();

            for (int i = options.headerContainsPropertyNames ? 1 : 0; i < lines.Length; i++)
            {
                if (lines[i] == "") continue;

                object tObj = (T)Activator.CreateInstance(typeof(T));

                string[] values = SplitLine(lines[i], options);

                for (int p = 0; p < properties.Length; p++)
                {
                    Type pt = properties[p].PropertyType;

                    object value = null;

                    if (values[p].ToUpper() == "NULL" || values[p] == "")
                        continue;


                    if (pt == typeof(string))
                    {
                        value = values[p].Replace(options.textDelimeter.ToString(), String.Empty);
                    }
                    else if (pt == typeof(DateTime))
                    {
                        values[p] = values[p].Replace(options.textDelimeter.ToString(), String.Empty);
                        value = ParseDateTime(values[p], options);
                    }
                    else if (pt.IsArray)
                    {
                        //Because LotteryNumbers only has arrays that contain ints, that is all we will handle.
                        values[p] = values[p].Replace("\"", String.Empty);
                        string[] ints = values[p].Split(',');

                        int[] ar = new int[ints.Length];
                        for (int a = 0; a < ints.Length; a++)
                        {
                            ar[a] = int.Parse(ints[a]);
                        }
                        value = ar;
                    }
                    else if (pt.IsValueType)
                    {
                        if (pt == typeof(int))
                        {
                            value = int.Parse(values[p]);
                        }
                        else if (pt == typeof(decimal))
                        {
                            value = decimal.Parse(values[p]);
                        }

                    }
                    else if (pt.IsClass)
                    {
                        //Do nothing for now.
                    }

                    properties[p].SetValue(tObj, value);

                }
                tList.Add((T)tObj);
            }

            return tList.ToArray();
        }

        /// <summary>
        /// Converts an array of objects and their data into a string in the CSV format.
        /// </summary>
        /// <param name="value">The array of objects.</param>
        /// <param name="options">The options used while serializing.</param>
        /// <returns>A string in CSV format, containing the data from the objects in <paramref name="value"/>.</returns>
        public static string Serialize<T>(T[] value)
        {
            return Serialize(value, new CsvSerializerOptions.Serializing { textDelimeter = CsvSerializerOptions.TextDelimeter.DoubleQuotes, separator = CsvSerializerOptions.Separator.Space, headerContainsPropertyNames = true });
        }

        /// <summary>
        /// Converts an array of objects and their data into a string in the CSV format, using <see cref="CsvSerializerOptions"/>.
        /// </summary>
        /// <param name="value">The array of objects.</param>
        /// <param name="options">The options used while serializing.</param>
        /// <returns>A string in CSV format, containing the data from the objects in <paramref name="value"/>.</returns>
        public static string Serialize<T>(T[] value, CsvSerializerOptions.Serializing options)
        {
            StringBuilder stringBuilder = new StringBuilder();
            PropertyInfo[] properties = options.properties != null ? options.properties : value[0].GetType().GetProperties();

            char separator = options.GetSeparator();
            char textDelimeter = options.GetTextDelimeter();

            //A whole bunch of hacky bullshit

            if (options.headerContainsPropertyNames)
            {
                properties = ScrubUnsupportedTypes(properties);

                foreach (var property in properties)
                {
                    stringBuilder.Append(property.Name);

                    //Only add separator to all items but the last.
                    if (!property.Equals(properties[properties.Length - 1]))
                        stringBuilder.Append(separator);

                    //Make thing
                    
                }
                stringBuilder.Append(Environment.NewLine);
            }

            foreach (T v in value)
            {
                foreach (var property in properties)
                {
                    //Make sure object is not null.
                    object? obj = property.GetValue(v);
                    if (obj == null)
                    {
                        //If header does not include property names, when deserializing it has a good chance of putting values in the wrong property, which will eventually if not immediately cause an exception.
                        //if (!options.headerContainsPropertyNames)
                        //{
                            //So just add NULL so the deserializer knows what cell its supposed to be parsing.
                            stringBuilder.Append("NULL");

                            //Only add separator to all items but the last.
                            if (!property.Equals(properties[properties.Length - 1]))
                                stringBuilder.Append(separator);
                        //}
                        continue;
                    }

                    Type pt = property.PropertyType;
                    if (pt == typeof(string))
                        stringBuilder.Append(textDelimeter + (string)property.GetValue(v) + textDelimeter);
                    else if (pt == typeof(DateTime))
                    {
                        stringBuilder.Append(textDelimeter);
                        DateTime dt = (DateTime)property.GetValue(v);
                        stringBuilder.Append(SerializeDateTime(dt, options));
                        stringBuilder.Append(textDelimeter);
                    }
                    else if (pt.IsArray)
                    {
                        stringBuilder.Append(textDelimeter);
                        foreach (var ar in (Array)property.GetValue(v))
                        {
                            stringBuilder.Append(ar.ToString());
                            stringBuilder.Append(',');
                        }
                        //Remove last separator
                        stringBuilder.Remove(stringBuilder.Length - 1, 1);
                        stringBuilder.Append(textDelimeter);
                    }
                    else if (pt.IsValueType && 
                        (property.PropertyType == typeof(int) || property.PropertyType == typeof(decimal)))
                    {
                        stringBuilder.Append(property.GetValue(v).ToString());
                    }
                    else if (pt.IsClass)
                    {
                        //Do nothing for now.
                    }

                    //Only add separator to all items but the last.
                    if (!property.Equals(properties[properties.Length - 1]))
                        stringBuilder.Append(separator);
                }
                stringBuilder.Append(Environment.NewLine);
            }

            return stringBuilder.ToString();
        }


       


        /// <summary>
        /// Splits the <paramref name="line"/> string using <see cref="CsvSerializerOptions.Deserializing.separators"/> characters,
        /// ignoring the characters when inside <see cref="CsvSerializerOptions.textDelimeter"/>s.
        /// </summary>
        /// <param name="line">The string to split.</param>
        /// <param name="options">The options containing the separator and text delimeter characters.</param>
        /// <returns>The <paramref name="line"/> split into separate strings.</returns>
        public static string[] SplitLine(string line, CsvSerializerOptions.Deserializing options)
        {
            List<string> split = new List<string>();

            int lastIndex = 0;
            int currentIndex = 0;

            while (lastIndex + 1 < line.Length)
            {
                if (line[lastIndex] == (char)32) lastIndex++;

                if (line[lastIndex] == (char)options.textDelimeter)
                {
                    currentIndex = line.IndexOf((char)options.textDelimeter, ++lastIndex);
                }
                else
                {
                    currentIndex = line.IndexOfAny(options.GetSeparators(), lastIndex);
                    //line.Substring(lastIndex, line.Length - lastIndex).First(x => options.GetSeparators().Contains(x))
                }
                if (currentIndex == -1)
                {
                    split.Add(line.Substring(lastIndex, line.Length - lastIndex));
                    break;
                }
                split.Add(line.Substring(lastIndex, currentIndex - lastIndex));
                lastIndex = currentIndex + 1;
            }


            return split.ToArray();
        }

    }
}





