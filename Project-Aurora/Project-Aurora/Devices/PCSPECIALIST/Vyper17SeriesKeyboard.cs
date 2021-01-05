using HidLibrary;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Devices.PCSPECIALIST
{
    internal class Vyper17SeriesKeyboard : HidDeviceBase
    {
        private readonly byte Control = (byte)Enums.Control.LightOn;
        private readonly byte Effect = (byte)Enums.Effect.Color;
        private readonly byte Speed = (byte)Enums.Speed.Maximum;
        private readonly byte Light = (byte)Enums.Light.Maximum;
        private readonly byte Direction = (byte)Enums.Direction.None;

        private readonly byte ColorIndex = 0x08;
        private readonly byte Save = 0x00;

        public Vyper17SeriesKeyboard()
        {
            VendorID = 0x048d;
            ProductIDs = new[] { 0xce00 };
            UsagePage = unchecked((short)0xff12);
        }

        public override void SetColor(byte r, byte g, byte b)
        {
            byte[] data = new byte[] { 0x00, 0x14, 0x00, 0x00, r, g, b, 0x00 };

            for (int index = 0; index < ColorIndex; index++)
            {
                data[3] = (byte)(index + 1);
                // Write LED color
                device.WriteFeatureData(data);
            }

            // Write LED effect
            data = new byte[] { 0x00, 0x08, Control, Effect, Speed, Light, ColorIndex, Direction, Save };

            device.WriteFeatureData(data);
        }
    }
}
