// we must define a custom runner

using System.Diagnostics;
using Template.libs;

namespace Template
{

    public class Runner
    {
        static void Main()
        {
            SocketStrapper strapper = new SocketStrapper();
            strapper.Start(); 
            // Set up events to be declared dynamically based on the config??
            strapper.ConnectEvents(typeof(Form), new Dictionary<string, dynamic>()
            {
                {
                    "wowButton", "ExampleEvent"
                }
            });
            // Now start Python GTK GUI
            string fullPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var path = Path.Combine(fullPath, "libs/pygui/gui");
            var p = new ProcessStartInfo(path);
            p.Arguments = $"--config={fullPath}/conf/config.json";
            Process.Start(p);
        }
    }
}