/*
 Copyright (C) 2021 Ralf Konrad Eckel

 This file is part of QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/

 QuantLib is free software: you can redistribute it and/or modify it
 under the terms of the QuantLib license.  You should have received a
 copy of the license along with this program; if not, please email
 <quantlib-dev@lists.sf.net>. The license is also available online at
 <http://quantlib.org/license.shtml>.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

#pragma warning disable CS1718 // Comparison made to same variable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ql = QuantLib;

namespace TimesTest
{
    class Times
    {
        class TestCaseException : Exception { }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                DateTime startTime = DateTime.Now;

                RunTestCases();

                DateTime endTime = DateTime.Now;
                TimeSpan delta = endTime - startTime;
                Console.WriteLine();
                Console.WriteLine("Run completed in {0} s", delta.TotalSeconds);
                Console.WriteLine();
            }
            catch (TestCaseException exc)
            {
                Console.Error.WriteLine(exc);
                throw;
            }
        }

        private static void TestCase(bool result, bool expected)
        {
            if (result != expected) throw new TestCaseException();
        }

        private static void RunTestCases()
        {
            Action<string, IEnumerable<ql.Period>> writePeriods = (heading, periods2Write) =>
            {
                Console.Write($"  {heading}:  ");
                foreach (var item in periods2Write)
                {
                    var itemAsString = item != null ? item.ToString() : "null";
                    Console.Write($"{itemAsString}  ");
                }
                Console.WriteLine();
            };

            var tenorNull = null as ql.Period;
            var tenor91D = new ql.Period("91D");
            var tenor03M = new ql.Period("03M");
            var tenor06M = new ql.Period("06M");
            var tenor12M = new ql.Period("12M");
            var tenor01Y = new ql.Period("01Y");
            var tenor02Y = new ql.Period("02Y");

            var periods = new List<ql.Period>() { tenor01Y, tenorNull, tenor02Y, tenor06M, tenor03M };

            Console.WriteLine("Testing sorting of a list.");
            writePeriods("Before sorting", periods);

            periods.Sort();

            writePeriods(" After sorting", periods);

            TestCase(periods[0] == tenorNull, expected: true);
            TestCase(periods[1] == tenor03M, expected: true);
            TestCase(periods[2] == tenor06M, expected: true);
            TestCase(periods[3] == tenor01Y, expected: true);
            TestCase(periods[4] == tenor02Y, expected: true);


            #region test Period.CompareTo(Period)

            Console.WriteLine("test Period.CompareTo(Period)");

            TestCase(tenor12M.CompareTo(tenorNull) > 0, expected: true);
            TestCase(tenor12M.CompareTo(tenor03M) > 0, expected: true);
            TestCase(tenor12M.CompareTo(tenor06M) > 0, expected: true);
            TestCase(tenor12M.CompareTo(tenor01Y) == 0, expected: true);
            TestCase(tenor01Y.CompareTo(tenor01Y) == 0, expected: true);
            TestCase(tenor12M.CompareTo(tenor02Y) < 0, expected: true);

            #endregion

            #region test Period == Period

            Console.WriteLine("test Period == Period");

            TestCase(tenorNull == null, expected: true);
            TestCase(null == tenorNull, expected: true);
            TestCase(tenorNull == tenor12M, expected: false);
            TestCase(tenor12M == null, expected: false);
            TestCase(tenor12M == tenor12M, expected: true);
            TestCase(tenor12M == tenor01Y, expected: true);
            TestCase(tenor12M == tenor06M, expected: false);
            TestCase(tenor06M == tenor12M, expected: false);

            #endregion

            #region test Period != Period

            Console.WriteLine("test Period != Period");

            TestCase(tenorNull != null, expected: false);
            TestCase(null != tenorNull, expected: false);
            TestCase(tenorNull != tenor12M, expected: true);
            TestCase(tenor12M != null, expected: true);
            TestCase(tenor12M != tenor12M, expected: false);
            TestCase(tenor12M != tenor01Y, expected: false);
            TestCase(tenor12M != tenor06M, expected: true);

            #endregion

            #region test Period < Period

            Console.WriteLine("test Period < Period");

            TestCase(tenorNull < null, expected: false);
            TestCase(null < tenorNull, expected: false);
            TestCase(tenorNull < tenor12M, expected: true);
            TestCase(tenor12M < null, expected: false);
            TestCase(tenor12M < tenor12M, expected: false);
            TestCase(tenor12M < tenor01Y, expected: false);
            TestCase(tenor12M < tenor06M, expected: false);
            TestCase(tenor06M < tenor12M, expected: true);

            #endregion

            #region test Period <= Period

            Console.WriteLine("test Period <= Period");

            TestCase(tenorNull <= null, expected: true);
            TestCase(null <= tenorNull, expected: true);
            TestCase(tenorNull <= tenor12M, expected: true);
            TestCase(tenor12M <= null, expected: false);
            TestCase(tenor12M <= tenor12M, expected: true);
            TestCase(tenor12M <= tenor01Y, expected: true);
            TestCase(tenor12M <= tenor06M, expected: false);
            TestCase(tenor06M <= tenor12M, expected: true);

            #endregion

            #region test Period > Period

            Console.WriteLine("test Period > Period");

            TestCase(tenorNull > null, expected: false);
            TestCase(null > tenorNull, expected: false);
            TestCase(tenorNull > tenor12M, expected: false);
            TestCase(tenor12M > null, expected: true);
            TestCase(tenor12M > tenor12M, expected: false);
            TestCase(tenor12M > tenor01Y, expected: false);
            TestCase(tenor12M > tenor06M, expected: true);
            TestCase(tenor06M > tenor12M, expected: false);

            #endregion

            #region test Period >= Period

            Console.WriteLine("test Period >= Period");

            TestCase(tenorNull >= null, expected: true);
            TestCase(null >= tenorNull, expected: true);
            TestCase(tenorNull >= tenor12M, expected: false);
            TestCase(tenor12M >= null, expected: true);
            TestCase(tenor12M >= tenor12M, expected: true);
            TestCase(tenor12M >= tenor01Y, expected: true);
            TestCase(tenor12M >= tenor06M, expected: true);
            TestCase(tenor06M >= tenor12M, expected: false);

            #endregion

            Console.WriteLine("test Period.ToString()");
            TestCase(tenor01Y.ToString() == tenor12M.ToString(), expected: true);

            Console.WriteLine("test Period.GetHashCode()");
            TestCase(tenor01Y.GetHashCode() == tenor12M.GetHashCode(), expected: true);

            Console.WriteLine("test that uncomparable periods throw");
            Func<bool> compare91Dversus03MthrowsApplicationException = () =>
            {
                bool hasThrown = false;
                try
                {
                    tenor91D.CompareTo(tenor03M);
                }
                catch (System.ApplicationException)
                {
                    hasThrown = true;
                }
                return hasThrown;
            };

            TestCase(compare91Dversus03MthrowsApplicationException(), expected: true);
        }
    }
}

#pragma warning restore CS1718 // Comparison made to same variable
