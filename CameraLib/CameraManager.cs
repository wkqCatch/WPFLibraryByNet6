using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using PvDotNet;

namespace CameraLib
{
    public class CameraManager : IDisposable
    {
        public event EventHandler? ConnectCompleteEvent;

        public List<CameraDevice> CameraList { get; set; } = new List<CameraDevice>();

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                DisconnectAllDevice();
                _disposed = true;
            }
        }

        async public void ConnectAllDeviceAsync()
        {
            await Task.Run(() =>
            {
                DisconnectAllDevice();

                using (PvSystem lSystem = new())
                {
                    lSystem.Find();

                    List<CameraDevice> lDeviceList = new();
                    List<Task<bool>> lTaskList = new();

                    for (uint nInterfaceCounter = 0; nInterfaceCounter < lSystem.InterfaceCount; nInterfaceCounter++)
                    {
                        PvInterface interfaceTemp = lSystem.GetInterface(nInterfaceCounter);

                        for (uint nDeviceCounter = 0; nDeviceCounter < interfaceTemp.DeviceCount; nDeviceCounter++)
                        {
                            try
                            {
                                PvDeviceInfo deviceInfoTemp = interfaceTemp.GetDeviceInfo(nDeviceCounter);

                                string strNewIPAddr = GetNewIPAddr(interfaceTemp.IPAddress);
                                PvDevice.SetIPConfiguration(deviceInfoTemp.MACAddress, strNewIPAddr);

                                CameraDevice lCamDevice = new();

                                Task<bool> lConnectTask = lCamDevice.ConnectDeviceAsync(strNewIPAddr);

                                lTaskList.Add(lConnectTask);
                                lDeviceList.Add(lCamDevice);
                            }
                            catch (PvException ex)
                            {
                                Trace.WriteLine(ex.Message);
                            }
                        }
                    }

                    if (lTaskList.Count > 0)
                    {
                        Trace.WriteLine($"有搜寻到相机");

                        Task.WaitAll(lTaskList.ToArray());

                        Trace.WriteLine($"所有相机连接完成");

                        for (int nCounter = 0; nCounter < lDeviceList.Count; nCounter++)
                        {
                            if (lDeviceList[nCounter].IsConnected)
                            {
                                CameraList.Add(lDeviceList[nCounter]);
                            }
                            else
                            {
                                lDeviceList[nCounter].Dispose();
                            }
                        }
                    }
                }

                Trace.WriteLine($"连接的相机个数:{CameraList.Count}");
            });

            ConnectCompleteEvent?.Invoke(this, EventArgs.Empty);
        }

        public void DisconnectAllDevice()
        {
            if (CameraList.Count < 1)
            {
                return;
            }

            List<Task> lTaskList = new List<Task>();

            for (int nCounter = 0; nCounter < CameraList.Count; nCounter++)
            {
                Task lTask = Task.Run(CameraList[nCounter].Dispose);
                lTaskList.Add(lTask);
            }

            Task.WaitAll(lTaskList.ToArray());

            CameraList.Clear();
        }

        static string GetNewIPAddr(string strOldIPAddr)
        {
            IPAddress IPAddr = IPAddress.Parse(strOldIPAddr);
            byte[] IPBytes = IPAddr.GetAddressBytes();
            if (IPBytes.Length > 3)
            {
                IPBytes[3] += 1;
                return new IPAddress(IPBytes).ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
