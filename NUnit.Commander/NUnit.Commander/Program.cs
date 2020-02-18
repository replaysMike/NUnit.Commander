using AnyConsole;
using System;
using System.Drawing;

namespace NUnit.Commander
{
    class Program
    {
        static void Main(string[] args)
        {
            var console = new ExtendedConsole();
            var myDataContext = new ConsoleDataContext();
            console.Configure(config =>
            {
                config.SetStaticRow("Header", RowLocation.Top, Color.White, Color.DarkRed);
                config.SetStaticRow("SubHeader", RowLocation.Top, 1, Color.White, Color.FromArgb(30, 30, 30));
                config.SetStaticRow("Footer", RowLocation.Bottom, Color.White, Color.DarkBlue);
                config.SetLogHistoryContainer(RowLocation.Top, 2);
                config.SetDataContext(myDataContext);
                config.SetUpdateInterval(TimeSpan.FromMilliseconds(100));
                config.SetMaxHistoryLines(1000);
                config.SetHelpScreen(new DefaultHelpScreen());
                config.SetQuitHandler((consoleInstance) => {
                    // do something special when quit occurs
                });
            });
            console.OnKeyPress += Console_OnKeyPress; ;
            console.WriteRow("Header", "NUnit Commander", ColumnLocation.Left, Color.Yellow); // show text on the left
            console.WriteRow("Header", Component.Time, ColumnLocation.Right); // show the time on the right
            console.WriteRow("SubHeader", "Real-Time Test Monitor", ColumnLocation.Left, Color.FromArgb(60, 60, 60));
            console.Start();

            using (var commander = new Commander(console))
            {
                commander.ConnectIpcServer();
                commander.WaitForCompletion();
            }
            console.Close();
            console.Dispose();
        }

        private static void Console_OnKeyPress(KeyPressEventArgs e)
        {
            
        }
    }
}
