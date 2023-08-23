using System.Collections;
using System.Data;
using System.Xml.Serialization;

namespace LiveboxToZabbix;

internal class Program
{
    static void ExceptionHandler(Action act)
    {
        try
        {
            act.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    static void Main(string[] args)
    {
        XmlSerializer ser = new XmlSerializer(typeof(Configuration));
        using (StreamWriter tw = new StreamWriter("config-sample.xml", false))
            ser.Serialize(tw, Configuration.Instance);
        if (File.Exists("config.xml"))
            using (StreamReader sr = new StreamReader("config.xml"))
                Configuration.Instance = ser.Deserialize(sr) as Configuration;
        else
        {
            Configuration.Instance.ZabbixHostToPopulate = Environment.GetEnvironmentVariable("LTZ_ZabbixHostToPopulate") ?? Configuration.Instance.ZabbixHostToPopulate;
            Configuration.Instance.ZabbixServerPort = int.TryParse(Environment.GetEnvironmentVariable("LTZ_ZabbixServerPort"), out int tmpVal) ? tmpVal : Configuration.Instance.ZabbixServerPort;
            Configuration.Instance.ZabbixServer = Environment.GetEnvironmentVariable("LTZ_ZabbixServer") ?? Configuration.Instance.ZabbixServer;
            Configuration.Instance.LiveboxLogin = Environment.GetEnvironmentVariable("LTZ_LiveboxLogin") ?? Configuration.Instance.LiveboxLogin;
            Configuration.Instance.LiveboxPassword = Environment.GetEnvironmentVariable("LTZ_LiveboxPassword") ?? Configuration.Instance.LiveboxPassword;
            Configuration.Instance.LiveboxUrl = Environment.GetEnvironmentVariable("LTZ_LiveboxUrl") ?? Configuration.Instance.LiveboxUrl;
        }

        Livebox4WS livebox = new Livebox4WS(Configuration.Instance.LiveboxLogin, Configuration.Instance.LiveboxPassword, Configuration.Instance.LiveboxUrl);

        while (true)
        {
            livebox.PlayAuthAsync().GetAwaiter().GetResult();
            for (int i = 0; i < 30 * 4; i++)
            {
                ExceptionHandler(() => livebox.RetrieveADSLStatsAsync().GetAwaiter().GetResult());
                ExceptionHandler(() => livebox.RetrieveADSLStats2Async().GetAwaiter().GetResult());
                ExceptionHandler(() => livebox.RetrieveWanStatusAsync().GetAwaiter().GetResult());

                Thread.Sleep(15 * 1000);
            }
        }
    }
}
