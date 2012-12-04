using OnlineMonitoring.ServerCore.Packets;

namespace OnlineMonitoring.ServerCore.DataBase
{
    public class DataBaseManager : ClsCmd
    {
        public DataBaseManager(string connectionString) : base(connectionString) { }


        public int SaveBasePacket(BasePacket packet)
        {
            return Execute("INSERT INTO [Packet] ([IMEI],[AlarmType],[State],[Latitude],[LatitudeLetter],[Longitude],[LongitudeLetter],[ValidNavigDateTime],[PDOP],[HDOP],[VDOP]" +
                     ",[Status],[RTC],[Voltage],[ADC],[LACCI],[Temperature],[Odometer],[SerialID],[RFIDNo],[Speed],[Direction],[MagneticVariation],[MagneticVariationLetter],[Mode])" +
                      " VALUES (@imei,@alarmType,@state,@latitude,@latitudeLetter,@longitude,@longitudeLetter,@validNavigDateTime,@pdop,@hdop,@vdop,@status,@rtc,@voltage,@adc,@lacci," +
                     "@temperature,@odometer,@serialId,@rfIdNo,@speed,@direction,@magneticVariation,@magneticVariationLetter,@mode)"
                     , "@imei", packet.IMEI, "@alarmType", packet.AlarmType, "@state", packet.State, "@latitude", packet.Latitude, "@latitudeLetter", packet.LatitudeLetter
                     , "@longitude", packet.Longitude, "@longitudeLetter", packet.LongitudeLetter, "@validNavigDateTime", packet.ValidNavigDateTime, "@pdop", packet.PDOP
                     , "@hdop", packet.HDOP, "@vdop", packet.VDOP, "@status", packet.Status, "@rtc", packet.RTC, "@voltage", packet.Voltage, "@adc", packet.ADC, "@lacci", packet.LACCI
                     , "@temperature", packet.Temperature, "@odometer", packet.Odometer, "@serialId", packet.SerialID, "@rfIdNo", packet.RFIDNo, "@speed", packet.Speed
                     , "@direction", packet.Direction, "@magneticVariation", packet.MagneticVariation, "@magneticVariationLetter", packet.MagneticVariationLetter, "@mode", packet.Mode);

        }
    }
}