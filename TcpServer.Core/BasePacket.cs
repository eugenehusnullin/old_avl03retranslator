using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TcpServer.Core
{
    public class BasePacket
    {
        public BasePacket()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");

            AlarmType = "AA";
            State = 'V';

            Speed = 0;

            LatitudeLetter = 'N';
            LongitudeLetter = 'E';

            ValidNavigDateTime = DateTime.Now;

            PDOP = 0;
            HDOP = 0;
            VDOP = 0;

            Status = "000000000000";
            RTC = DateTime.Now;

            Voltage = "00000000";
            ADC = "00000000";
            LACCI = "00000000";

            Temperature = "0000";
            Odometer = 0;

            SerialID = 1;
            RFIDNo = string.Empty;

            MagneticVariationLetter = 'N';
            Mode = 'A';
        }

        #region init methods

        public static BasePacket GetFromWialon(byte[] bytes)
        {
            var result = new BasePacket();

            var curentIndex = 0;

            var bytesBlockId = ByteHelper.GetBlockByEndByte(bytes, curentIndex, 0x0);
            curentIndex = curentIndex + bytesBlockId.Length + 1;
            var id = Encoding.ASCII.GetString(bytesBlockId);
            result.IMEI = id;

            var utcBlock = ByteHelper.GetBlockByCount(bytes, curentIndex, 4);
            var utcTime = BitConverter.ToInt32(new[] { utcBlock[3], utcBlock[2], utcBlock[1], utcBlock[0] }, 0);
            curentIndex = curentIndex + 4;
            result.RTC = utcTime.FromUDateTime();

            var messageBlock = ByteHelper.GetBlockByCount(bytes, curentIndex, 4);
            var messageFlags = BitConverter.ToInt32(new[] { messageBlock[3], messageBlock[2], messageBlock[1], messageBlock[0] }, 0);
            curentIndex = curentIndex + 4;

            var data = GetWialonBlockData(bytes, curentIndex);
            var posInfo = GetPosInfo(data);
            result.Longitude = posInfo.Longitude;
            result.Latitude = posInfo.Latitude;

            result.Voltage = GetWialonDoubleData(data, "pwr_ext").ToString();

            var status = GetWialonStringData(data, "avl_inputs");
            status = GetWialonStringData(data, "avl_outputs");

            result.Status = status;

            return result;
        }
        private static string GetWialonStringData(IEnumerable<WialonBlockData> data, string name)
        {
            var result = string.Empty;

            var block = data.FirstOrDefault(_ => _.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (block != null)
            {
                result = ByteHelper.GetStringFromBytes(block.Value);
            }

            return result;
        }
        private static double GetWialonDoubleData(IEnumerable<WialonBlockData> data, string name)
        {
            double result = 0;

            var block = data.FirstOrDefault(_ => _.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (block != null)
            {
                var dataBytes = block.Value;
                result = BitConverter.ToDouble(new[] { dataBytes[0], dataBytes[1], dataBytes[2], dataBytes[3], dataBytes[4], dataBytes[5], dataBytes[6], dataBytes[7] }, 0);
            }

            return result;
        }
        private static WialonPosInfo GetPosInfo(IEnumerable<WialonBlockData> data)
        {
            var posinfoBlock = data.FirstOrDefault(_ => _.Name.Equals("posinfo", StringComparison.InvariantCultureIgnoreCase));
            var posInfo = new WialonPosInfo();
            if (posinfoBlock != null)
            {
                var dataBytes = posinfoBlock.Value;

                posInfo.Longitude = BitConverter.ToDouble(new[] { dataBytes[0], dataBytes[1], dataBytes[2], dataBytes[3], dataBytes[4], dataBytes[5], dataBytes[6], dataBytes[7] }, 0);
                posInfo.Latitude = BitConverter.ToDouble(new[] { dataBytes[8], dataBytes[9], dataBytes[10], dataBytes[11], dataBytes[12], dataBytes[13], dataBytes[14], dataBytes[15] }, 0);
                posInfo.Altitude = BitConverter.ToDouble(new[] { dataBytes[16], dataBytes[17], dataBytes[18], dataBytes[19], dataBytes[20], dataBytes[21], dataBytes[22], dataBytes[23] }, 0);
                posInfo.Speed = BitConverter.ToInt16(new[] { dataBytes[24], dataBytes[25] }, 0);
                posInfo.Cource = BitConverter.ToInt16(new[] { dataBytes[26], dataBytes[27] }, 0);
                posInfo.Satelits = dataBytes[28];
            }
            return posInfo;
        }
        private static IEnumerable<WialonBlockData> GetWialonBlockData(IList<byte> bytes, int startIndex)
        {
            var result = new List<WialonBlockData>();
            var curentIndex = startIndex;

            while (curentIndex < bytes.Count)
            {
                var data = new WialonBlockData();

                var typeBlock = ByteHelper.GetBlockByCount(bytes, curentIndex, 2);
                data.Type = BitConverter.ToInt16(new[] { typeBlock[1], typeBlock[0] }, 0);
                curentIndex = curentIndex + 2;

                var lenghtBlock = ByteHelper.GetBlockByCount(bytes, curentIndex, 4);
                data.Lenght = BitConverter.ToInt32(new[] { lenghtBlock[3], lenghtBlock[2], lenghtBlock[1], lenghtBlock[0] }, 0);
                curentIndex = curentIndex + 4;

                data.IsHidden = bytes[curentIndex] == 0;
                curentIndex = curentIndex + 1;

                data.DataType = bytes[curentIndex];
                curentIndex = curentIndex + 1;

                var bytesBlockName = ByteHelper.GetBlockByEndByte(bytes, curentIndex, 0x0);
                curentIndex = curentIndex + bytesBlockName.Length + 1;
                data.Name = Encoding.ASCII.GetString(bytesBlockName);

                var valueLenght = data.Lenght - (bytesBlockName.Length + 3); // 3 = IsHidden(1) + DataType(1) + EndName(1)
                data.Value = ByteHelper.GetBlockByCount(bytes, curentIndex, valueLenght);
                curentIndex = curentIndex + data.Value.Length;

                result.Add(data);
            }

            return result;
        }


        public static BasePacket GetFromAdv(byte[] bytes)
        {
            var dataBytes = GetAdvDataBytes(bytes);
            var deviceId = BitConverter.ToInt32(bytes, 5);

            var result = new BasePacket();

            result.IMEI = (600000000000000 + deviceId).ToString();

            result.RTC = new DateTime(2000 + dataBytes[1], dataBytes[2], dataBytes[3], dataBytes[5], dataBytes[6], dataBytes[7]);
            result.RTC = result.RTC.AddHours(4);

            result.ValidNavigDateTime = new DateTime(2000 + dataBytes[8], dataBytes[9], dataBytes[10], dataBytes[12], dataBytes[13], dataBytes[14]);
            result.ValidNavigDateTime = result.ValidNavigDateTime.AddHours(4);

            var lat = BitConverter.ToSingle(new[] { dataBytes[17], dataBytes[18], dataBytes[19], dataBytes[20] }, 0);
            result.Latitude = Math.Abs(lat);
            result.LatitudeLetter = lat < 0 ? 'N' : 'S';

            var lon = BitConverter.ToSingle(new[] { dataBytes[21], dataBytes[22], dataBytes[23], dataBytes[24] }, 0);
            result.Longitude = Math.Abs(lon);
            result.LongitudeLetter = lon < 0 ? 'E' : 'W';

            var direction = BitConverter.ToSingle(new[] { dataBytes[25], dataBytes[26], dataBytes[27], dataBytes[28] }, 0);
            result.Direction = direction;

            var sog = BitConverter.ToSingle(new[] { dataBytes[29], dataBytes[30], dataBytes[31], dataBytes[32] }, 0);
            result.Speed = sog;

            var altitude = BitConverter.ToSingle(new[] { dataBytes[33], dataBytes[34], dataBytes[35], dataBytes[36] }, 0);
            result.Altitude = altitude;

            //result.PDOP = 2.2f;
            //result.HDOP = 1.3f;
            //result.VDOP = 1.8f;

            var isAlarm = dataBytes[0] != 0;
            var inputs = BitConverter.ToInt32(new[] { dataBytes[45], dataBytes[46], dataBytes[47], dataBytes[48] }, 0);
            var outputs = BitConverter.ToInt32(new[] { dataBytes[49], dataBytes[50], dataBytes[51], dataBytes[52] }, 0);

            result.Status = GetStatus(isAlarm, inputs, outputs);

            result.Voltage = "00000000";

            result.State = 'A';

            return result;
        }
        private static string GetStatus(bool isAlarm, int inputs, int outputs)
        {
            var sb = new StringBuilder();
            var inputsBin = ByteHelper.GetBinValue(inputs, 4);
            var outputsBin = ByteHelper.GetBinValue(outputs, 4);
            sb.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}", isAlarm ? 1 : 0, isAlarm ? 1 : 0, 0, 0, 0, inputsBin[3], 0, 0, 0, 0, 0, 0);
            //sb.AppendFormat("{11}{10}{9}{8}{7}{6}{5}{4}{3}{2}{1}{0}", isAlarm ? 1 : 0, isAlarm ? 1 : 0, 0, 0, 0, 0, 0, inputsBin[0], 0, 0, 0, 0);

            return sb.ToString();
        }
        private static byte[] GetAdvDataBytes(IEnumerable<byte> bytes)
        {
            return bytes.Skip(21).Take(87).ToArray();
        }

        public static BasePacket GetFromGlonass(string stringPacket)
        {
            const string pattern_old = @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})(?<Chip>U|R)(?<State>A|V)(?<Satellites>\d{2})"
                                  + @"(?<Latitude>[0-9\.]{8})(?<LatitudeLetter>N|S)(?<Longitude>[0-9\.]{9})(?<LongitudeLetter>E|W)(?<Speed>[0-9]{3})(?<Direction>[0-9]{3})"
                                  + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<DateTime>[0-9]{14})\|(?<RTC>[0-9]{14})\|(?<Status>[0-9]{12})"
                                  + @"\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6})\|(?<SerialID>\d{4})\|(?<RFIDNo>\d*)\|"
                                  + @"(?<Checksum>\w{4})";

            const string pattern_9_19_Impuls = @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})(?<Chip>U|R)(?<State>A|V)(?<Satellites>\d{2})"
                                  + @"(?<Latitude>[0-9\.]{8})(?<LatitudeLetter>N|S)(?<Longitude>[0-9\.]{9})(?<LongitudeLetter>E|W)(?<Speed>[0-9]{3})(?<Direction>[0-9]{3})"
                                  + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<DateTime>[0-9]{14})\|(?<RTC>[0-9]{14})\|(?<Status>[0-9]{12})"
                                  + @"\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6})\|(?<SerialID>\d{4})\|(?<RFIDNo>\d*)"
                                  + @"\|(?<FuelImpuls>\d{5})\|(?<Checksum>\w{4})";

            var is_9_19_Impuls = stringPacket.Split('|').Length == 17;

            var pattern = is_9_19_Impuls ? pattern_9_19_Impuls : pattern_old;
            var regex = new Regex(pattern);
            var match = regex.Match(stringPacket);
            var matchGroups = match.Groups;

            var result = new BasePacket();

            long imei;
            if (!long.TryParse(matchGroups["Imei"].Value, out imei)) throw new Exception("В пакете не верный IMEI");
            result.IMEI = imei.ToString();

            result.AlarmType = matchGroups["AlarmType"].Value;

            char state;
            char.TryParse(matchGroups["State"].Value, out state);
            result.State = state;

            // coordinats 
            float latitude;
            float.TryParse(matchGroups["Latitude"].Value, out latitude);
            result.Latitude = ConvertGlonassToBaseCoordinat(latitude);

            char latitudeLetter;
            char.TryParse(matchGroups["LatitudeLetter"].Value, out latitudeLetter);
            result.LatitudeLetter = latitudeLetter;

            float longitude;
            float.TryParse(matchGroups["Longitude"].Value, out longitude);
            result.Longitude = ConvertGlonassToBaseCoordinat(longitude);

            char longitudeLetter;
            char.TryParse(matchGroups["LongitudeLetter"].Value, out longitudeLetter);
            result.LongitudeLetter = longitudeLetter;

            // speed
            int sog;
            int.TryParse(matchGroups["Speed"].Value, out sog);
            result.Speed = sog;

            // speed
            int direction;
            int.TryParse(matchGroups["Direction"].Value, out direction);
            result.Direction = direction;

            // 
            float pdop;
            float.TryParse(matchGroups["PDOP"].Value, out pdop);
            result.PDOP = pdop;

            float hdop;
            float.TryParse(matchGroups["HDOP"].Value, out hdop);
            result.HDOP = hdop;

            float vdop;
            float.TryParse(matchGroups["VDOP"].Value, out vdop);
            result.VDOP = vdop;

            result.Status = matchGroups["Status"].Value;

            var rtcSttring = matchGroups["RTC"].Value;

            int rtcYear;
            int.TryParse(rtcSttring.Substring(0, 4), out rtcYear);

            int rtcMonth;
            int.TryParse(rtcSttring.Substring(4, 2), out rtcMonth);

            int rtcDay;
            int.TryParse(rtcSttring.Substring(6, 2), out rtcDay);

            int rtcHour;
            int.TryParse(rtcSttring.Substring(8, 2), out rtcHour);

            int rtcMinute;
            int.TryParse(rtcSttring.Substring(10, 2), out rtcMinute);

            int rtcSeconds;
            int.TryParse(rtcSttring.Substring(6, 2), out rtcSeconds);

            result.RTC = new DateTime(rtcYear, rtcMonth, rtcDay, rtcHour, rtcMinute, rtcSeconds);

            result.Voltage = matchGroups["Voltage"].Value;
            result.ADC = matchGroups["ADC"].Value;
            result.LACCI = matchGroups["LACCI"].Value;

            result.Temperature = matchGroups["Temperature"].Value;

            float odometer;
            float.TryParse(matchGroups["Odometer"].Value, out odometer);
            result.Odometer = odometer;

            int serialID;
            int.TryParse(matchGroups["SerialID"].Value, out serialID);
            result.SerialID = serialID;

            result.RFIDNo = matchGroups["RFIDNo"].Value;

            if (is_9_19_Impuls)
            {
                int fuelImpuls;
                int.TryParse(matchGroups["FuelImpuls"].Value, out fuelImpuls);
                result.FuelImpuls = fuelImpuls;

                var fuelString = GetIntToString(fuelImpuls, 4);
                result.ADC = result.ADC.Substring(0, 4) + fuelString;
            }

            return result;
        }
        private static float ConvertGlonassToBaseCoordinat(float coordinate)
        {
            var degrees = Math.Floor(coordinate);
            var result = degrees * 100 + 60 * (coordinate - degrees);
            return (float)result;
        }
        public static string GetIntToString(int inputs, int i)
        {
            var result = Convert.ToString(inputs, 10);

            while (result.Length < i)
            {
                result = result.Insert(0, "0");
            }

            return result;
        }

        #endregion

        #region to packet methods

        public string ToPacketGlonass()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0:D15}", IMEI);
            sb.Append("|");
            sb.AppendFormat("{0}", AlarmType);

            sb.Append("|");
            sb.AppendFormat("{0:F1}", PDOP);
            sb.Append("|");
            sb.AppendFormat("{0:F1}", HDOP);
            sb.Append("|");
            sb.AppendFormat("{0:F1}", VDOP);
            sb.Append("|");
            sb.AppendFormat("{0}", Status);
            sb.Append("|");
            sb.AppendFormat("{0:yyyyMMddHHmmss}", RTC);
            sb.Append("|");
            sb.AppendFormat("{0}", Voltage);
            sb.Append("|");
            sb.AppendFormat("{0}", ADC);
            sb.Append("|");
            sb.AppendFormat("{0}", LACCI);
            sb.Append("|");
            sb.AppendFormat("{0}", Temperature);
            sb.Append("|");
            sb.AppendFormat("{0:F4}", Odometer);
            sb.Append("|");
            sb.AppendFormat("{0:D4}", SerialID);
            sb.Append("|");
            sb.AppendFormat("{0}", RFIDNo);

            //sb.Append("359772032388028|61$GPRMC,301620.000,A,4650.3652,N,02927.7965,E,0.00,,231111,,,A*70|03.1|02.7|01.6|000000000000|20111123201620|03990000|00000000|000803C2|0000|0.0000|0005|");

            sb.Insert(0, string.Format("$${0:X2}", sb.Length + 9));
            sb.Append("|");

            var crc16Ibm = new Crc16Ibm();
            var crc = crc16Ibm.ComputeChecksumASCII(sb.ToString());

            sb.AppendFormat("{0}", crc.ToString("X4"));

            return sb.ToString();
        }


        public string ToPacketGps()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0:D15}", IMEI);
            sb.Append("|");
            sb.AppendFormat("{0}", AlarmType);

            // NMEA
            sb.Append(BuildNmea());

            sb.Append("|");
            sb.AppendFormat("{0:F1}", PDOP);
            sb.Append("|");
            sb.AppendFormat("{0:F1}", HDOP);
            sb.Append("|");
            sb.AppendFormat("{0:F1}", VDOP);
            sb.Append("|");
            sb.AppendFormat("{0}", Status);
            sb.Append("|");
            sb.AppendFormat("{0:yyyyMMddHHmmss}", RTC);
            sb.Append("|");
            sb.AppendFormat("{0}", Voltage);
            sb.Append("|");
            sb.AppendFormat("{0}", ADC);
            sb.Append("|");
            sb.AppendFormat("{0}", LACCI);
            sb.Append("|");
            sb.AppendFormat("{0}", Temperature);
            sb.Append("|");
            sb.AppendFormat("{0:F4}", Odometer);
            sb.Append("|");
            sb.AppendFormat("{0:D4}", SerialID);
            sb.Append("|");
            sb.AppendFormat("{0}", RFIDNo);

            //sb.Append("359772032388028|61$GPRMC,301620.000,A,4650.3652,N,02927.7965,E,0.00,,231111,,,A*70|03.1|02.7|01.6|000000000000|20111123201620|03990000|00000000|000803C2|0000|0.0000|0005|");

            sb.Insert(0, string.Format("$${0:X2}", sb.Length + 9));
            sb.Append("|");

            var crc16Ibm = new Crc16Ibm();
            var crc = crc16Ibm.ComputeChecksumASCII(sb.ToString());

            sb.AppendFormat("{0}", crc.ToString("X4"));

            return sb.ToString();
        }
        private string BuildNmea()
        {
            var sb = new StringBuilder();

            sb.Append("GPRMC");
            sb.AppendFormat(",{0:HHmmss.fff}", ValidNavigDateTime);
            sb.AppendFormat(",{0}", State);

            sb.AppendFormat(",{0:F4}", Latitude);
            sb.AppendFormat(",{0}", LatitudeLetter);

            sb.AppendFormat(",{0:F4}", Longitude);
            sb.AppendFormat(",{0}", LongitudeLetter);

            sb.AppendFormat(",{0:F2}", Speed);
            sb.AppendFormat(",{0:F2}", Direction);

            sb.AppendFormat(",{0:ddMMyy}", ValidNavigDateTime);

            sb.AppendFormat(",{0}", MagneticVariation);
            sb.AppendFormat(",{0}", MagneticVariationLetter);

            sb.AppendFormat(",{0}", Mode);

            var crc = CrcNmea.ComputeChecksumASCII(sb.ToString());
            sb.Insert(0, "$");

            sb.AppendFormat("*{0:X2}", crc);

            return sb.ToString();
        }

        #endregion

        #region properties

        /// <summary>
        /// Уникальный идентификатор модема
        /// </summary>
        public string IMEI { get; set; }

        /// <summary>
        /// Gprs тип сигнала
        /// 0x01  SOS button is pressed 
        /// 0x49  Button A is pressed
        /// 0x09  Auto Shutdown Alarm
        /// 0x10  Low battery Alarm 
        /// 0x11  Over Speed Alarm
        /// 0x13  Recover From Over Speed
        /// 0x30  Parking Alarm  
        /// 0x42  Out Geo-fence Alarm
        /// 0x43  Into Geo-fence Alarm
        /// 0x50  IO-1 Close  —digital input 1 closed
        /// 0x51  IO-1 Open  —digital input 1 opened
        /// 0x52  IO-2 Close  —digital input 2 closed
        /// 0x53  IO-2 Open  —digital input 2 opened
        /// 0x54  IO-3 Close  —digital input 3 close
        /// 0x55  IO-3 Open  —digital input 3 opened
        /// 0x56  IO-4 Close  —digital input 4 close
        /// 0x57  IO-4 Open  —digital input 4 opened
        /// 0x60  Begin Charge
        /// 0x61  End Charge
        /// 0x66  Find a new RFID
        /// 0x88  Heartbeat 
        /// 0x91  Into Sleep Mode
        /// 0x92  Wakeup From Sleep Mode
        /// 0xAA  Interval GPRS data 
        /// </summary>
        public string AlarmType { get; set; }

        /// <summary>
        /// A - верные координаты
        /// V - ошибочные координаты
        /// </summary>
        public char State { get; set; }

        public double Latitude { get; set; }
        public char LatitudeLetter { get; set; }

        public double Longitude { get; set; }
        public char LongitudeLetter { get; set; }

        public DateTime ValidNavigDateTime { get; set; }

        public float PDOP { get; set; }
        public float HDOP { get; set; }
        public float VDOP { get; set; }

        /// <summary>
        /// Status(12 Bytes)
        /// Byte 01 —— SOS button
        /// Byte 02 —— Button A button 
        /// Byte 05 —— Input 1  the status of the digital input 1  PORT7 (some times connect to the engine)
        /// Byte 06 —— Input 2  the status of the digital input 2  PORT6
        /// Byte 07 —— Input 3   
        /// Byte 08 —— Input 4
        /// Byte 09 —— Out 1   
        /// Byte 10 —— Out 2 
        /// Byte 11 —— Out 3
        /// Byte 12 —— Out 4
        /// </summary>
        public string Status { get; set; }
        public DateTime RTC { get; set; }


        public string Voltage { get; set; }
        public string ADC { get; set; }
        public string LACCI { get; set; }

        public string Temperature { get; set; }
        public float Odometer { get; set; }

        public int SerialID { get; set; }
        public string RFIDNo { get; set; }

        public int FuelImpuls { get; set; }

        /// <summary>
        /// Скорость морская миля
        /// </summary>
        public float Speed { get; set; }
        /// <summary>
        /// Направление в градусах
        /// </summary>
        public float Direction { get; set; }

        public float Altitude { get; set; }

        public float MagneticVariation { get; set; }
        public char MagneticVariationLetter { get; set; }

        public char Mode { get; set; }

        #endregion
    }
}