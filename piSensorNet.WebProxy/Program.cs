using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace piSensorNet.WebProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = Environment.CurrentDirectory + @"\..\piSensorNet.Web\project.json";
            var config = new ConfigurationBuilder().AddJsonFile(path).Build();
            var webCommand = //new[] { @"""C:\Users\kiziu\.dnx\runtimes\dnx-clr-win-x86.1.0.0-rc1-update2\bin\Microsoft.Dnx.Host.dll""" }.Concat
                (config["commands:web"].Split(' ').Concat(new[] {"run"})).ToArray();
            
            var assembly = Assembly.LoadFile(@"C:\Users\kiziu\.dnx\runtimes\dnx-clr-win-x86.1.0.0-rc1-update2\bin\Microsoft.Dnx.ApplicationHost.dll");
            var type = assembly.GetType("Microsoft.Dnx.ApplicationHost.Program", true);
            var method = type.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            var methodDelegate = (Func<string[], int>)Delegate.CreateDelegate(typeof(Func<string[], int>), method);

            var result = methodDelegate(webCommand);
        }
    }
}
