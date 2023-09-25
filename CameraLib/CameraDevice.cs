
using HalconDotNet;
using PvDotNet;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture.Core;

namespace CameraLib
{
    public class CameraDevice : IDisposable
    {
        public object AcquisitionLocker { get; set; } = new object();
        readonly PvDevice mDevice = new();
        readonly PvStream mStream = new();
        PvPipeline? mPipeline;
        readonly PvBuffer mConvertBuffer = new();
        readonly PvBufferConverter mBufferConverter = new(16);

        public event EventHandler? AcquisitionEvent;

        public bool IsConnected => mDevice.IsConnected;

        public PvGenParameterArray? DeviceParamArray
        {
            get { return mDevice.GenParameters; }
        }

        public HImage AcquisitionImage { get; set; } = new HImage();
        Task? mAcquisitionTask;
        CancellationTokenSource? mAcquisitionTokenSource;
        public PvAcquisitionStateManager? AcquisitionManager { get; set; }
        private bool mDisposed;

        public CameraDevice()
        {
            mConvertBuffer.Image.Alloc(512, 512, PvPixelType.RGB8Packed);
        }

        ~CameraDevice()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }

                // 释放非托管资源
                DisConnectDevice();
                mDisposed = true;
            }
        }

        public async Task<bool> ConnectDeviceAsync(string strIP)
        {
            return await Task.Run(async () =>
                           {
                               // Connecting
                               try
                               {
                                   // Connect to device
                                   mDevice.Connect(strIP);

                                   // Open stream
                                   mStream.Open(strIP);

                                   // Create pipeline
                                   mPipeline = new PvPipeline(mStream);
                               }
                               catch (PvException ex)
                               {
                                   Trace.WriteLine(ex.Message);
                                   await DisConnectDeviceAsync();
                                   return false;
                               }

                               // Configuring
                               try
                               {
                                   // Negotiate packet size. Failure means default value as configured on 
                                   // the device is used.
                                   mDevice.NegotiatePacketSize();

                                   // Set stream destination
                                   mDevice.SetStreamDestination(mStream.LocalIPAddress, mStream.LocalPort);

                                   // Read payload size, pre-allocate buffers of the pipeline
                                   long lPayloadSize = mDevice.GenParameters.GetIntegerValue("PayloadSize");
                                   mPipeline.BufferSize = (uint)lPayloadSize;

                                   // Set buffer count. Use more buffers (at expense of using more memory) to 
                                   // eleminate missing block IDs
                                   mPipeline.BufferCount = 16;
                               }
                               catch (PvException ex)
                               {
                                   Trace.WriteLine(ex.Message);
                                   await DisConnectDeviceAsync();
                                   return false;
                               }

                               // StartStreaming
                               try
                               {
                                   // Start (arm) the pipeline
                                   mPipeline.Start();

                                   // Start display thread
                                   mAcquisitionTokenSource = new CancellationTokenSource();

                                   mAcquisitionTask = Task.Factory.StartNew(AcquisitonThread,
                                                                            mAcquisitionTokenSource.Token,
                                                                            TaskCreationOptions.LongRunning,
                                                                            TaskScheduler.Default);

                                   // Create AcquisitionManager
                                   AcquisitionManager = new PvAcquisitionStateManager(mDevice, mStream);
                               }
                               catch (PvException ex)
                               {
                                   Trace.WriteLine(ex.Message);
                                   await DisConnectDeviceAsync();
                                   return false;
                               }
                               return true;
                           });
        }

        public bool ConnectDevice(string strIP)
        {
            // Connecting
            try
            {
                // Connect to device
                mDevice.Connect(strIP);

                // Open stream
                mStream.Open(strIP);

                // Create pipeline
                mPipeline = new PvPipeline(mStream);
            }
            catch (PvException ex)
            {
                Trace.WriteLine(ex.Message);
                DisConnectDevice();
                return false;
            }

            // Configuring
            try
            {
                // Negotiate packet size. Failure means default value as configured on 
                // the device is used.
                mDevice.NegotiatePacketSize();

                // Set stream destination
                mDevice.SetStreamDestination(mStream.LocalIPAddress, mStream.LocalPort);

                // Read payload size, pre-allocate buffers of the pipeline
                long lPayloadSize = mDevice.GenParameters.GetIntegerValue("PayloadSize");
                mPipeline.BufferSize = (uint)lPayloadSize;

                // Set buffer count. Use more buffers (at expense of using more memory) to 
                // eleminate missing block IDs
                mPipeline.BufferCount = 16;
            }
            catch (PvException ex)
            {
                Trace.WriteLine(ex.Message);
                DisConnectDevice();
                return false;
            }

            // StartStreaming
            try
            {
                // Start (arm) the pipeline
                mPipeline.Start();

                // Start display thread
                mAcquisitionTokenSource = new CancellationTokenSource();

                mAcquisitionTask = Task.Factory.StartNew(AcquisitonThread,
                                                         mAcquisitionTokenSource.Token,
                                                         TaskCreationOptions.LongRunning,
                                                         TaskScheduler.Default);

                // Create AcquisitionManager
                AcquisitionManager = new PvAcquisitionStateManager(mDevice, mStream);
            }
            catch (PvException ex)
            {
                Trace.WriteLine(ex.Message);
                DisConnectDevice();
                return false;
            }

            return true;
        }

        public async Task DisConnectDeviceAsync()
        {
            // Stop acquisition
            if (AcquisitionManager != null)
            {
                if (AcquisitionManager.State == PvAcquisitionState.Locked)
                {
                    AcquisitionManager.Stop();
                }
            }
            await Task.Run(() =>
            {
                try
                {
                    // Stop display thread
                    if (mAcquisitionTask != null)
                    {
                        mAcquisitionTokenSource?.Cancel();
                        mAcquisitionTask.Wait();
                        mAcquisitionTask = null;
                        mAcquisitionTokenSource = null;
                    }

                    // Stop the pipeline
                    mPipeline?.Stop();

                    // Close Stream
                    mStream.Close();

                    // Disconnect Device
                    mDevice.Disconnect();

                    // Free buffer
                    mConvertBuffer.Free();
                }
                catch (PvException ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            });
        }

        public void DisConnectDevice()
        {
            try
            {
                // Stop acquisition
                if (AcquisitionManager != null)
                {
                    if (AcquisitionManager.State == PvAcquisitionState.Locked)
                    {
                        AcquisitionManager.Stop();
                    }
                }

                // Stop display thread
                if (mAcquisitionTask != null)
                {
                    mAcquisitionTokenSource?.Cancel();
                    mAcquisitionTask.Wait();
                    mAcquisitionTask = null;
                }

                // Stop the pipeline
                mPipeline?.Stop();

                // Close Stream
                mStream.Close();

                // Disconnect Device
                mDevice.Disconnect();

                // Free Buffer
                mConvertBuffer.Free();
            }
            catch (PvException ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public void StartAcquisition()
        {
            if (IsConnected)
            {
                return;
            }

            mPipeline?.Reset();
            mStream.Parameters.ExecuteCommand("Reset");
            if (AcquisitionManager != null)
            {
                if (AcquisitionManager.State != PvAcquisitionState.Locked)
                {
                    AcquisitionManager.Start();
                }
            }
        }

        public void StopAcquisition()
        {
            if (AcquisitionManager != null)
            {
                if (AcquisitionManager.State == PvAcquisitionState.Locked)
                {
                    AcquisitionManager.Stop();
                }
            }
        }

        public bool ImageConventer(PvBuffer pvBuffer)
        {
            try
            {
                if (pvBuffer.Image.PixelType == PvPixelType.Mono8)
                {
                    unsafe
                    {
                        AcquisitionImage.GenImage1(
                                            "byte",
                                            (int)pvBuffer.Image.Width,
                                            (int)pvBuffer.Image.Height,
                                            new IntPtr(pvBuffer.DataPointer));

                        Trace.WriteLine("灰度图，不转换");
                    }
                }
                else
                {
                    if (pvBuffer.Image.PixelType != mConvertBuffer.Image.PixelType)
                    {
                        if (mBufferConverter.IsConversionSupported(pvBuffer.Image.PixelType, mConvertBuffer.Image.PixelType))
                        {
                            try
                            {
                                mBufferConverter.Convert(pvBuffer, mConvertBuffer, true);
                                Trace.WriteLine("进行了转换");
                                Trace.WriteLine(mConvertBuffer.Image.Width);
                                Trace.WriteLine(mConvertBuffer.Image.Height);
                                Trace.WriteLine(mConvertBuffer.Image.PixelType);
                            }
                            catch (PvException ex)
                            {
                                Trace.WriteLine(ex.Message);
                                return false;
                            }

                            unsafe
                            {
                                try
                                {
                                    AcquisitionImage.GenImageInterleaved(
                                                        new IntPtr(mConvertBuffer.DataPointer),
                                                        "rgb",
                                                        (int)mConvertBuffer.Image.Width,
                                                        (int)mConvertBuffer.Image.Height,
                                                        0,
                                                        "byte",
                                                        0,
                                                        0,
                                                        0,
                                                        0,
                                                        -1,
                                                        0);
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(ex.Message);
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        unsafe
                        {
                            AcquisitionImage.GenImageInterleaved(
                                                new IntPtr(pvBuffer.DataPointer),
                                                "rgb",
                                                (int)pvBuffer.Image.Width,
                                                (int)pvBuffer.Image.Height,
                                                0,
                                                "byte",
                                                0,
                                                0,
                                                0,
                                                0,
                                                -1,
                                                0);
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return false;
            }
        }

        public void AcquisitonThread()
        {
            PvBuffer? lBuffer = null;
            for (; ; )
            {
                if (mAcquisitionTokenSource!.IsCancellationRequested)
                {
                    return;
                }

                // Retrieve next buffer from acquisition pipeline
                PvResult lResult = mPipeline!.RetrieveNextBuffer(ref lBuffer, 500);
                if (lResult.IsOK)
                {
                    // Operation result of buffer is OK, display
                    if (lBuffer.OperationResult.IsOK)
                    {
                        // ReaderWriterLock locker(this)
                        // {
                        // }
                        Trace.WriteLine("output image");
                        if (ImageConventer(lBuffer))
                        {
                            AcquisitionEvent?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    // We got a buffer (good or not) we must release it back
                    mPipeline.ReleaseBuffer(lBuffer);
                }
            }
        }
    }
}
