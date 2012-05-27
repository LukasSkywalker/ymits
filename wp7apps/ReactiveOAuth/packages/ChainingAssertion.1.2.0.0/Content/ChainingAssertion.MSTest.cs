﻿/*--------------------------------------------------------------------------
 * Chaining Assertion for MSTest
 * ver 1.2.0.0 (Mar. 3rd, 2011)
 *
 * created and maintained by neuecc <ils@neue.cc - @neuecc on Twitter>
 * licensed under Microsoft Public License(Ms-PL)
 * http://chainingassertion.codeplex.com/
 *--------------------------------------------------------------------------*/

/* -- Tutorial --
 * | at first, include this file on MSTest Project.
 * 
 * | three example, "Is" overloads.
 * 
 * // This same as Assert.AreEqual(25, Math.Pow(5, 2))
 * Math.Pow(5, 2).Is(25);
 * 
 * // This same as Assert.IsTrue("foobar".StartsWith("foo") && "foobar".EndWith("bar"))
 * "foobar".Is(s => s.StartsWith("foo") && s.EndsWith("bar"));
 * 
 * // This same as CollectionAssert.AreEqual(Enumerable.Range(1,5), new[]{1, 2, 3, 4, 5})
 * Enumerable.Range(1, 5).Is(1, 2, 3, 4, 5);
 * 
 * | CollectionAssert
 * | if you want to use CollectionAssert Methods then use Linq to Objects and Is
 *
 * new[] { 1, 3, 7, 8 }.Contains(8).Is(true);
 * new[] { 1, 3, 7, 8 }.Count(i => i % 2 != 0).Is(3);
 * new[] { 1, 3, 7, 8 }.Any().Is(true);
 * new[] { 1, 3, 7, 8 }.All(i => i < 5).Is(false);
 *
 * // IsOrdered
 * var array = new[] { 1, 5, 10, 100 };
 * array.Is(array.OrderBy(x => x));
 *
 * | Other Assertions
 * 
 * // Null Assertions
 * Object obj = null;
 * obj.IsNull();             // Assert.IsNull(obj)
 * new Object().IsNotNull(); // Assert.IsNotNull(obj)
 *
 * // Not Assertion
 * "foobar".IsNot("fooooooo"); // Assert.AreNotEqual
 * new[] { "a", "z", "x" }.IsNot("a", "x", "z"); /// CollectionAssert.AreNotEqual
 *
 * // ReferenceEqual Assertion
 * var tuple = Tuple.Create("foo");
 * tuple.IsSameReferenceAs(tuple); // Assert.AreSame
 * tuple.IsNotSameReferenceAs(Tuple.Create("foo")); // Assert.AreNotSame
 *
 * // Type Assertion
 * "foobar".IsInstanceOf<string>(); // Assert.IsInstanceOfType
 * (999).IsNotInstanceOf<double>(); // Assert.IsNotInstanceOfType
 * 
 * | Advanced Collection Assertion
 * 
 * var lower = new[] { "a", "b", "c" };
 * var upper = new[] { "A", "B", "C" };
 *
 * // Comparer CollectionAssert, use IEqualityComparer<T> or Func<T,T,bool> delegate
 * lower.Is(upper, StringComparer.InvariantCultureIgnoreCase);
 * lower.Is(upper, (x, y) => x.ToUpper() == y.ToUpper());
 *
 * // or you can use Linq to Objects - SequenceEqual
 * lower.SequenceEqual(upper, StringComparer.InvariantCultureIgnoreCase).Is(true);
 * 
 * | Exception Test
 * 
 * // Exception Test(alternative of ExpectedExceptionAttribute)
 * AssertEx.Throws<ArgumentNullException>(() => "foo".StartsWith(null));
 * 
 * // return value is occured exception
 * var ex = AssertEx.Throws<InvalidOperationException>(() =>
 * {
 *     throw new InvalidOperationException("foobar operation");
 * });
 * ex.Message.Is(s => s.Contains("foobar")); // additional exception assertion
 * 
 * // must not throw any exceptions
 * AssertEx.DoesNotThrow(() =>
 * {
 *     // code
 * });
 * 
 * | Parameterized Test
 * | TestCase takes parameters and send to TestContext's Extension Method "Run".
 * 
 * [TestClass]
 * public class UnitTest
 * {
 *     public TestContext TestContext { get; set; }
 * 
 *     [TestMethod]
 *     [TestCase(1, 2, 3)]
 *     [TestCase(10, 20, 30)]
 *     [TestCase(100, 200, 300)]
 *     public void TestMethod2()
 *     {
 *         TestContext.Run((int x, int y, int z) =>
 *         {
 *             (x + y).Is(z);
 *             (x + y + z).Is(i => i < 1000);
 *         });
 *     }
 * }
 * 
 * | TestCaseSource
 * | TestCaseSource can take static field/property that types is only object[][].
 * 
 * [TestMethod]
 * [TestCaseSource("toaruSource")]
 * public void TestTestCaseSource()
 * {
 *     TestContext.Run((int x, int y, string z) =>
 *     {
 *         string.Concat(x, y).Is(z);
 *     });
 * }
 * 
 * public static object[] toaruSource = new[]
 * {
 *     new object[] {1, 1, "11"},
 *     new object[] {5, 3, "53"},
 *     new object[] {9, 4, "94"}
 * };
 * 
 * -- more details see project home --*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    #region Extensions

    public static partial class AssertEx
    {
        /// <summary>Assert.AreEqual, if T is IEnumerable then CollectionAssert.AreEqual</summary>
        public static void Is<T>(this T actual, T expected, string message = "")
        {
            if (typeof(T) != typeof(String) && typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                ((IEnumerable)actual).Cast<object>().Is(((IEnumerable)expected).Cast<object>(), message);
                return;
            }

            Assert.AreEqual(expected, actual, message);
        }

        /// <summary>Assert.IsTrue(predicate(value))</summary>
        public static void Is<T>(this T value, Expression<Func<T, bool>> predicate, string message = "")
        {
            var paramName = predicate.Parameters.First().Name;
            var msg = string.Format("{0} = {1}, {2}{3}",
                paramName, value, predicate,
                string.IsNullOrEmpty(message) ? "" : ", " + message);

            Assert.IsTrue(predicate.Compile().Invoke(value), msg);
        }

        /// <summary>CollectionAssert.AreEqual</summary>
        public static void Is<T>(this IEnumerable<T> actual, params T[] expected)
        {
            Is(actual, expected.AsEnumerable());
        }

        /// <summary>CollectionAssert.AreEqual</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string message = "")
        {
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), message);
        }

        /// <summary>CollectionAssert.AreEqual</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer, string message = "")
        {
            Is(expected, actual, comparer.Equals, message);
        }

        /// <summary>CollectionAssert.AreEqual</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, Func<T, T, bool> equalityComparison, string message = "")
        {
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), new ComparisonComparer<T>(equalityComparison), message);
        }

        /// <summary>Assert.AreNotEqual, if T is IEnumerable then CollectionAssert.AreNotEqual</summary>
        public static void IsNot<T>(this T actual, T notExpected, string message = "")
        {
            if (typeof(T) != typeof(String) && typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                ((IEnumerable)actual).Cast<object>().IsNot(((IEnumerable)notExpected).Cast<object>(), message);
                return;
            }

            Assert.AreNotEqual(notExpected, actual, message);
        }

        /// <summary>CollectionAssert.AreNotEqual</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, params T[] notExpected)
        {
            IsNot(actual, notExpected.AsEnumerable());
        }

        /// <summary>CollectionAssert.AreNotEqual</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, string message = "")
        {
            CollectionAssert.AreNotEqual(notExpected.ToArray(), actual.ToArray(), message);
        }

        /// <summary>CollectionAssert.AreNotEqual</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer, string message = "")
        {
            IsNot(expected, actual, comparer.Equals, message);
        }

        /// <summary>CollectionAssert.AreNotEqual</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> expected, Func<T, T, bool> equalityComparison, string message = "")
        {
            CollectionAssert.AreNotEqual(expected.ToArray(), actual.ToArray(), new ComparisonComparer<T>(equalityComparison), message);
        }

        /// <summary>Assert.IsNull</summary>
        public static void IsNull<T>(this T value)
        {
            Assert.IsNull(value);
        }

        /// <summary>Assert.IsNotNull</summary>
        public static void IsNotNull<T>(this T value)
        {
            Assert.IsNotNull(value);
        }

        /// <summary>Assert.AreSame</summary>
        public static void IsSameReferenceAs<T>(this T actual, T expected, string message = "")
        {
            Assert.AreSame(expected, actual, message);
        }

        /// <summary>Assert.AreNotSame</summary>
        public static void IsNotSameReferenceAs<T>(this T actual, T notExpected, string message = "")
        {
            Assert.AreNotSame(notExpected, actual, message);
        }

        /// <summary>Assert.IsInstanceOfType</summary>
        public static void IsInstanceOf<TExpected>(this object value, string message = "")
        {
            Assert.IsInstanceOfType(value, typeof(TExpected), message);
        }

        /// <summary>Assert.IsNotInstanceOfType</summary>
        public static void IsNotInstanceOf<TWrong>(this object value, string message = "")
        {
            Assert.IsNotInstanceOfType(value, typeof(TWrong), message);
        }

        /// <summary>Alternative of ExpectedExceptionAttribute</summary>
        public static T Throws<T>(Action testCode, string message = "") where T : Exception
        {
            var exception = ExecuteCode(testCode);
            var headerMsg = "Failed Throws<" + typeof(T).Name + ">.";
            var additionalMsg = string.IsNullOrEmpty(message) ? "" : ", " + message;

            if (exception == null)
            {
                var formatted = headerMsg + " No exception was thrown" + additionalMsg;
                throw new AssertFailedException(formatted);
            }
            else if (!typeof(T).Equals(exception.GetType()))
            {
                var formatted = string.Format("{0} Catched:{1}{2}", headerMsg, exception.GetType().Name, additionalMsg);
                throw new AssertFailedException(formatted);
            }

            return (T)exception;
        }

        /// <summary>does not throw any exceptions</summary>
        public static void DoesNotThrow(Action testCode, string message = "")
        {
            var exception = ExecuteCode(testCode);
            if (exception != null)
            {
                var formatted = string.Format("Failed DoesNotThrow. Catched:{0}{1}", exception.GetType().Name, string.IsNullOrEmpty(message) ? "" : ", " + message);
                throw new AssertFailedException(formatted);
            }
        }

        /// <summary>execute action and return exception when catched otherwise return null</summary>
        private static Exception ExecuteCode(Action testCode)
        {
            try
            {
                testCode();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>EqualityComparison to IComparer Converter for CollectionAssert</summary>
        private class ComparisonComparer<T> : IComparer
        {
            readonly Func<T, T, bool> comparison;

            public ComparisonComparer(Func<T, T, bool> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(object x, object y)
            {
                return (comparison != null)
                    ? comparison((T)x, (T)y) ? 0 : -1
                    : object.Equals(x, y) ? 0 : -1;
            }
        }
    }

    #endregion

    #region TestCase

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Parameters { get; private set; }

        /// <summary>parameters provide to TestContext.Run.</summary>
        public TestCaseAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseSourceAttribute : Attribute
    {
        public string SourceName { get; private set; }

        /// <summary>point out static field/property name. source must be object[][].</summary>
        public TestCaseSourceAttribute(string sourceName)
        {
            SourceName = sourceName;
        }
    }

    public static class TestContextExtensions
    {
        private static IEnumerable<object[]> GetParameters(TestContext context)
        {
            var classType = Type.GetType(context.FullyQualifiedTestClassName);
            var method = classType.GetMethod(context.TestName);

            var testCase = method
                .GetCustomAttributes(typeof(TestCaseAttribute), false)
                .Cast<TestCaseAttribute>()
                .Select(x => x.Parameters);

            var testCaseSource = method
                .GetCustomAttributes(typeof(TestCaseSourceAttribute), false)
                .Cast<TestCaseSourceAttribute>()
                .SelectMany(x =>
                {
                    var p = classType.GetProperty(x.SourceName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    var val = (p != null)
                        ? p.GetValue(null, null)
                        : classType.GetField(x.SourceName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);

                    return ((object[])val).Cast<object[]>();
                });

            return testCase.Concat(testCaseSource);
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1>(this TestContext context, Action<T1> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2>(this TestContext context, Action<T1, T2> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3>(this TestContext context, Action<T1, T2, T3> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4>(this TestContext context, Action<T1, T2, T3, T4> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5>(this TestContext context, Action<T1, T2, T3, T4, T5> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6>(this TestContext context, Action<T1, T2, T3, T4, T5, T6> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10],
                    (T12)parameters[11]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10],
                    (T12)parameters[11],
                    (T13)parameters[12]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10],
                    (T12)parameters[11],
                    (T13)parameters[12],
                    (T14)parameters[13]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10],
                    (T12)parameters[11],
                    (T13)parameters[12],
                    (T14)parameters[13],
                    (T15)parameters[14]
                    );
            }
        }
        
        /// <summary>Run Parameterized Test marked by TestCase Attribute.</summary>
        public static void Run<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this TestContext context, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> assertion)
        {
            foreach (var parameters in GetParameters(context))
            {
                assertion(
                    (T1)parameters[0],
                    (T2)parameters[1],
                    (T3)parameters[2],
                    (T4)parameters[3],
                    (T5)parameters[4],
                    (T6)parameters[5],
                    (T7)parameters[6],
                    (T8)parameters[7],
                    (T9)parameters[8],
                    (T10)parameters[9],
                    (T11)parameters[10],
                    (T12)parameters[11],
                    (T13)parameters[12],
                    (T14)parameters[13],
                    (T15)parameters[14],
                    (T16)parameters[15]
                    );
            }
        }
    }

    #endregion
}
