using Newtonsoft.Json;
using System.IO;
using XDPeople.Data;
using XDPeople.Framework;
using XDPeople.License;
using XDPeople.Utils;

namespace API_EXAMPLE
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Constant.PathLicense = @"C:\XDSoftware_testes\cfg\";
            Constant.MyXDPublicKey = @"C:\XDSoftware_testes\cfg\xd.pem";
            
            IniFile.Load();
            Localization.Language = new Languages(IniFile.Lang);
            XDLicence.Load(Constant.PathLicense + Constant.LicFile, XDProg.XDGC);
            XDLicence.DefaultXDProg = XDProg.XDGC;
            Db.Configure();

            if (Db.Connect() == 0)
            {
                
                Db.InitializeDatabase(null, true);
                GlobalVars.LoadInitialConfigurations(false, true);
                GlobalVars.LoadTerminalConfig();
                GlobalVars.LoadCurrency();
                SystemSettings.CurrentUser = GlobalVars.GlobalListEmployee[0];

                string text = File.ReadAllText(@"C:\json_example.txt");

                dynamic orderObjectData = JsonConvert.DeserializeObject<object>(text);
                examples.GenerateDocument(orderObjectData);

            }
        }
    }
}