using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using PvDotNet;

namespace CameraLib
{
    public class CameraManager : IDisposable
    {
        public event EventHandler ConnectCompleteEvent;

        public List<CameraDevice> MCameraList { get; set; } = new List<CameraDevice>();

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

        async public Task ConnectAllDeviceAsync()
        {
            await Task.Run(() =>
            {
                DisconnectAllDevice();

                using (PvSystem lSystem = new PvSystem())
                {
                    lSystem.Find();

                    List<CameraDevice> lDeviceList = new List<CameraDevice>();
                    List<Task<bool>> lTaskList = new List<Task<bool>>();

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

                                CameraDevice lCamDevice = new CameraDevice();

                                Task<bool> lConnectTask = Task.Run(() =>
                                {
                                    return lCamDevice.ConnectDevice(strNewIPAddr);
                                });

                                lTaskList.Add(lConnectTask);
                                lDeviceList.Add(lCamDevice);
                            }
                            catch (PvException ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    if (lTaskList.Count > 0)
                    {
                        Task.WaitAll(lTaskList.ToArray());

                        for (int nCounter = 0; nCounter < lDeviceList.Count; nCounter++)
                        {
                            if (lDeviceList[nCounter].IsConnected())
                            {
                                MCameraList.Add(lDeviceList[nCounter]);
                            }
                            else
                            {
                                lDeviceList[nCounter].Dispose();
                            }
                        }
                    }
                }

                Console.WriteLine($"连接的相机个数:{MCameraList.Count}");
            });

            ConnectCompleteEvent?.Invoke(this, EventArgs.Empty);
        }

        public void DisconnectAllDevice()
        {
            if (MCameraList.Count < 1)
            {
                return;
            }

            List<Task> lTaskList = new List<Task>();

            for (int nCounter = 0; nCounter < MCameraList.Count; nCounter++)
            {
                Task lTask = Task.Run(MCameraList[nCounter].Dispose);
                lTaskList.Add(lTask);
            }

            Task.WaitAll(lTaskList.ToArray());

            MCameraList.Clear();

            Console.WriteLine("所有相机断开连接了");
        }

        string GetNewIPAddr(string strOldIPAddr)
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
