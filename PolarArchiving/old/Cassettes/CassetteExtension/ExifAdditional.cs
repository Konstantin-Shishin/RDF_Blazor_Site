using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exif
{
    public enum ColorRepresentation
    {
        sRGB,
        Uncalibrated
    }

    public enum FlashMode
    {
        FlashFired,
        FlashDidNotFire
    }

    public enum ExposureMode
    {
        Manual,
        NormalProgram,
        AperturePriority,
        ShutterPriority,
        LowSpeedMode,
        HighSpeedMode,
        PortraitMode,
        LandscapeMode,
        Unknown
    }

    public enum WhiteBalanceMode
    {
        Daylight,
        Fluorescent,
        Tungsten,
        Flash,
        StandardLightA,
        StandardLightB,
        StandardLightC,
        D55,
        D65,
        D75,
        Other,
        Unknown
    }
}
