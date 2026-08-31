namespace Script.CommonLib.Tests
{
    internal static class TestResultVerifier
    {
        internal static bool Verify<TTest>(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{typeof(TTest).Name}] {testName} failed.");

            return result;
        }
    }
}
