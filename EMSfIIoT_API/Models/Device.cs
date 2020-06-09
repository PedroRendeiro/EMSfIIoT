using System;
using System.Net;

namespace EMSfIIoT_API.Models
{
    public class Device
    {
        public DateTime Date { get; set; }

        public string Uri { get; set; }

        public Guid Id { get; set; }

        public Gateway Gateway { get; set; }
    }

    [Serializable]
    public class Settings
    {
        public int Framesize { get; set; }
        public int Quality { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }
        public int Saturation { get; set; }
        public int Sharpness { get; set; }
        public int Special_effect { get; set; }
        public int Wb_mode { get; set; }
        public int Awb { get; set; }
        public int Awb_gain { get; set; }
        public int Aec { get; set; }
        public int Aec2 { get; set; }
        public int Ae_level { get; set; }
        public int Aec_value { get; set; }
        public int Agc { get; set; }
        public int Agc_gain { get; set; }
        public int Gainceiling { get; set; }
        public int Bpc { get; set; }
        public int Wpc { get; set; }
        public int Raw_gma { get; set; }
        public int Lenc { get; set; }
        public int Vflip { get; set; }
        public int Hmirror { get; set; }
        public int Dcw { get; set; }
        public int Colorbar { get; set; }
        public int Led_intensity { get; set; }
    }
}