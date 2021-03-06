﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TcpServer.Core.exceptions;

namespace TcpServer.Core
{
    public class BasePacket
    {
        private BasePacket()
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

        public bool isSOS()
        {
            return AlarmType.Equals("01");
        }

        public static BasePacket GetFromGlonass(string stringPacket)
        {

// новый блок с датч. топлива
// $$AF863771020234458|AAUA0655.71719N038.23135E000003|01.3|00.8|01.0|20141029095029|20141029095029|000000000000|04020000|00000000|0000|0000|13DDEC5A|0000|0.0000|0047||00000|4D3C

// старый блок
// $$9F359772036674894|AAUA0753.36640N055.93727E000000|02.5|01.8|01.8|20141029103052|20141029103052|000000000000|14121179|00000000|00D1EFFD|0000|0.0000|0124||5E9E
// $$A5359772039754511|AAUA0855.74210N036.86489E000000|02.7|02.2|01.5|20131129130737|20131129130737|001000000000|14171226|00000000|13D249F9|0000|0.0000|0776||00000|B8B3
            const string pattern_old = @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})(?<Chip>U|R)(?<State>A|V)(?<Satellites>\d{2})"
                                  + @"(?<Latitude>[0-9\.]{8})(?<LatitudeLetter>N|S)(?<Longitude>[0-9\.]{9})(?<LongitudeLetter>E|W)(?<Speed>[0-9]{3})(?<Direction>[0-9]{3})"
                                  + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<DateTime>[0-9]{14})\|(?<RTC>[0-9]{14})\|(?<Status>[0-9]{12})"
                                  + @"\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6,})\|(?<SerialID>\d{4})\|(?<RFIDNo>\d*)\|"
                                  + @"(?<Checksum>\w{4})";

            const string pattern_9_19_Impuls = @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})(?<Chip>U|R)(?<State>A|V)(?<Satellites>\d{2})"
                                  + @"(?<Latitude>[0-9\.]{8})(?<LatitudeLetter>N|S)(?<Longitude>[0-9\.]{9})(?<LongitudeLetter>E|W)(?<Speed>[0-9]{3})(?<Direction>[0-9]{3})"
                                  + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<DateTime>[0-9]{14})\|(?<RTC>[0-9]{14})\|(?<Status>[0-9]{12})"
                                  + @"\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6,})\|(?<SerialID>\d{4})\|(?<RFIDNo>\d*)"
                                  + @"\|(?<FuelImpuls>\d{5})\|(?<Checksum>\w{4})";

            const string pattern_fuel = @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})(?<Chip>U|R)(?<State>A|V)(?<Satellites>\d{2})"
                                  + @"(?<Latitude>[0-9\.]{8})(?<LatitudeLetter>N|S)(?<Longitude>[0-9\.]{9})(?<LongitudeLetter>E|W)(?<Speed>[0-9]{3})(?<Direction>[0-9]{3})"
                                  + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<DateTime>[0-9]{14})\|(?<RTC>[0-9]{14})\|(?<Status>[0-9]{12})"
                                  + @"\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})\|(?<f1>\w{4})\|(?<f2>\w{4})\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6,})\|(?<SerialID>\d{4})\|(?<RFIDNo>\d*)"
                                  + @"\|(?<FuelImpuls>\d{5})\|(?<Checksum>\w{4})";


            var cnt_parts = stringPacket.Split('|').Length;
            if (cnt_parts == 14 || cnt_parts == 15)
            {
                return GetFromGPRMC(stringPacket);
            }
            else
            {
                int messageType;
                string pattern;
                switch (cnt_parts)
                {
                    case 16:
                        messageType = 1;
                        pattern = pattern_old;
                        break;
                    case 17:
                        messageType = 2;
                        pattern = pattern_9_19_Impuls;
                        break;
                    case 19:
                        messageType = 3;
                        pattern = pattern_fuel;
                        break;
                    default:
                        messageType = 0;
                        pattern = string.Empty;
                        break;
                }

                if (messageType == 0)
                {
                    throw new Exception("bad packet");
                }

                
                var regex = new Regex(pattern);
                var match = regex.Match(stringPacket);
                var matchGroups = match.Groups;

                var result = new BasePacket();

                long imeiCheck;
                if (!long.TryParse(matchGroups["Imei"].Value, out imeiCheck))
                {
                    throw new BadPacketException();
                }
                result.IMEI = matchGroups["Imei"].Value;

                result.AlarmType = matchGroups["AlarmType"].Value;

                char state;
                char.TryParse(matchGroups["State"].Value, out state);
                result.State = state;

                // coordinats 
                float latitude;
                float.TryParse(matchGroups["Latitude"].Value, out latitude);
                result.LatitudeOrig = latitude;
                result.Latitude = ConvertGlonassToBaseCoordinat(latitude);

                char latitudeLetter;
                char.TryParse(matchGroups["LatitudeLetter"].Value, out latitudeLetter);
                result.LatitudeLetter = latitudeLetter;

                float longitude;
                float.TryParse(matchGroups["Longitude"].Value, out longitude);
                result.LongitudeOrig = longitude;
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

                // RTC
                var rtcString = matchGroups["RTC"].Value;

                int rtcYear;
                int.TryParse(rtcString.Substring(0, 4), out rtcYear);

                int rtcMonth;
                int.TryParse(rtcString.Substring(4, 2), out rtcMonth);

                int rtcDay;
                int.TryParse(rtcString.Substring(6, 2), out rtcDay);

                int rtcHour;
                int.TryParse(rtcString.Substring(8, 2), out rtcHour);

                int rtcMinute;
                int.TryParse(rtcString.Substring(10, 2), out rtcMinute);

                int rtcSeconds;
                int.TryParse(rtcString.Substring(12, 2), out rtcSeconds);

                try
                {
                    result.ValidNavigDateTime = new DateTime(rtcYear, rtcMonth, rtcDay, rtcHour, rtcMinute, rtcSeconds);
                }
                catch
                {
                    result.ValidNavigDateTime = new DateTime();
                }

                //Datetime
                var datetimeString = matchGroups["DateTime"].Value;

                int datetimeYear;
                int.TryParse(datetimeString.Substring(0, 4), out datetimeYear);

                int datetimeMonth;
                int.TryParse(datetimeString.Substring(4, 2), out datetimeMonth);

                int datetimeDay;
                int.TryParse(datetimeString.Substring(6, 2), out datetimeDay);

                int datetimeHour;
                int.TryParse(datetimeString.Substring(8, 2), out datetimeHour);

                int datetimeMinute;
                int.TryParse(datetimeString.Substring(10, 2), out datetimeMinute);

                int datetimeSeconds;
                int.TryParse(datetimeString.Substring(12, 2), out datetimeSeconds);

                try
                {
                    result.RTC = new DateTime(datetimeYear, datetimeMonth, datetimeDay, datetimeHour, datetimeMinute, datetimeSeconds);
                }
                catch
                {
                    result.RTC = result.ValidNavigDateTime;
                }

                //Voltage and etc.
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

                if (messageType == 2 || messageType == 3)
                {
                    int fuelImpuls;
                    int.TryParse(matchGroups["FuelImpuls"].Value, out fuelImpuls);
                    result.FuelImpuls = fuelImpuls;

                    //var fuelString = GetIntToString(fuelImpuls, 4);
                    //result.ADC = result.ADC.Substring(0, 4) + fuelString;
                }

                return result;
            }
        }

        public static BasePacket GetFromGPRMC(string stringPacket)
        {
// without coord $$AE353358018980081|AA000000000000000000000000000000000000000000000000000000000000|00.0|00.0|00.0|100001000000|20000000000000|14121262|00000000|1E305333|0000|0.0000|0014|1325
//$$B7359772035557439|AA$GPRMC,173354.771,A,5543.1196,N,03813.8631,E,0.14,79.87,261113,,,A*57|04.5|03.1|03.2|000000000000|20131126173353|03710000|00000000|13DDEBD8|0000|0.0000|0002|3D7C
            const string pattern =
    @"\$\$(?<Len>\w{2})(?<Imei>\d{15})\|(?<AlarmType>\w{2})((\$GPRMC,(?<Time>[0-9\.]{9,11}),(?<State>A|V),(?<Latitude>[0-9\.]{7,10}),(?<LatitudeLetter>N|S),"
    + @"(?<Longitude>[0-9\.]{8,11}),(?<LongitudeLetter>E|W),(?<Speed>[0-9\.]*),(?<Direction>[0-9\.]*),(?<Date>[0-9]{6}),([0-9\.]{1,}|),([0-9\.]{1,}|)(,(A|D|E|N|)|)\*\w{2,})|(\d{1,}))"
    + @"\|(?<PDOP>[0-9\.]{4})\|(?<HDOP>[0-9\.]{4})\|(?<VDOP>[0-9\.]{4})\|(?<Status>[0-9]{12})\|(?<RTC>[0-9]{14})\|(?<Voltage>[0-9]{8})\|(?<ADC>[0-9]{8})"
    + @"\|(?<LACCI>\w{8})\|(?<Temperature>\w{4})\|(?<Odometer>[0-9\.]{6})\|(?<SerialID>\d{4})\|(\|?)(?<Checksum>\w{4})";

            var regex = new Regex(pattern);
            var match = regex.Match(stringPacket);
            var matchGroups = match.Groups;

            var result = new BasePacket();

            long imeiCheck;
            if (!long.TryParse(matchGroups["Imei"].Value, out imeiCheck)) throw new BadPacketException();
            result.IMEI = matchGroups["Imei"].Value;

            result.AlarmType = matchGroups["AlarmType"].Value;

            char state;
            if (!char.TryParse(matchGroups["State"].Value, out state))
            {
                state = 'Z';
            }
            result.State = state;

            // coordinats 
            float latitude;
            string slat = matchGroups["Latitude"].Value;
            float.TryParse(slat, out latitude);
            result.Latitude = latitude;
            result.LatitudeOrig = int.Parse(slat.Substring(0, 2)) + (float.Parse(slat.Substring(2))*60);

            char latitudeLetter;
            char.TryParse(matchGroups["LatitudeLetter"].Value, out latitudeLetter);
            result.LatitudeLetter = latitudeLetter;

            float longitude;
            string slon = matchGroups["Longitude"].Value;
            float.TryParse(slon, out longitude);
            result.Longitude = longitude;
            result.LongitudeOrig = int.Parse(slon.Substring(0, 3)) + (float.Parse(slon.Substring(3))*60);

            char longitudeLetter;
            char.TryParse(matchGroups["LongitudeLetter"].Value, out longitudeLetter);
            result.LongitudeLetter = longitudeLetter;

            // speed
            float sog;
            if (!float.TryParse(matchGroups["Speed"].Value, out sog))
            {
                sog = 0.0F;
            }
            result.Speed = sog;

            // speed
            float direction;
            if (!float.TryParse(matchGroups["Direction"].Value, out direction))
            {
                direction = 0.0F;
            }
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

            // RTC
            var rtcString = matchGroups["RTC"].Value;

            int rtcYear;
            int.TryParse(rtcString.Substring(0, 4), out rtcYear);

            int rtcMonth;
            int.TryParse(rtcString.Substring(4, 2), out rtcMonth);

            int rtcDay;
            int.TryParse(rtcString.Substring(6, 2), out rtcDay);

            int rtcHour;
            int.TryParse(rtcString.Substring(8, 2), out rtcHour);

            int rtcMinute;
            int.TryParse(rtcString.Substring(10, 2), out rtcMinute);

            int rtcSeconds;
            int.TryParse(rtcString.Substring(12, 2), out rtcSeconds);

            try
            {
                result.ValidNavigDateTime = new DateTime(rtcYear, rtcMonth, rtcDay, rtcHour, rtcMinute, rtcSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                result.ValidNavigDateTime = new DateTime();
            }

            //Datetime
            var date = matchGroups["Date"].Value;
            var time = matchGroups["Time"].Value;
            if (String.IsNullOrEmpty(date) || String.IsNullOrEmpty(time))
            {
                result.RTC = result.ValidNavigDateTime;
            }
            else
            {
                int datetimeYear;
                int.TryParse(date.Substring(4, 2), out datetimeYear);
                datetimeYear += 2000;

                int datetimeMonth;
                int.TryParse(date.Substring(2, 2), out datetimeMonth);

                int datetimeDay;
                int.TryParse(date.Substring(0, 2), out datetimeDay);

                int datetimeHour;
                int.TryParse(time.Substring(0, 2), out datetimeHour);

                int datetimeMinute;
                int.TryParse(time.Substring(2, 2), out datetimeMinute);

                int datetimeSeconds;
                int.TryParse(time.Substring(4, 2), out datetimeSeconds);

                try
                {
                    result.RTC = new DateTime(datetimeYear, datetimeMonth, datetimeDay, datetimeHour, datetimeMinute, datetimeSeconds);
                }
                catch
                {
                    result.RTC = result.ValidNavigDateTime;
                }
            }

            //Voltage and etc.
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

            result.RFIDNo = "";

            return result;
        }

        private static float ConvertGlonassToBaseCoordinat(float coordinate)
        {
            var degrees = Math.Floor(coordinate);
            var result = degrees * 100 + 60 * (coordinate - degrees);
            return (float)result;
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

            sb.Append("|");
            sb.AppendFormat("{0}", VERSION);

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

            if (State == 'Z')
            {
                sb.Append("000000000000000000000000000000000000000000000000000000000000");
            }
            else
            {
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
            }

            return sb.ToString();
        }

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

        public double LatitudeOrig { get; set; }
        public double Latitude { get; set; }
        public char LatitudeLetter { get; set; }

        public double LongitudeOrig { get; set; }
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

        public string VERSION = "";
    }
}