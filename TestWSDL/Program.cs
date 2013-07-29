using Microsoft.Web.Services3.Design;
using Microsoft.Web.Services3.Security.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestOds;

namespace TestWSDL
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = new Program();
            program.testWSDL();
        }

        void testWSDL()
        {
            PolicyAssertion[] policyAssertion = new PolicyAssertion[] {
                            new OdsTransportAssertion()
                            //new UsernameOverTransportAssertion()
                        };
            Policy policy = new Policy(policyAssertion);

            TelemetryService service = new TelemetryService();
            service.SetPolicy(policy);
            service.SetClientCredential(new UsernameToken("BRAITMONITOR", "BRAITMONITOR", PasswordOption.SendPlainText));

            var telemetry = new telemetryBa();
            telemetry.gpsCode = "IMEI";
            telemetry.coordX = 30d;
            telemetry.coordY = 30d;
            telemetry.date = new DateTime();
            telemetry.speed = 30d;
            telemetry.glonass = false;

            var telemetryDetailsCollection = new List<telemetryDetailBa>();
            var telemetryDetails = new telemetryDetailBa();
            telemetryDetails.sensorCode = "pwr_ext";
            telemetryDetails.value = 12;
            telemetryDetailsCollection.Add(telemetryDetails);

            service.storeTelemetry(telemetry, telemetryDetailsCollection.ToArray());
        }
    }
}
