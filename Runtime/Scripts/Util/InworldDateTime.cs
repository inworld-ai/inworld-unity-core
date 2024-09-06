/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

using System;
namespace Inworld
{
    [Serializable]
    public class Duration
    {
        public long seconds;
        public int nanos; // milliseconds * 1,000,000,000
    }
    public static class InworldDateTime
    {
        // YAN: In Unity we use the first format.
        //      And server will return the last 2 types of format.
        static readonly string[] s_TimeFormat = 
        {
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ssZ"
        };
        /// <summary>
        ///     Get the string of timestamp for the current UTC Time.
        /// </summary>
        public static string UtcNow => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        /// <summary>
        ///     Convert DateTime to string of timestamp.
        /// </summary>
        /// <param name="dateTime">dateTime to process.</param>
        /// <returns></returns>
        public static string ToString(DateTime dateTime) => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        /// <summary>
        ///     Convert the string of timestamp to DateTime.
        /// </summary>
        /// <param name="timestamp">string of timestamp to process</param>
        /// <returns></returns>
        public static DateTime ToDateTime(string timestamp) => DateTime.TryParseExact
        (
            timestamp, s_TimeFormat,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out DateTime outTime
        ) ? outTime : DateTime.MinValue;

        public static Duration ToDuration(float duration) => new Duration()
        {
            seconds = (long)duration,
            nanos = (int)((duration - (long)duration) * 1000000000)
        };
    }
}
