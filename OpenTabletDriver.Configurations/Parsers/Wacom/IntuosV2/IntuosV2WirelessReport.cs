using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver.Configurations.Parsers.Wacom.IntuosV2
{
    public struct IntuosV2WirelessReport : ITabletReport, IHoverReport, IConfidenceReport, ITiltReport, IEraserReport
    {
        public IntuosV2WirelessReport(byte[] report)
        {
            Raw = report;

            Position = new Vector2
            {
                X = Unsafe.ReadUnaligned<ushort>(ref report[2]) | (report[3] << 8),
                Y = Unsafe.ReadUnaligned<ushort>(ref report[4]) | (report[5] << 8)
            };
            Pressure = Unsafe.ReadUnaligned<ushort>(ref report[6]);

            var penByte = report[1];
            PenButtons = new bool[]
            {
                penByte.IsBitSet(1),
                penByte.IsBitSet(2)
            };
            HighConfidence = report[1].IsBitSet(5);
            HoverDistance = report[16];
        }

        public byte[] Raw { set; get; }
        public Vector2 Position { set; get; }
        public Vector2 Tilt { set; get; }
        public uint Pressure { set; get; }
        public bool Eraser { set; get; }
        public bool[] PenButtons { set; get; }
        public bool HighConfidence { set; get; }
        public uint HoverDistance { set; get; }
    }
}
