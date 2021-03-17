using NUnit.Commander.Display;
using NUnit.Framework;

namespace NUnit.Commander.Tests.Display
{
    [TestFixture]
    public class StackTracePrettifyTests
    {
        [Test]
        public void Should_FormatStackTrace()
        {
            var stackTrace = @"at System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)
at System.Environment.get_StackTrace()
at UserQuery.RunUserAuthoredQuery() in c:\Users\johndoe\AppData\Local\Temp\LINQPad\_piwdiese\query_dhwxhm.cs:line 33
at LINQPad.ExecutionModel.ClrQueryRunner.Run()
at LINQPad.ExecutionModel.Server.RunQuery(QueryRunner runner)
at LINQPad.ExecutionModel.Server.StartQuery(QueryRunner runner)
at LINQPad.ExecutionModel.Server.<>c__DisplayClass36.<ExecuteClrQuery>b__35()
at LINQPad.ExecutionModel.Server.SingleThreadExecuter.Work()
at System.Threading.ThreadHelper.ThreadStart_Context(Object state)
at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)
at System.Threading.ThreadHelper.ThreadStart()";

            var prettyStackTrace = StackTracePrettify.Format(stackTrace, new ColorScheme(NUnit.Commander.Configuration.ColorSchemes.Default));

            Assert.NotNull(prettyStackTrace);
            // 69 for the win
            Assert.AreEqual(69, prettyStackTrace.TextFragments.Count);
            Assert.AreEqual(150, prettyStackTrace.Width);
            Assert.AreEqual(13, prettyStackTrace.Height);
        }
    }
}
