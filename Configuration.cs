using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveboxToZabbix;
[Serializable]
public class Configuration
{
    public static Configuration Instance { get; set; } = new Configuration();

    public string LiveboxUrl { get; set; } = "http://192.168.1.1";
    public string LiveboxLogin { get; set; } = "admin";
    public string LiveboxPassword { get; set; } = "P@ssw0rd";

    public string ZabbixServer { get; set; } = "192.168.1.100";
    public string ZabbixHostToPopulate { get; set; } = "adsl";
    public int ZabbixServerPort { get; set; } = 10051;
}
