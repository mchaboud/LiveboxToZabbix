using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Zabbix_Sender;

namespace LiveboxToZabbix;
internal class Livebox5WS
{
    public string Login { get; set; }
    public string Password { private get; set; }
    //public int Port { get; set; } //In case for remote access if debugged one day...
    public string Address { get; set; }

    private HttpClient client;
    private string authToken;

    public Livebox5WS(string login, string password, string address)
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

        Livebox5WSRequest req = new("sah.Device.Information", "createContext", new ParametersCreateContext("webui", this.Login, this.Password));
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
    internal async Task RetrieveDeviceInfo()
    {
        // POST http://192.168.1.1/ws
        //{ "service":"DeviceInfo","method":"get","parameters":{ } }
        //{"status":{"Manufacturer":"Arcadyan","ManufacturerOUI":"047056","ModelName":"ArcadyanLBFIBRE_OFR","Description":"ArcadyanLBFIBRE_OFR Arcadyan fr","ProductClass":"Livebox Fibre","SerialNumber":"JA2304xxxxxxxxx","HardwareVersion":"AR_LBF_2.1","SoftwareVersion":"ARFI-fr-4.66.0.1_10.5.0","RescueVersion":"ARFI-fr-4.46.0.1_10.5.0","ModemFirmwareVersion":"","EnabledOptions":"","AdditionalHardwareVersion":"","AdditionalSoftwareVersion":"g0-f-fr","SpecVersion":"1.1","ProvisioningCode":"HASH.3780.xxxx","UpTime":2109,"FirstUseDate":"0001-01-01T00:00:00Z","DeviceLog":"","VendorConfigFileNumberOfEntries":1,"ManufacturerURL":"http://www.arcadyan.com","Country":"fr","ExternalIPAddress":"90.0.0.0","DeviceStatus":"Up","NumberOfReboots":6,"UpgradeOccurred":false,"ResetOccurred":false,"RestoreOccurred":false,"StandbyOccurred":false,"X_SOFTATHOME-COM_AdditionalSoftwareVersions":"Bootloader=10.5.0,RescueBootloader=10.5.0","BaseMAC":"04:70:XX:XX:XX:XX"}}
        await askWS(
       new Livebox5WSRequest("DeviceInfo", "get", new Object()),
           HttpMethod.Post,
           async (res) =>
           {
               string resp_ = await res.Content.ReadAsStringAsync();
               GetDeviceInfoResp resp = await res.Content.ReadFromJsonAsync<GetDeviceInfoResp>();
               ZS_Request r = new ZS_Request
               {
                   data = new ZS_Data[]
                   {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Manufacturer", resp?.status?.Manufacturer ??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ManufacturerOUI", resp?.status?.ManufacturerOUI ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ModelName", resp?.status?.ModelName ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Description", resp?.status?.Description ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ProductClass", resp?.status?.ProductClass ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DeviceInfoSerialNumber", resp?.status?.SerialNumber ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DeviceInfoHardwareVersion", resp?.status?.HardwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"SoftwareVersion", resp?.status?.SoftwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"RescueVersion", resp?.status?.RescueVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ModemFirmwareVersion", resp?.status?.ModemFirmwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"EnabledOptions", resp?.status?.EnabledOptions ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"AdditionalHardwareVersion", resp?.status?.AdditionalHardwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"AdditionalSoftwareVersion", resp?.status?.AdditionalSoftwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"SpecVersion", resp?.status?.SpecVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ProvisioningCode", resp?.status?.ProvisioningCode ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpTime", resp?.status?.UpTime.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"FirstUseDate", resp?.status?.FirstUseDate.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DeviceLog", resp?.status?.DeviceLog ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"VendorConfigFileNumberOfEntries", resp?.status?.VendorConfigFileNumberOfEntries.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ManufacturerURL", resp?.status?.ManufacturerURL ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Country", resp?.status?.Country ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ExternalIPAddress", resp?.status?.ExternalIPAddress ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DeviceStatus", resp?.status?.DeviceStatus ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"NumberOfReboots", resp?.status?.NumberOfReboots.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpgradeOccurred", resp?.status?.UpgradeOccurred.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ResetOccurred", resp?.status?.ResetOccurred.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"RestoreOccurred", resp?.status?.RestoreOccurred.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"StandbyOccurred", resp?.status?.StandbyOccurred.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"X_SOFTATHOME-COM_AdditionalSoftwareVersions", resp?.status?.X_SOFTATHOME_COM_AdditionalSoftwareVersions ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"BaseMAC", resp?.status?.BaseMAC ?? "N/A"),
                   }
               };
               return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
           }
       );
    }
    internal async Task RetrieveNMC()
    {
        // POST http://
        //{"service":"NMC","method":"get","parameters":{}}
        //{"status":{"WanModeList":"Ethernet_PPP;Ethernet_DHCP;GPON_DHCP;GPON_PPP","WanMode":"GPON_DHCP","Username":"fti/xxxxxxx","FactoryResetScheduled":false,"ConnectionError":false,"DefaultsLoaded":true,"ProvisioningState":"done","OfferType":"Res","OfferName":"MonoLigne","IPTVMode":"Internet"}}
        await askWS(
         new Livebox5WSRequest("NMC", "get", new Object()),
             HttpMethod.Post,
             async (res) =>
             {
                 string resp_ = await res.Content.ReadAsStringAsync();
                 GetNMCStatsResp resp = await res.Content.ReadFromJsonAsync<GetNMCStatsResp>();
                 ZS_Request r = new ZS_Request
                 {
                     data = new ZS_Data[]
                     {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"WanModeList", resp?.status?.WanModeList ??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"WanMode", resp?.status?.WanMode ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Username", resp?.status?.Username ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"FactoryResetScheduled", resp?.status?.FactoryResetScheduled.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ConnectionError", resp?.status?.ConnectionError.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DefaultsLoaded", resp?.status?.DefaultsLoaded.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ProvisioningState", resp?.status?.ProvisioningState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"OfferType", resp?.status?.OfferType ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"OfferName", resp?.status?.OfferName ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPTVMode", resp?.status?.IPTVMode ?? "N/A"),
                     }
                 };
                 return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
             }
         );
    }
    internal async Task RetrieveONTStatusAsync()
    {
        // POST http://
        //{"service":"NeMo.Intf.veip0","method":"getMIBs","parameters":{"mibs":"gpon"}}
        //{"status":{"gpon":{"veip0":{"RegistrationID":"","VeipPptpUni":true,"OmciIsTmOwner":false,"MaxBitRateSupported":1000,"SignalRxPower":-20457,"SignalTxPower":1873,"Temperature":40,"Voltage":32952,"Bias":9,"SerialNumber":"ARLT3XXXXXXX","HardwareVersion":"ARLTARLBF21","EquipmentId":"ArcadyanLBFIBREOFR","VendorId":"ARLT","VendorProductCode":1,"PonId":"","ONTSoftwareVersion0":"SAHEOFR044600","ONTSoftwareVersion1":"SAHEOFR046600","ONTSoftwareVersionActive":1,"ONUState":"O5_Operation","DownstreamMaxRate":2488000,"UpstreamMaxRate":1244000,"DownstreamCurrRate":2488000,"UpstreamCurrRate":1244000}}}}
        await askWS(
            new Livebox5WSRequest("NeMo.Intf.veip0", "getMIBs", new ParametersGetMIBs("gpon")),
                HttpMethod.Post,
                async (res) =>
                {
                    string resp_ = await res.Content.ReadAsStringAsync();
                    GetONTStatsResp resp = await res.Content.ReadFromJsonAsync<GetONTStatsResp>();
                    ZS_Request r = new ZS_Request
                    {
                        data = new ZS_Data[]
                        {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"RegistrationID", resp?.status?.gpon?.veip0?.RegistrationID ??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"VeipPptpUni", resp?.status?.gpon?.veip0?.VeipPptpUni.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"OmciIsTmOwner", resp?.status?.gpon?.veip0?.OmciIsTmOwner.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"MaxBitRateSupported", resp?.status?.gpon?.veip0?.MaxBitRateSupported.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"SignalRxPower", resp?.status?.gpon?.veip0?.SignalRxPower.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"SignalTxPower", resp?.status?.gpon?.veip0?.SignalTxPower.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Temperature", resp?.status?.gpon?.veip0?.Temperature.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Voltage", resp?.status?.gpon?.veip0?.Voltage.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Bias", resp?.status?.gpon?.veip0?.Bias.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONTStatsSerialNumber", resp?.status?.gpon?.veip0?.SerialNumber ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONTStatsHardwareVersion", resp?.status?.gpon?.veip0?.HardwareVersion ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"EquipmentId", resp?.status?.gpon?.veip0?.EquipmentId ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"VendorId", resp?.status?.gpon?.veip0?.VendorId ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"VendorProductCode", resp?.status?.gpon?.veip0?.VendorProductCode.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"PonId", resp?.status?.gpon?.veip0?.PonId ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONTSoftwareVersion0", resp?.status?.gpon?.veip0?.ONTSoftwareVersion0 ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONTSoftwareVersion1", resp?.status?.gpon?.veip0?.ONTSoftwareVersion1 ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONTSoftwareVersionActive", resp?.status?.gpon?.veip0?.ONTSoftwareVersionActive.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ONUState", resp?.status?.gpon?.veip0?.ONUState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamMaxRate", resp?.status?.gpon?.veip0?.DownstreamMaxRate.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamMaxRate", resp?.status?.gpon?.veip0?.UpstreamMaxRate.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DownstreamCurrRate", resp?.status?.gpon?.veip0?.DownstreamCurrRate.ToString() ?? "-1"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"UpstreamCurrRate", resp?.status?.gpon?.veip0?.UpstreamCurrRate.ToString() ?? "-1"),
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
            new Livebox5WSRequest("NMC", "getWANStatus", new Object()),
                HttpMethod.Post,
                async (res) =>
                {
                    string resp_ = await res.Content.ReadAsStringAsync();
                    GetWANStatusResp resp = await res.Content.ReadFromJsonAsync<GetWANStatusResp>();
                    ZS_Request r = new ZS_Request
                    {
                        data = new ZS_Data[]
                        {
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"WanState", resp.data.WanState??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkType", resp.data.LinkType??"N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LinkState", resp.data.LinkState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"GponState", resp.data.GponState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"MACAddress", resp.data.MACAddress ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"Protocol", resp.data.Protocol ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"ConnectionState", resp.data.ConnectionState ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"LastConnectionError", resp.data.LastConnectionError ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPAddress", resp.data.IPAddress ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"RemoteGateway", resp.data.RemoteGateway ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"DNSServers", resp.data.DNSServers.ToString() ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPv6Address", resp.data.IPv6Address ?? "N/A"),
                            new ZS_Data(Configuration.Instance.ZabbixHostToPopulate,"IPv6DelegatedPrefix", resp.data.IPv6DelegatedPrefix ??"N/A"),
                        }
                    };
                    return r.Send(Configuration.Instance.ZabbixServer, Configuration.Instance.ZabbixServerPort);
                }
            );
    }

    private async Task askWS(Livebox5WSRequest req, HttpMethod met, Func<HttpResponseMessage, Task<ZS_Response>> OnSuccess)
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
    public class Livebox5WSRequest
    {
        public Livebox5WSRequest(string service, string method, object parameters)
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
    public class GetWANStatusResp
    {
        //{"status":true,"data":{"WanState":"up","LinkType":"gpon","LinkState":"up","GponState":"O5_Operation","MACAddress":"04:70:56:43:D9:94","Protocol":"dhcp","ConnectionState":"Bound","LastConnectionError":"None","IPAddress":"90.9.17.80","RemoteGateway":"90.9.16.1","DNSServers":"80.10.246.134,81.253.149.5","IPv6Address":"2a01:cb15:8458:3900:670:56ff:fe43:d994","IPv6DelegatedPrefix":"2a01:cb15:8458:3900::/56"}}
        public bool status { get; set; }
        public GetDSLStatsStatusResp data { get; set; }
        [Serializable]
        public class GetDSLStatsStatusResp
        {
            public string WanState { get; set; }
            public string LinkType { get; set; }
            public string LinkState { get; set; }
            public string GponState { get; set; }
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
    public class GetONTStatsResp
    {
        public GetONTStatsStatusResp status { get; set; }

        [Serializable]
        public class GetONTStatsStatusResp
        {
            public GetONTStatsStatusGponResp gpon { get; set; }
        }
        [Serializable]
        public class GetONTStatsStatusGponResp
        {
            public GetONTStatsStatusGponVeip0Resp veip0 { get; set; }
        }
        [Serializable]
        public class GetONTStatsStatusGponVeip0Resp
        {
            public string RegistrationID { get; set; }
            public bool VeipPptpUni { get; set; }
            public bool OmciIsTmOwner { get; set; }
            public int MaxBitRateSupported { get; set; }
            public int SignalRxPower { get; set; }
            public int SignalTxPower { get; set; }
            public int Temperature { get; set; }
            public int Voltage { get; set; }
            public int Bias { get; set; }
            public string SerialNumber { get; set; }
            public string HardwareVersion { get; set; }
            public string EquipmentId { get; set; }
            public string VendorId { get; set; }
            public int VendorProductCode { get; set; }
            public string PonId { get; set; }
            public string ONTSoftwareVersion0 { get; set; }
            public string ONTSoftwareVersion1 { get; set; }
            public int ONTSoftwareVersionActive { get; set; }
            public string ONUState { get; set; }
            public int DownstreamMaxRate { get; set; }
            public int UpstreamMaxRate { get; set; }
            public int DownstreamCurrRate { get; set; }
            public int UpstreamCurrRate { get; set; }
        }

    }
    [Serializable]
    internal class GetNMCStatsResp
    {
        public GetNMCStatsStatusResp status { get; set; }
        [Serializable]
        public class GetNMCStatsStatusResp
        {
            public string WanModeList { get; set; }
            public string WanMode { get; set; }
            public string Username { get; set; }
            public bool FactoryResetScheduled { get; set; }
            public bool ConnectionError { get; set; }
            public bool DefaultsLoaded { get; set; }
            public string ProvisioningState { get; set; }
            public string OfferType { get; set; }
            public string OfferName { get; set; }
            public string IPTVMode { get; set; }
        }
    }
    [Serializable]
    internal class GetDeviceInfoResp
    {
        public GetDeviceInfoStatusResp status { get; set; }
        [Serializable]
        public class GetDeviceInfoStatusResp
        {
            public string Manufacturer { get; set; }
            public string ManufacturerOUI { get; set; }
            public string ModelName { get; set; }
            public string Description { get; set; }
            public string ProductClass { get; set; }
            public string SerialNumber { get; set; }
            public string HardwareVersion { get; set; }
            public string SoftwareVersion { get; set; }
            public string RescueVersion { get; set; }
            public string ModemFirmwareVersion { get; set; }
            public string EnabledOptions { get; set; }
            public string AdditionalHardwareVersion { get; set; }
            public string AdditionalSoftwareVersion { get; set; }
            public string SpecVersion { get; set; }
            public string ProvisioningCode { get; set; }
            public int UpTime { get; set; }
            public DateTime FirstUseDate { get; set; }
            public string DeviceLog { get; set; }
            public int VendorConfigFileNumberOfEntries { get; set; }
            public string ManufacturerURL { get; set; }
            public string Country { get; set; }
            public string ExternalIPAddress { get; set; }
            public string DeviceStatus { get; set; }
            public int NumberOfReboots { get; set; }
            public bool UpgradeOccurred { get; set; }
            public bool ResetOccurred { get; set; }
            public bool RestoreOccurred { get; set; }
            public bool StandbyOccurred { get; set; }
            [JsonPropertyName("X_SOFTATHOME-COM_AdditionalSoftwareVersions")]
            public string X_SOFTATHOME_COM_AdditionalSoftwareVersions { get; set; }
            public string BaseMAC { get; set; }
        }
    }
}

