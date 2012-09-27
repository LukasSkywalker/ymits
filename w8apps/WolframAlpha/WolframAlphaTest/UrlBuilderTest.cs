using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using WolframAlpha;
using System.Text.RegularExpressions;

namespace WolframAlphaTest
{
    [TestClass]
    public class UrlBuilderTest
    {
        UrlBuilder ub;

        [TestInitialize]
        public void SetUp() {
            ub = new UrlBuilder();
            ub.AddAppId("APP_ID").AddInput("WHO_DAT_GIRL");
        }

        [TestMethod]
        public void TestAssumption()
        {
            String s1 = ub.AddAssumption("Word").Build();
            StringAssert.Contains(s1, "Word");
            StringAssert.Contains(s1, "APP_ID");
            StringAssert.Contains(s1, "WHO_DAT_GIRL");

            String s2 = ub.AddAssumption("Number").Build();
            StringAssert.Contains(s2, "Number");
            StringAssert.DoesNotMatch(s2, new Regex("Word"));
            StringAssert.Contains(s2, "APP_ID");
            StringAssert.Contains(s2, "WHO_DAT_GIRL");

            String s3 = ub.AddState("MORE_DIGITS", "42", 2).Build();
            StringAssert.Contains(s3, "Number");
            StringAssert.DoesNotMatch(s3, new Regex("Word"));
            StringAssert.Contains(s3, "APP_ID");
            StringAssert.Contains(s3, "WHO_DAT_GIRL");
            StringAssert.Contains(s3, "WHO_DAT_GIRL");
        }
    }
}
