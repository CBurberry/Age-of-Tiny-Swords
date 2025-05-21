using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

/// <summary>
/// Bunch of static helper functions to be used at editor/runtime. In no particular order.
/// </summary>
namespace RuntimeStatics
{
    public static class BitwiseHelpers
    {
        /// <summary>
        /// Count the number of bits set in a numeric type (useful for bitwise flag count check)
        /// Source: https://stackoverflow.com/questions/677204/counting-the-number-of-flags-set-on-an-enumeration/42557518#42557518
        /// </summary>
        /// <param name="lValue">enum value to check</param>
        /// <returns>The number of set bits (excluding 0)</returns>
        public static int GetSetBitCount(long lValue)
        {
            int iCount = 0;

            //Loop the value while there are still bits
            while (lValue != 0)
            {
                //Remove the end bit
                lValue = lValue & (lValue - 1);

                //Increment the count
                iCount++;
            }

            //Return the count
            return iCount;
        }
    }

    public static class CoroutineUtilities
    {
        public static IEnumerator WaitForSecondsWithInterrupt(float seconds, [DisallowNull] Func<bool> interruptPredicate)
        {
            float timer = seconds;
            while (timer > 0f && !interruptPredicate())
            {
                yield return null;
                timer -= Time.deltaTime;
            }
        }

        public static IEnumerator WaitForSecondsWithInterrupt<T>(float seconds, [DisallowNull] Func<T, bool> interruptPredicate, T arg)
        {
            float timer = seconds;
            while (timer > 0f && !interruptPredicate(arg))
            {
                yield return null;
                timer -= Time.deltaTime;
            }
        }
    }

    //Source: https://stackoverflow.com/questions/5542816/printing-flags-enum-as-separate-flags
    public static class EnumExtensions
    {
        public static IEnumerable<Enum> GetFlags(this Enum value)
        {
            return GetFlags(value, Enum.GetValues(value.GetType()).Cast<Enum>().ToArray());
        }

        public static IEnumerable<Enum> GetIndividualFlags(this Enum value)
        {
            return GetFlags(value, GetFlagValues(value.GetType()).ToArray());
        }

        private static IEnumerable<Enum> GetFlags(Enum value, Enum[] values)
        {
            ulong bits = Convert.ToUInt64(value);
            List<Enum> results = new List<Enum>();
            for (int i = values.Length - 1; i >= 0; i--)
            {
                ulong mask = Convert.ToUInt64(values[i]);
                if (i == 0 && mask == 0L)
                    break;
                if ((bits & mask) == mask)
                {
                    results.Add(values[i]);
                    bits -= mask;
                }
            }
            if (bits != 0L)
                return Enumerable.Empty<Enum>();
            if (Convert.ToUInt64(value) != 0L)
                return results.Reverse<Enum>();
            if (bits == Convert.ToUInt64(value) && values.Length > 0 && Convert.ToUInt64(values[0]) == 0L)
                return values.Take(1);
            return Enumerable.Empty<Enum>();
        }

        private static IEnumerable<Enum> GetFlagValues(Type enumType)
        {
            ulong flag = 0x1;
            foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
            {
                ulong bits = Convert.ToUInt64(value);
                if (bits == 0L)
                    //yield return value;
                    continue; // skip the zero value
                while (flag < bits) flag <<= 1;
                if (flag == bits)
                    yield return value;
            }
        }
    }
}
