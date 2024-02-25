using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zabbix_Sender;

namespace LiveboxToZabbix;
internal class Livebox4WS
{
    public string Login { get; set; }
    public string Password { private get; set; }
    //public int Port { get; set; } //In case for remote access if debugged one day...
    public string Address { get; set; }

    private HttpClient client;
    private string authToken;

    public Livebox4WS(string login, string password, string address)
    {
        this.Login = login;
        this.Password = password;
        this.Address = address;
        this.client = new HttpClient();
    }

    internal async Task PlayAuthAsync()
    {
        // POST http://192.168.1.1/ws
        //{"service":"sah.Device.Information","method":"createContext","parameters":{"applicationName":"webui","username":"admin","password":"SuperPassword"}}
        //{"status":0,"data":{"contextID":"TheToken","username":"admin","groups":"http,admin"}}

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove("X-Context");
        client.DefaultRequestHeaders.Add("Authorization", "X-Sah-Login");

        Livebox4WSRequest req = new Livebox4WSRequest("sah.Device.Information", "createContext", new ParametersCreateContext("webui", this.Login, this.Password));
        StringContent reqContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(req), mediaType: new MediaTypeHeaderValue("application/x-sah-ws-4-call+json"));
        HttpResponseMessage res = await client.PostAsync($"{this.Address}/ws", reqContent);
        if (res.IsSuccessStatusCode)
        {
#if DEBUG
            Console.WriteLine(await res.Content.ReadAsStringAsync());
#endif
            CreateContextResp? resp = await res.Content.ReadFromJsonAsync<CreateContextResp>();
            if (resp.status != null)
            {
                this.authToken = resp.data.contextID;
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Remove("X-Context");

                client.DefaultRequestHeaders.Add("Authorization", $"X-Sah {this.authToken}");
                client.DefaultRequestHeaders.Add("X-Context", this.authToken);
            }
            else
                Console.WriteLine(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }
        else
            throw new Exception(res.StatusCode.ToString());
    }

    internal async Task RetrieveADSLStatsAsync()
    {
        // POST http://192.168.1.1/ws
        //{"service":"NeMo.Intf.dsl0","method":"getDSLStats","parameters":{}}
        //{"status":{"ReceiveBlocks":9217132,"TransmitBlocks":5844445,"CellDelin":0,"LinkRetrain":2,"InitErrors":0,"InitTimeouts":0,"LossOfFraming":0,"ErroredSecs":59,"SeverelyErroredSecs":10,"FECErrors":5582285,"ATUCFECErrors":7828,"HECErrors":0,"ATUCHECErrors":0,"CRCErrors":194,"ATUCCRCErrors":7}}
        await askWS(
            new Livebox4WSRequest("NeMo.Intf.dsl0", "getDSLStats", new Object()),
                HttpMethod.Post,
                async (res) =>
                {
                    GetDSLStatsResp resp = await res.Content.ReadFromJsonAsync<GetDSLStatsResp>();
                    ZS_Request r = new ZS_Request
                    {
                        data = new ZS_Data[]
                        {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ATUCCRCErrors", resp.status.ATUCCRCErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ATUCFECErrors", resp.status.ATUCFECErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ATUCHECErrors", resp.status.ATUCHECErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"CRCErrors", resp.status.CRCErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"CellDelin", resp.status.CellDelin.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ErroredSecs", resp.status.ErroredSecs.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"FECErrors", resp.status.FECErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"HECErrors", resp.status.HECErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"InitErrors", resp.status.InitErrors.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"InitTimeouts", resp.status.InitTimeouts.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkRetrain", resp.status.LinkRetrain.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LossOfFraming", resp.status.LossOfFraming.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ReceiveBlocks", resp.status.ReceiveBlocks.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"SeverelyErroredSecs", resp.status.SeverelyErroredSecs.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"TransmitBlocks", resp.status.TransmitBlocks.ToString()),
                        }
                    };
                    return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
                }
            );
    }
    internal async Task RetrieveADSLStats2Async()
    {
        // POST http://192.168.1.1/ws
        //{"service":"NeMo.Intf.data","method":"getMIBs","parameters":{"mibs":"dsl"}}
        //{"status":{"dsl":{"dsl0":{"DSLPlugin":"","XTMPlugin":"","DSLIPC":"/var/run/dsl","LastChangeTime":566823,"LastChange":192856,"UpstreamCurrRate":1013,"DownstreamCurrRate":6484,"LinkStatus":"Up","UpstreamMaxRate":1013,"DownstreamMaxRate":6653,"UpstreamAttenuation":277,"DownstreamAttenuation":562,"DownstreamLineAttenuation":485,"UpstreamLineAttenuation":279,"UpstreamNoiseMargin":65,"DownstreamNoiseMargin":65,"UpstreamPower":121,"DownstreamPower":191,"FirmwareVersion":"A2pvbH042x5.d27","StandardsSupported":"G.992.1_Annex_A, G.992.1_Annex_B, G.992.1_Annex_C,T1.413, T1.413i2,ETSI_101_388, G.992.2,G.992.3_Annex_A, G.992.3_Annex_B, G.992.3_Annex_C, G.992.3_Annex_I, G.992.3_Annex_J,G.992.3_Annex_M, G.992.4,G.992.5_Annex_A, G.992.5_Annex_B, G.992.5_Annex_C, G.992.5_Annex_I, G.992.5_Annex_J, G.992.5_Annex_M, G.993.1,G.993.1_Annex_A, G.993.2_Annex_A, G.993.2_Annex_B","StandardUsed":"G.992.3_Annex_A","DataPath":"Interleaved","InterleaveDepth":0,"ModulationType":"ADSL","ChannelEncapsulationType":"G.992.3_Annex_K_ATM","ModulationHint":"ADSL","AllowedProfiles":"8a, 8b, 8c, 8d, 12a, 12b, 17a, 17b, 30a, 35b, 106a, 212a, 106b","CurrentProfile":"","UPBOKLE":870}}}}
        await askWS(
        new Livebox4WSRequest("NeMo.Intf.data", "getMIBs", new ParametersGetMIBs("dsl")),
                HttpMethod.Post,
        async (res) =>
        {
            GetDSLStats2Resp resp = await res.Content.ReadFromJsonAsync<GetDSLStats2Resp>();
            ZS_Request r = new ZS_Request
            {
                data = new ZS_Data[]
                {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DSLPlugin", resp.status.dsl.dsl0.DSLPlugin.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"XTMPlugin", resp.status.dsl.dsl0.XTMPlugin.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DSLIPC", resp.status.dsl.dsl0.DSLIPC.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LastChangeTime", resp.status.dsl.dsl0.LastChangeTime.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LastChange", resp.status.dsl.dsl0.LastChange.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamCurrRate", resp.status.dsl.dsl0.UpstreamCurrRate.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamCurrRate", resp.status.dsl.dsl0.DownstreamCurrRate.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkStatus", resp.status.dsl.dsl0.LinkStatus.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamMaxRate", resp.status.dsl.dsl0.UpstreamMaxRate.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamMaxRate", resp.status.dsl.dsl0.DownstreamMaxRate.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamAttenuation", resp.status.dsl.dsl0.UpstreamAttenuation.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamAttenuation", resp.status.dsl.dsl0.DownstreamAttenuation.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamLineAttenuation", resp.status.dsl.dsl0.DownstreamLineAttenuation.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamLineAttenuation", resp.status.dsl.dsl0.UpstreamLineAttenuation.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamNoiseMargin", resp.status.dsl.dsl0.UpstreamNoiseMargin.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamNoiseMargin", resp.status.dsl.dsl0.DownstreamNoiseMargin.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamPower", resp.status.dsl.dsl0.UpstreamPower.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamPower", resp.status.dsl.dsl0.DownstreamPower.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"FirmwareVersion", resp.status.dsl.dsl0.FirmwareVersion.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"StandardsSupported", resp.status.dsl.dsl0.StandardsSupported.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"StandardUsed", resp.status.dsl.dsl0.StandardUsed.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DataPath", resp.status.dsl.dsl0.DataPath.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"InterleaveDepth", resp.status.dsl.dsl0.InterleaveDepth.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ModulationType", resp.status.dsl.dsl0.ModulationType.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ChannelEncapsulationType", resp.status.dsl.dsl0.ChannelEncapsulationType.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ModulationHint", resp.status.dsl.dsl0.ModulationHint.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"AllowedProfiles", resp.status.dsl.dsl0.AllowedProfiles.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"CurrentProfile", resp.status.dsl.dsl0.CurrentProfile.ToString()),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UPBOKLE", resp.status.dsl.dsl0.UPBOKLE.ToString()),
                }
            };
            return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
        }
            );
    }
    internal async Task RetrieveWanStatusAsync()
    {
        // POST http://192.168.1.1/ws
        //{"service":"NMC","method":"getWANStatus","parameters":{}}
        //{"status":true,"data":{"WanState":"up","LinkType":"dsl","LinkState":"up","MACAddress":"AC:XX:XX:XX:XX:XX","Protocol":"dhcp","ConnectionState":"Bound","LastConnectionError":"None","IPAddress":"2.0.0.0","RemoteGateway":"2.0.0.1","DNSServers":"80.0.0.0,81.0.0.0","IPv6Address":"2a01::1111","IPv6DelegatedPrefix":"2a01::/56"}}
        await askWS(
            new Livebox4WSRequest("NMC", "getWANStatus", new Object()),
                HttpMethod.Post,
                async (res) =>
                {
                    GetWANStatusResp resp = await res.Content.ReadFromJsonAsync<GetWANStatusResp>();
                    ZS_Request r = new ZS_Request
                    {
                        data = new ZS_Data[]
                        {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"WanState", resp?.data?.WanState??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkType", resp?.data?.LinkType??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkState", resp ?.data?.LinkState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"MACAddress", resp ?.data?.MACAddress ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Protocol", resp ?.data?.Protocol ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ConnectionState", resp ?.data?.ConnectionState??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LastConnectionError", resp ?.data?.LastConnectionError ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPAddress", resp ?.data?.IPAddress ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"RemoteGateway", resp ?.data?.RemoteGateway ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DNSServers", resp ?.data?.DNSServers ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPv6Address", resp ?.data?.IPv6Address??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPv6DelegatedPrefix", resp?.data?.IPv6DelegatedPrefix??"N/A"),
                        }
                    };
                    return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
                }
            );
    }

    private async Task askWS(Livebox4WSRequest req, HttpMethod met, Func<HttpResponseMessage, Task<ZS_Response>> OnSuccess)
    {
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(req));
        content.Headers.ContentType.MediaType = "application/x-sah-ws-4-call+json";
        HttpRequestMessage reqmsg = new HttpRequestMessage(met, $"{this.Address}/ws") { Content = content };
        //reqmsg.Headers.Add("Content-Type", "application/x-sah-ws-4-call+json");
        HttpResponseMessage res = await client.SendAsync(reqmsg);
        if (res.IsSuccessStatusCode)
        {
            try
            {
#if DEBUG
                Console.WriteLine(await res.Content.ReadAsStringAsync());
#endif
                ZS_Response zbxResp = await OnSuccess.Invoke(res);
                Console.WriteLine($"{DateTime.Now:dd:MM:yyyy-HH:mm:ss:ff} {zbxResp.response} {zbxResp.info}");
            }
            catch (Exception e)
            {
                Console.WriteLine(await res.Content.ReadAsStringAsync());
                throw;
            }
        }
        else
            throw new Exception(res.StatusCode.ToString());
    }


    [Serializable]
    public class Livebox4WSRequest
    {
        public Livebox4WSRequest(string service, string method, object parameters)
        {
            this.service = service;
            this.method = method;
            this.parameters = parameters;
        }

        public string service { get; set; }
        public string method { get; set; }
        public object parameters { get; set; }
    }
    [Serializable]
    public class ParametersCreateContext
    {
        public ParametersCreateContext(string applicationName, string username, string password)
        {
            this.applicationName = applicationName;
            this.username = username;
            this.password = password;
        }

        public string applicationName { get; set; }
        public string username { get; set; }
        public string password { get; set; }

    }
    [Serializable]
    public class ParametersGetMIBs
    {
        public ParametersGetMIBs(string mibs)
        {
            this.mibs = mibs;
        }
        public string mibs { get; set; }
    }
    [Serializable]
    public class CreateContextResp
    {
        [Serializable]
        public class CreateContexErrorsResp
        {
            public string error { get; set; }
            public string description { get; set; }
            public string info { get; set; }
        }
        [Serializable]
        public class CreateContexDatatResp
        {
            public string contextID { get; set; }
            public string username { get; set; }
            public string groups { get; set; }
        }
        public int? status { get; set; }
        public CreateContexDatatResp data { get; set; }
        public CreateContexErrorsResp errors { get; set; }
    }

    [Serializable]
    public class GetDSLStatsResp
    {
        public GetDSLStatsStatusResp status { get; set; }
        [Serializable]
        public class GetDSLStatsStatusResp
        {
            public long ReceiveBlocks { get; set; }
            public long TransmitBlocks { get; set; }
            public long CellDelin { get; set; }
            public long LinkRetrain { get; set; }
            public long InitErrors { get; set; }
            public long InitTimeouts { get; set; }
            public long LossOfFraming { get; set; }
            public long ErroredSecs { get; set; }
            public long SeverelyErroredSecs { get; set; }
            public long FECErrors { get; set; }
            public long ATUCFECErrors { get; set; }
            public long HECErrors { get; set; }
            public long ATUCHECErrors { get; set; }
            public long CRCErrors { get; set; }
            public long ATUCCRCErrors { get; set; }
        }
    }
    [Serializable]
    public class GetWANStatusResp
    {
        public bool status { get; set; }
        public GetDSLStatsStatusResp data { get; set; }
        [Serializable]
        public class GetDSLStatsStatusResp
        {
            public string WanState { get; set; }
            public string LinkType { get; set; }
            public string LinkState { get; set; }
            public string MACAddress { get; set; }
            public string Protocol { get; set; }
            public string ConnectionState { get; set; }
            public string LastConnectionError { get; set; }
            public string IPAddress { get; set; }
            public string RemoteGateway { get; set; }
            public string DNSServers { get; set; }
            public string IPv6Address { get; set; }
            public string IPv6DelegatedPrefix { get; set; }
        }
    }

    [Serializable]
    public class GetDSLStats2Resp
    {
        //{"status":{"dsl":{"dsl0":{"DSLPlugin":"","XTMPlugin":"","DSLIPC":"/var/run/dsl","LastChangeTime":566823,"LastChange":192856,"UpstreamCurrRate":1013,"DownstreamCurrRate":6484,"LinkStatus":"Up","UpstreamMaxRate":1013,"DownstreamMaxRate":6653,"UpstreamAttenuation":277,"DownstreamAttenuation":562,"DownstreamLineAttenuation":485,"UpstreamLineAttenuation":279,"UpstreamNoiseMargin":65,"DownstreamNoiseMargin":65,"UpstreamPower":121,"DownstreamPower":191,"FirmwareVersion":"A2pvbH042x5.d27","StandardsSupported":"G.992.1_Annex_A, G.992.1_Annex_B, G.992.1_Annex_C,T1.413, T1.413i2,ETSI_101_388, G.992.2,G.992.3_Annex_A, G.992.3_Annex_B, G.992.3_Annex_C, G.992.3_Annex_I, G.992.3_Annex_J,G.992.3_Annex_M, G.992.4,G.992.5_Annex_A, G.992.5_Annex_B, G.992.5_Annex_C, G.992.5_Annex_I, G.992.5_Annex_J, G.992.5_Annex_M, G.993.1,G.993.1_Annex_A, G.993.2_Annex_A, G.993.2_Annex_B","StandardUsed":"G.992.3_Annex_A","DataPath":"Interleaved","InterleaveDepth":0,"ModulationType":"ADSL","ChannelEncapsulationType":"G.992.3_Annex_K_ATM","ModulationHint":"ADSL","AllowedProfiles":"8a, 8b, 8c, 8d, 12a, 12b, 17a, 17b, 30a, 35b, 106a, 212a, 106b","CurrentProfile":"","UPBOKLE":870}}}}
        public GetDSLStatsStatusResp status { get; set; }
        [Serializable]
        public class GetDSLStatsStatusResp
        {
            public GetDSLStatsStatusDSLResp dsl { get; set; }
        }
        [Serializable]
        public class GetDSLStatsStatusDSLResp
        {
            public GetDSLStatsStatusDSL0Resp dsl0 { get; set; }
        }
        [Serializable]
        public class GetDSLStatsStatusDSL0Resp
        {
            public string DSLPlugin { get; set; }
            public string XTMPlugin { get; set; }
            public string DSLIPC { get; set; }
            public int LastChangeTime { get; set; }
            public int LastChange { get; set; }
            public int UpstreamCurrRate { get; set; }
            public int DownstreamCurrRate { get; set; }
            public string LinkStatus { get; set; }
            public int UpstreamMaxRate { get; set; }
            public int DownstreamMaxRate { get; set; }
            public int UpstreamAttenuation { get; set; }
            public int DownstreamAttenuation { get; set; }
            public int DownstreamLineAttenuation { get; set; }
            public int UpstreamLineAttenuation { get; set; }
            public int UpstreamNoiseMargin { get; set; }
            public int DownstreamNoiseMargin { get; set; }
            public int UpstreamPower { get; set; }
            public int DownstreamPower { get; set; }
            public string FirmwareVersion { get; set; }
            public string StandardsSupported { get; set; }
            public string StandardUsed { get; set; }
            public string DataPath { get; set; }
            public int InterleaveDepth { get; set; }
            public string ModulationType { get; set; }
            public string ChannelEncapsulationType { get; set; }
            public string ModulationHint { get; set; }
            public string AllowedProfiles { get; set; }
            public string CurrentProfile { get; set; }
            public int UPBOKLE { get; set; }

        }

    }

}
