using BitConverter;
using EchoDotNetLite.Models;
using EchoDotNetLite.Specifications;
using System.Text;

namespace BRouteController;


public class 低圧スマート電力量メータ
{
    public EchoObjectInstance EchoObjectInstance { get; private set; }
    public EchoNode EchoNode { get; private set; }
    public 低圧スマート電力量メータ(EchoNode echoNode, EchoObjectInstance echoObjectInstance)
    {
        this.EchoNode = echoNode;
        this.EchoObjectInstance = echoObjectInstance;
        if (echoObjectInstance.Spec != 機器.住宅設備関連機器.低圧スマート電力量メータ)
        {
            throw new Exception("invalid device class");
        }
    }

    public decimal? 積算電力量計測値_正方向計測値
    {
        get
        {
            return 積算電力量計測値(0xE0);
        }
    }

    public decimal? 積算電力量計測値_逆方向計測値
    {
        get
        {
            return 積算電力量計測値(0xE3);
        }
    }

    private decimal? 積算電力量計測値(byte code)
    {
        var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == code).FirstOrDefault();
        if (prop == null)
        {
            return null;
        }
        if (prop.Value.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFE }))
        {
            return null;
        }
        //積算電力量有効桁数（EPC = 0xD7）
        byte? d7 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xD7).FirstOrDefault()?.Value[0];
        //係数（EPC=0xD3）
        byte[]? d3 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xD3).FirstOrDefault()?.Value;
        //積算電力量単位（EPC=0xE1）
        byte? e1 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xE1).FirstOrDefault()?.Value[0];
        if (d7 == null || d3 == null || e1 == null)
        {
            return null;
        }
        var 係数 = EndianBitConverter.BigEndian.ToUInt32(d3, 0);
        var 積算電力量単位 = e1 switch
        {
            0x00 => 1m,
            0x01 => 0.1m,
            0x02 => 0.01m,
            0x03 => 0.001m,
            0x04 => 0.0001m,
            0x0A => 10m,
            0x0B => 100m,
            0x0C => 1000m,
            0x0D => 10000m,
            _ => throw new InvalidOperationException($"積算電力量単位が不正値:{e1}"),
        };
        return EndianBitConverter.BigEndian.ToUInt32(prop.Value, 0) * 係数 * 積算電力量単位;
    }

    public (long datetime, decimal? kWh)? 定時積算電力量計測値_正方向計測値
    {
        get
        {
            return 定時積算電力量計測値(0xEA);
        }
    }

    public (long datetime, decimal? kWh)? 定時積算電力量計測値_逆方向計測値
    {
        get
        {
            return 定時積算電力量計測値(0xEB);
        }
    }

    private (long datetime, decimal? kWh)? 定時積算電力量計測値(byte code)
    {
        var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == code).FirstOrDefault();
        if (prop == null)
        {
            return null;
        }
        var ymdhms = prop.Value[0..7];
        var datetime = new DateTimeOffset(EndianBitConverter.BigEndian.ToInt16(ymdhms, 0), ymdhms[2], ymdhms[3], ymdhms[4], ymdhms[5], ymdhms[6], TimeSpan.FromHours(9)); ;
        var val = prop.Value[7..11];
        if (val.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFE }))
        {
            return (datetime.ToUnixTimeMilliseconds(), null);
        }
        //積算電力量有効桁数（EPC = 0xD7）
        byte? d7 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xD7).FirstOrDefault()?.Value[0];
        //係数（EPC=0xD3）
        byte[]? d3 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xD3).FirstOrDefault()?.Value;
        //積算電力量単位（EPC=0xE1）
        byte? e1 = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xE1).FirstOrDefault()?.Value[0];
        if (d7 == null || d3 == null || e1 == null)
        {
            return (datetime.ToUnixTimeMilliseconds(), null);
        }
        var 係数 = EndianBitConverter.BigEndian.ToUInt32(d3, 0);
        var 積算電力量単位 = e1 switch
        {
            0x00 => 1m,
            0x01 => 0.1m,
            0x02 => 0.01m,
            0x03 => 0.001m,
            0x04 => 0.0001m,
            0x0A => 10m,
            0x0B => 100m,
            0x0C => 1000m,
            0x0D => 10000m,
            _ => throw new InvalidOperationException($"積算電力量単位が不正値:{e1}"),
        };
        var kWh = EndianBitConverter.BigEndian.ToUInt32(val, 0) * 係数 * 積算電力量単位;
        return (datetime.ToUnixTimeMilliseconds(), kWh);
    }

    public int? 瞬時電力計測値
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xE7).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            return EndianBitConverter.BigEndian.ToInt32(prop.Value, 0);
        }
    }

    public (decimal r, decimal t)? 瞬時電流計測値
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0xE8).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            return (EndianBitConverter.BigEndian.ToInt16(prop.Value, 0) * 0.1m, EndianBitConverter.BigEndian.ToInt16(prop.Value, 2) * 0.1m);
        }
    }

    public long? 現在年月日時刻
    {
        get
        {
            var ymd = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x98).FirstOrDefault()?.Value;
            if (ymd == null)
            {
                return null;
            }
            var hm = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x97).FirstOrDefault()?.Value;
            if (hm == null)
            {
                return null;
            }
            var datetime = new DateTimeOffset(EndianBitConverter.BigEndian.ToInt16(ymd, 0), ymd[2], ymd[3], hm[0], hm[1], 0, TimeSpan.FromHours(9));
            return datetime.ToUnixTimeMilliseconds();
        }
    }

    public string? 製造番号
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x8D).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(prop.Value).Trim().Replace("\0", "");
        }
    }

    public string? メーカコード
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x8A).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            return BytesConvert.ToHexString(prop.Value);
        }
    }

    public string? 規格Version情報
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x82).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            return $"Release:{Encoding.ASCII.GetString(prop.Value[2..3])},Revision:{(int)prop.Value[3]}";
        }
    }
    public string? 設置場所
    {
        get
        {
            var prop = EchoObjectInstance.GETProperties.Where(p => p.Spec.Code == 0x81).FirstOrDefault();
            if (prop == null)
            {
                return null;
            }
            var val = prop.Value[0];
            if (val == 0b00000000)
            {
                return "設置場所未設定";
            }
            if (val == 0b11111111)
            {
                return "設置場所不定";
            }
            if (val == 0b00000001)
            {
                return "位置情報定義";
            }
            if (val >= 0b00000010 && val <= 0b00000111)
            {
                return $"for future reserved:({Convert.ToString(val, 2)})";
            }
            if (val >= 0b10000000 && val <= 0b11111110)
            {
                return $"フリー定義:({Convert.ToString(val, 2)})";
            }

            var 設置 = (val & 0b11111000) switch
            {
                0b00001000 => "居間、リビング",
                0b00010000 => "食堂、ダイニング",
                0b00011000 => "台所、キッチン",
                0b00100000 => "浴室、バス",
                0b00101000 => "トイレ",
                0b00110000 => "洗面所、脱衣所",
                0b00111000 => "廊下",
                0b01000000 => "部屋",
                0b01001000 => "階段",
                0b01010000 => "玄関",
                0b01011000 => "納戸",
                0b01100000 => "庭、外周",
                0b01101000 => "車庫",
                0b01110000 => "ベランダ、バルコニー",
                0b01111000 => "その他",
                _ => "定義外orバグ",
            };
            var 場所 = val & 0b00000111;
            return $"{設置}({場所})";
        }
    }

}