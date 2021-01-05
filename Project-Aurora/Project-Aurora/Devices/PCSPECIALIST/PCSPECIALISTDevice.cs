using Aurora.Settings;
using Aurora.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DK = Aurora.Devices.DeviceKeys;

namespace Aurora.Devices.PCSPECIALIST
{
    public class PCSPECIALISTDevice : IDevice
    {
        private VariableRegistry variableRegistry = null;
        private Stopwatch watch = new Stopwatch();
        private long lastUpdateTime = 0;

        private List<HidDeviceBase> connectedDevices = new List<HidDeviceBase>();

        public string DeviceName => "PCSPECIALIST";

        protected string DeviceInfo => string.Join(", ", connectedDevices.Select(hd => hd.GetType().Name));

        public string DeviceDetails => IsInitialized
            ? $"Initialized{(string.IsNullOrWhiteSpace(DeviceInfo) ? "" : ": " + DeviceInfo)}"
            : "Not Initialized";

        public string DeviceUpdatePerformance => IsInitialized
            ? lastUpdateTime + " ms"
            : "";

        public bool IsInitialized => connectedDevices.Count != 0;

        public VariableRegistry RegisteredVariables
        {
            get
            {
                if (variableRegistry == null)
                {
                    variableRegistry = new VariableRegistry();

                    variableRegistry.Register($"{DeviceName}_enable_shutdown_color", false, "Enable shutdown color");
                    variableRegistry.Register($"{DeviceName}_shutdown_color", new RealColor(Color.FromArgb(255, 255, 255, 255)), "Shutdown color");
                }

                return variableRegistry;
            }
        }

        public bool Initialize()
        {
            if (!IsInitialized)
            {
                var devices = from type in Assembly.GetExecutingAssembly().GetTypes()
                              where typeof(HidDeviceBase).IsAssignableFrom(type) && !type.IsAbstract
                              let inst = (HidDeviceBase)Activator.CreateInstance(type)
                              select inst;

                foreach (var device in devices)
                {
                    if (device.Connect())
                    {
                        connectedDevices.Add(device);
                    }
                }
            }

            return IsInitialized;
        }

        public void Reset()
        {
            Shutdown();
            Initialize();
        }

        public void Shutdown()
        {
            if (IsInitialized)
            {
                if (Global.Configuration.VarRegistry.GetVariable<bool>($"{DeviceName}_enable_shutdown_color"))
                {
                    Color color = Global.Configuration.VarRegistry.GetVariable<RealColor>($"{DeviceName}_shutdown_color").GetDrawingColor();

                    foreach (var dev in connectedDevices )
                    {
                        // Apply shutdown color
                        dev.SetColor(color.R, color.G, color.B);
                    }
                }

                foreach (var dev in connectedDevices)
                {
                    // Disconnect devices
                    dev.Disconnect();
                }

                connectedDevices.Clear();
            }
        }

        public bool UpdateDevice(Dictionary<DK, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            var color = MergeKeyColors(keyColors);

            foreach (var dev in connectedDevices)
            {
                if (color != dev.LastColor)
                {
                    dev.LastColor = color;
                    dev.SetColor(color.R, color.G, color.B);
                }
            }

            return true;
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false)
        {
            watch.Restart();

            bool result = UpdateDevice(colorComposition.keyColors, e, forced);

            watch.Stop();
            lastUpdateTime = watch.ElapsedMilliseconds;

            return result;
        }

        public Color MergeKeyColors(Dictionary<DK, Color> keyColors)
        {
            int keys = keyColors.Count;

            int r = 0;
            int g = 0;
            int b = 0;
            int a = 0;

            foreach (var item in keyColors)
            {
                r += item.Value.R;
                g += item.Value.G;
                b += item.Value.B;
                a += item.Value.A;
            }

            return Color.FromArgb(a / keys, r / keys, g / keys, b / keys);
        }
    }
}
