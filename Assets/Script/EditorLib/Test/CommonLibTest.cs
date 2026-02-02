using System;
using System.Linq;
using NUnit.Framework;
using Script.CommonLib;
using Script.CommonLib.Tests;

namespace Script.EditorLib.Test
{
    public class CommonLibTest
    {
        [Test]
        public void Test()
        {
            var assembly = typeof(NodeVisitorTest).Assembly;
            
            var testTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(ITest).IsAssignableFrom(type));

            var testSuccess = true;
            
            foreach (var testType in testTypes)
            {
                var testerInstance = Activator.CreateInstance(testType);
                
                if (testerInstance is not ITest tester)
                {
                    testSuccess = false;
                    LogHelper.Error("Test type is not ITest");
                    continue;
                }

                if (!tester.Test())
                {
                    LogHelper.Error($"[{tester.GetType().Name}] Test failed.");
                    testSuccess = false;
                }
                else
                {
                    LogHelper.Log($"[{tester.GetType().Name}] Test succeed.");
                }
            }
            
            Assert.IsTrue(testSuccess);
        }
    }
}
