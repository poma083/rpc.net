using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PDUDatas
{
    public class Tools
    {
        public static void CopyIntToArray(int x, byte[] ar, int pos)
        {
            byte[] arTmp;
            ConvertIntToArray(x, out arTmp);
            Array.Copy(arTmp, 0, ar, pos, 4);
        }

        public static void ConvertIntToArray(int x, out byte[] ar)
        {
            ar = new byte[4];
            ar[3] = Convert.ToByte(x & 0xFF);
            ar[2] = Convert.ToByte((x >> 8) & 0xFF);
            ar[1] = Convert.ToByte((x >> 16) & 0xFF);
            ar[0] = Convert.ToByte((x >> 24) & 0xFF);
        }
        public static void ConvertUIntToArray(uint x, out byte[] ar)
        {
            ar = new byte[4];
            ar[3] = Convert.ToByte(x & 0xFF);
            ar[2] = Convert.ToByte((x >> 8) & 0xFF);
            ar[1] = Convert.ToByte((x >> 16) & 0xFF);
            ar[0] = Convert.ToByte((x >> 24) & 0xFF);
        }
        public static void CopyIntToArray(uint x, byte[] ar, int pos)
        {
            byte[] arTmp;
            ConvertUIntToArray(x, out arTmp);
            Array.Copy(arTmp, 0, ar, pos, 4);
        }

        public static string ConvertIntToHexString(int x)
        {
            string resp;
            resp = "0x";
            resp += GetHexByte(Convert.ToByte((x >> 24) & 0xFF));
            resp += GetHexByte(Convert.ToByte((x >> 16) & 0xFF));
            resp += GetHexByte(Convert.ToByte((x >> 8) & 0xFF));
            resp += GetHexByte(Convert.ToByte(x & 0xFF));
            return resp;

        }
        public static string ConvertUIntToHexString(uint x)
        {
            string resp;
            resp = "0x";
            resp += GetHexByte(Convert.ToByte((x >> 24) & 0xFF));
            resp += GetHexByte(Convert.ToByte((x >> 16) & 0xFF));
            resp += GetHexByte(Convert.ToByte((x >> 8) & 0xFF));
            resp += GetHexByte(Convert.ToByte(x & 0xFF));
            return resp;

        }
        public static bool ConvertArrayToUInt(byte[] ar, int offset, ref uint x)
        {
            if (ar.Length < offset + 4)
            {
                return false;
            }
            x = x & 0x00000000;
            x = x | Convert.ToUInt32(ar[offset + 3]);
            x = x | (Convert.ToUInt32(ar[offset + 2]) << 8);
            x = x | (Convert.ToUInt32(ar[offset + 1]) << 16);
            x = x | (Convert.ToUInt32(ar[offset + 0]) << 24);
            return true;
        }
        public static bool ConvertArrayToInt(byte[] ar, int offset, ref int x)
        {
            if (ar.Length < offset + 4)
            {
                return false;
            }
            x = x & 0x00000000;
            x = x | Convert.ToInt32(ar[offset + 3]);
            x = x | (Convert.ToInt32(ar[offset + 2]) << 8);
            x = x | (Convert.ToInt32(ar[offset + 1]) << 16);
            x = x | (Convert.ToInt32(ar[offset + 0]) << 24);
            return true;
        }

        public static string ConvertArrayToHexString(byte[] ar, int len)
        {
            int i;
            string sTmp;
            string HEX = "0123456789ABCDEF";
            sTmp = "";
            for (i = 0; i < len; i++)
            {
                sTmp = sTmp + HEX[(ar[i] >> 4) & 0x0F];
                sTmp = sTmp + HEX[ar[i] & 0x0F];
            }
            return sTmp;
        }
        public static string ConvertArrayToString(byte[] ar, int len)
        {
            string sTmp = "";
            sTmp = Encoding.ASCII.GetString(ar, 0, len);
            return sTmp;
        }
        public static string GetHexFromByte(byte a)
        {
            string sTmp;
            string HEX = "0123456789ABCDEF";
            sTmp = "0x";
            sTmp = sTmp + HEX[(a >> 4) & 0x0F];
            sTmp = sTmp + HEX[a & 0x0F];
            return sTmp;
        }
        public static string GetHexByte(byte a)
        {
            string sTmp;
            string HEX = "0123456789ABCDEF";
            sTmp = "";
            sTmp = sTmp + HEX[(a >> 4) & 0x0F];
            sTmp = sTmp + HEX[a & 0x0F];
            return sTmp;
        }
        public static byte[] ConvertHexStringToByteArray(string str)
        {
            if ((str.Length & 1) == 1)
                str += "0";
            int i, j, n;
            n = str.Length;
            byte a;
            byte[] x = new byte[n / 2];
            for (i = 0, j = 0; i < n; i++, j++)
            {
                a = getHexVal(str[i]);
                a <<= 4;
                a = Convert.ToByte(a | (getHexVal(str[i + 1]) & 0x0F));
                x[j] = a;
                i++;
            }
            return x;
        }
        public static byte[] ConvertStringToByteArray(string str)
        {
            int i, n;
            n = str.Length;
            byte[] x = new byte[n];
            for (i = 0; i < n; i++)
            {
                x[i] = (byte)str[i];
            }
            return x;
        }
        public static byte getHexVal(char ch)
        {
            byte b;
            b = 0;
            switch (ch)
            {
                case '0':
                    b = 0;
                    break;
                case '1':
                    b = 1;
                    break;
                case '2':
                    b = 2;
                    break;
                case '3':
                    b = 3;
                    break;
                case '4':
                    b = 4;
                    break;
                case '5':
                    b = 5;
                    break;
                case '6':
                    b = 6;
                    break;
                case '7':
                    b = 7;
                    break;
                case '8':
                    b = 8;
                    break;
                case '9':
                    b = 9;
                    break;
                case 'A':
                    b = 10;
                    break;
                case 'B':
                    b = 11;
                    break;
                case 'C':
                    b = 12;
                    break;
                case 'D':
                    b = 13;
                    break;
                case 'E':
                    b = 14;
                    break;
                case 'F':
                    b = 15;
                    break;
                case 'a':
                    b = 10;
                    break;
                case 'b':
                    b = 11;
                    break;
                case 'c':
                    b = 12;
                    break;
                case 'd':
                    b = 13;
                    break;
                case 'e':
                    b = 14;
                    break;
                case 'f':
                    b = 15;
                    break;
            }
            return b;
        }
        public static string ConvertNullEndArrayToHexString(byte[] ar, int len)
        {
            int i;
            string sTmp;
            string HEX = "0123456789ABCDEF";
            sTmp = "";
            for (i = 0; i < len; i++)
            {
                if (ar[i] == 0x00)
                    break;
                sTmp = sTmp + HEX[(ar[i] >> 4) & 0x0F];
                sTmp = sTmp + HEX[ar[i] & 0x0F];
            }
            return sTmp;
        }

        public static bool Get2ByteIntFromArray(byte[] ar, int pos, int length, out int res)
        {
            res = 0;
            bool result = false;
            int ps = pos;
            if (ps <= length)
                res = ar[ps];
            else
                return false;
            res <<= 8;
            ps++;
            if (ps <= length)
            {
                res |= ar[ps];
                result = true;
            }
            return result;
        }
        public static string GetString(string inpString, int maxLen, string defValue)
        {
            if (inpString == null)
                return defValue;
            if (inpString.Length > maxLen)
                return inpString.Substring(0, maxLen);
            return inpString;
        }
        public static string GetString(string inpString, string defValue)
        {
            if (inpString == null)
                return defValue;
            return inpString;
        }
        public static string GetDateString(DateTime pTime)
        {
            if (pTime == DateTime.MinValue)
                return "";
            return pTime.ToString("yyMMddHHmmss000R");
        }
        public static DateTime SMPPDateStrToDate(string SMPPDateTime)
        {
            DateTime result = DateTime.MinValue;
            //SMPPDateTime = "101209123344508-";

            if (SMPPDateTime.Length < 16)
            {
                result = DateTime.Now;
            }
            else
            {
                int year = 0;//0..99
                int month = 1;//1..12
                int day = 1;//1..31
                int hour = 0;//0..23
                int minute = 0;//0..59
                int seccond = 0;//0..59
                int partSeccond = 0;//0..9
                int ShiftQuarterOfHour = 0;//0..48

                year = int.Parse(SMPPDateTime.Substring(0, 2));
                month = int.Parse(SMPPDateTime.Substring(2, 2));
                day = int.Parse(SMPPDateTime.Substring(4, 2));
                hour = int.Parse(SMPPDateTime.Substring(6, 2));
                minute = int.Parse(SMPPDateTime.Substring(8, 2));
                seccond = int.Parse(SMPPDateTime.Substring(10, 2));
                partSeccond = int.Parse(SMPPDateTime.Substring(12, 1));
                ShiftQuarterOfHour = int.Parse(SMPPDateTime.Substring(13, 2));

                string relativ = SMPPDateTime.Substring(15, 1);
                switch (relativ)
                {
                    case "+":
                    case "-":
                        result = result.AddYears(2000);
                        break;
                    case "R":
                        break;
                }
                result = result.AddYears(year - 1);
                result = result.AddMonths(month - 1);
                result = result.AddDays(day - 1);
                result = result.AddHours(hour);
                result = result.AddMinutes(minute);
                result = result.AddSeconds(seccond);
                result = result.AddMilliseconds(partSeccond * 100);
                switch (relativ)
                {
                    case "+":
                        result = result.AddHours(ShiftQuarterOfHour / 4);
                        break;
                    case "-":
                        result = result.AddHours(-ShiftQuarterOfHour / 4);
                        break;
                }
            }
            return result;
        }

        public static byte GetDataCoding(string text)
        {
            if (UTF2BigEndianUnicode(text).Length != ASCII2BigEndianUnicode(text).Length)
            {
                return 8;
            }
            else
            {
                return 0;
            }
        }

        public static string UTF2BigEndianUnicode(string s)
        {
            Encoding ui = Encoding.BigEndianUnicode;
            Encoding u8 = Encoding.UTF8;
            return ui.GetString(u8.GetBytes(s));
        } // u2i
        public static string BigEndianUnicode2UTF(string s)
        {
            Encoding ui = Encoding.BigEndianUnicode;
            Encoding u8 = Encoding.UTF8;
            return u8.GetString(ui.GetBytes(s));
        } // i2u


        public static string ASCII2BigEndianUnicode(string s)
        {
            Encoding ui = Encoding.BigEndianUnicode;
            Encoding a = Encoding.ASCII;
            return ui.GetString(a.GetBytes(s));
        } // a2i
        public static string BigEndianUnicode2ASCII(string s)
        {
            Encoding ui = Encoding.BigEndianUnicode;
            Encoding a = Encoding.ASCII;
            return a.GetString(ui.GetBytes(s));
        } // i2a

        public static bool IsDigital(string s)
        {
            Regex re = new Regex(@"^[\d]+$");
            return re.Match(s).Success;
        }

        public static string GetRandomString(int Len)
        {
            string ret = "";
            int i;
            Random rnd = new Random(unchecked((int)DateTime.Now.Ticks));
            for (i = 0; i < Len; i++)
            {
                ret += Convert.ToString(rnd.Next(9));
            }
            return ret;
        }//GetRandomString
    }
}
