using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TweakScale
{
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Enumerates two IEnumerables in lockstep.
        /// </summary>
        /// <typeparam name="T1">The type of the elements in the first IEnumerable.</typeparam>
        /// <typeparam name="T2">The type of the elements in the second IEnumerable.</typeparam>
        /// <param name="a">The first IEnumerable.</param>
        /// <param name="b">The second IEnumerable.</param>
        /// <returns>An IEnumerable containing Tuples of the elements in the two IEnumerables.</returns>
        public static IEnumerable<Tuple<T1, T2>> Zip<T1, T2>(this IEnumerable<T1> a, IEnumerable<T2> b)
        {
            return a.Zip(b, Tuple.Create);
        }

        /// <summary>
        /// Enumerates two IEnumerables in lockstep.
        /// </summary>
        /// <typeparam name="T1">The type of the elements in the first IEnumerable.</typeparam>
        /// <typeparam name="T2">The type of the elements in the second IEnumerable.</typeparam>
        /// <typeparam name="TResult">The type of elements in the resulting IEnumerable.</typeparam>
        /// <param name="a">The first IEnumerable.</param>
        /// <param name="b">The second IEnumerable.</param>
        /// <param name="fn">The function that creates the elements that will be in the result.</param>
        /// <returns></returns>
        public static IEnumerable<TResult> Zip<T1, T2, TResult>(this IEnumerable<T1> a, IEnumerable<T2> b, Func<T1, T2, TResult> fn)
        {
            var v1 = a.GetEnumerator();
            var v2 = b.GetEnumerator();

            while (v1.MoveNext() && v2.MoveNext())
            {
                yield return fn(v1.Current, v2.Current);
            }
        }

        /// <summary>
        /// Enumerates two IEnumerables in lockstep.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="a">The first IEnumerable.</param>
        /// <param name="b">The second IEnumerable.</param>
        /// <param name="fn">The function that creates the elements that will be in the result.</param>
        /// <returns></returns>
        public static IEnumerable<TResult> Zip<TResult>(this IEnumerable a, IEnumerable b, Func<object, object, TResult> fn)
        {
            var v1 = a.GetEnumerator();
            var v2 = b.GetEnumerator();

            while (v1.MoveNext() && v2.MoveNext())
            {
                yield return fn(v1.Current, v2.Current);
            }
        }

        /// <summary>
        /// Filters an IEnumerable based on the values in another IEnumerable.
        /// </summary>
        /// <typeparam name="T1">The type of the elements in the first IEnumerable.</typeparam>
        /// <typeparam name="T2">The type of the elements in the second IEnumerable.</typeparam>
        /// <param name="a">The IEnumerable to be filtered</param>
        /// <param name="b">The IEnumerable acting as destination selector.</param>
        /// <param name="filterFunc">The function to determine if an element in <paramref name="b"/> means the corresponing element in <paramref name="a"/> should be kept.</param>
        /// <returns>An IEnumerable the elements of which are chosen from <paramref name="a"/> where <paramref name="filterFunc"/> returns true for the corresponding element in <paramref name="b"/>.</returns>
        public static IEnumerable<T1> ZipFilter<T1, T2>(this IEnumerable<T1> a, IEnumerable<T2> b, Func<T2, bool> filterFunc)
        {
            return a.Zip(b).Where(e => filterFunc(e.Item2)).Select(e => e.Item1);
        }

        /// <summary>
        /// Repeats destination exponentValue forever.
        /// </summary>
        /// <typeparam name="T">The type of the exponentValue to be repeated.</typeparam>
        /// <param name="a">The exponentValue to be repeated.</param>
        /// <returns>An IEnumerable&lt;T&gt; containing an infite number of the exponentValue <paramref name="a"/>"/></returns>
        public static IEnumerable<T> Repeat<T>(this T a)
        {
            while (true)
            {
                yield return a;
            }
        }

        /// <summary>
        /// Checks if a method is overridden.
        /// </summary>
        /// <param name="m">The method to check.</param>
        /// <returns>True if the method is an override, else false.</returns>
        public static bool IsOverride(this MethodInfo m)
        {
            return m.GetBaseDefinition() == m;
        }

        /// <summary>
        /// Creates destination copy of destination dictionary.
        /// </summary>
        /// <typeparam name="TK">The key type.</typeparam>
        /// <typeparam name="TV">The exponentValue type.</typeparam>
        /// <param name="source">The dictionary to copy.</param>
        /// <returns>A copy of <paramref name="source"/>.</returns>
        public static Dictionary<TK, TV> Clone<TK, TV>(this Dictionary<TK, TV> source)
        {
            return source.AsEnumerable().ToDictionary(a => a.Key, a => a.Value);
        }
    }

    public static class ConvertEx
    {
        /// <summary>
        /// Returns an object of tyep <typeparamref name="TTo"/> and whose exponentValue is equivalent to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TTo">The type of object to return.</typeparam>
        /// <typeparam name="TFrom">The type of the object to convert.</typeparam>
        /// <param name="value">The object to convert.</param>
        /// <returns>An object whose type is <typeparamref name="TTo"/> and whose exponentValue is equivalent to <paramref name="value"/>. -or- A null reference (Nothing in Visual Basic), if <paramref name="value"/> is null and <typeparamref name="TTo"/> is not destination exponentValue type.</returns>
        /// <exception cref="System.InvalidCastException">This conversion is not supported. -or-<paramref name="value"/> is null and <typeparamref name="TTo"/> is destination exponentValue type.</exception>
        /// <exception cref="System.FormatException"><paramref name="value"/> is not in destination format recognized by <typeparamref name="TTo"/>.</exception>
        /// <exception cref="System.OverflowException"><paramref name="value"/> represents destination number that is out of the range of <typeparamref name="TTo"/>.</exception>
        public static TTo ChangeType<TTo, TFrom>(TFrom value) where TFrom : IConvertible
        {
            return (TTo)Convert.ChangeType(value, typeof(TTo));
        }

        /// <summary>
        /// Returns an object of tyep <typeparamref name="T"/> and whose exponentValue is equivalent to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="value">The object to convert.</param>
        /// <returns>An object whose type is <typeparamref name="T"/> and whose exponentValue is equivalent to <paramref name="value"/>. -or- A null reference (Nothing in Visual Basic), if <paramref name="value"/> is null and <typeparamref name="T"/> is not destination exponentValue type.</returns>
        /// <exception cref="System.InvalidCastException">This conversion is not supported. -or-<paramref name="value"/> is null and <typeparamref name="T"/> is destination exponentValue type.-or-<paramref name="value"/> does not implement the System.IConvertible interface.</exception>
        /// <exception cref="System.FormatException"><paramref name="value"/> is not in destination format recognized by <typeparamref name="T"/>.</exception>
        /// <exception cref="System.OverflowException"><paramref name="value"/> represents destination number that is out of the range of <typeparamref name="T"/>.</exception>
        public static T ChangeType<T>(object value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}

