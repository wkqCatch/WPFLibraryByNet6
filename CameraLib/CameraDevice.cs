
using HalconDotNet;
using PvDotNet;
using System;
using System.Threading;

using System.Threading.Tasks;

namespace CameraLib
{
    public class CameraDevice : IDisposable
    {
        public object AcquisitionLocker { get; set; } = new object();

        PvDevice mDevice = new PvDevice();
        PvStream mStream = new PvStream();
        PvPipeline mPipeline;

        PvBuffer mConvertBuffer = new PvBuffer();

        PvBufferConverter mBufferConverter = new PvBufferConverter(16);

        public event EventHandler AcquisitionEvent;

        public PvGenParameterArray DeviceParamArray
        {
            get { return mDevice.GenParameters; }
        }

        public HImage AcquisitionImage { get; set; } = new HImage();

        Task mAcquisitionTask;
        CancellationTokenSource mAcquisitionTokenSource;

        public PvAcquisitionStateManager MAcquisitionManager { get; set; }

        private bool _disposed;

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
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }

                // 释放非托管资源
                DisConnectDevice();

                _disposed = true;
            }
        }

        public async Task<bool> ConnectDeviceAsync(string strIP)
        {
            bool bResult = await Task.Run(() =>
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
                                   Console.WriteLine(ex.Message);
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
                                   Console.WriteLine(ex.Message);
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
                                   mAcquisitionTask = new Task(AcquisitonThread, mAcquisitionTokenSource.Token, TaskCreationOptions.LongRunning);
                                   mAcquisitionTask.Start();

                                   // Create AcquisitionManager
                                   MAcquisitionManager = new PvAcquisitionStateManager(mDevice, mStream);
                               }
                               catch (PvException ex)
                               {
                                   Console.WriteLine(ex.Message);
                                   DisConnectDevice();
                                   return false;
                               }

                               return true;
                           });

            return bResult;
        }

        public bool ConnectDevice(string strIP)
        {
            // Connecting
            try
            {
                // Connect to device
                mDevice.Connect(strIP, PvAccessType.Control);

                // Open stream
                mStream.Open(strIP);

                // Create pipeline
                mPipeline = new PvPipeline(mStream);
            }
            catch (PvException ex)
            {
                Console.WriteLine(ex.Message);
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
                Console.WriteLine(ex.Message);
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
                mAcquisitionTask = new Task(AcquisitonThread, mAcquisitionTokenSource.Token, TaskCreationOptions.LongRunning);
                mAcquisitionTask.Start();

                // Create AcquisitionManager
                MAcquisitionManager = new PvAcquisitionStateManager(mDevice, mStream);
            }
            catch (PvException ex)
            {
                Console.WriteLine(ex.Message);
                DisConnectDevice();
                return false;
            }

            return true;
        }

        public async Task DisConnectDeviceAsync()
        {
            // Stop acquisition
            if (MAcquisitionManager != null)
            {
                if (MAcquisitionManager.State == PvAcquisitionState.Locked)
                {
                    MAcquisitionManager.Stop();
                }
            }

            await Task.Run(() =>
            {
                try
                {
                    // Stop display thread
                    if (mAcquisitionTask != null)
                    {
                        mAcquisitionTokenSource.Cancel();
                        mAcquisitionTask.Wait();
                        mAcquisitionTask = null;
                    }

                    // Stop the pipeline
                    if (mPipeline != null)
                    {
                        mPipeline.Stop();
                    }

                    // Close Stream
                    mStream.Close();

                    // Disconnect Device
                    mDevice.Disconnect();

                    // Free buffer
                    mConvertBuffer.Free();
                }
                catch (PvException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        public void DisConnectDevice()
        {
            try
            {
                // Stop acquisition
                if (MAcquisitionManager != null)
                {
                    if (MAcquisitionManager.State == PvAcquisitionState.Locked)
                    {
                        MAcquisitionManager.Stop();
                    }
                }

                // Stop display thread
                if (mAcquisitionTask != null)
                {
                    mAcquisitionTokenSource.Cancel();
                    mAcquisitionTask.Wait();
                    mAcquisitionTask = null;
                }

                // Stop the pipeline
                if (mPipeline != null)
                {
                    mPipeline.Stop();
                }

                // Close Stream
                mStream.Close();

                // Disconnect Device
                mDevice.Disconnect();

                // Free Buffer
                mConvertBuffer.Free();
            }
            catch (PvException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void StartAcquisition()
        {
            if (!mDevice.IsConnected)
            {
                return;
            }

            mPipeline.Reset();
            mStream.Parameters.ExecuteCommand("Reset");

            if (MAcquisitionManager != null)
            {
                if (MAcquisitionManager.State != PvAcquisitionState.Locked)
                {
                    MAcquisitionManager.Start();
                }
            }
        }

        public void StopAcquisition()
        {
            if (MAcquisitionManager != null)
            {
                if (MAcquisitionManager.State == PvAcquisitionState.Locked)
                {
                    MAcquisitionManager.Stop();
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
                        Console.WriteLine("灰度图，不转换");
                    }
                }
                else
                {
                    if (pvBuffer.Image.PixelType != PvPixelType.RGB8Packed)
                    {
                        if (mBufferConverter.IsConversionSupported(pvBuffer.Image.PixelType, PvPixelType.RGB8Packed))
                        {
                            try
                            {
                                mBufferConverter.Convert(pvBuffer, mConvertBuffer, true);
                                Console.WriteLine("进行了转换");
                                Console.WriteLine(mConvertBuffer.Image.Width);
                                Console.WriteLine(mConvertBuffer.Image.Height);
                                Console.WriteLine(mConvertBuffer.Image.PixelType);
                            }
                            catch (PvException ex)
                            {
                                Console.WriteLine(ex.Message);
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
        Console.WriteLine(ex.Message);
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
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public void AcquisitonThread()
        {
            PvBuffer lBuffer = null;

            for (; ; )
            {
                if (mAcquisitionTokenSource.IsCancellationRequested)
                {
                    return;
                }

                // Retrieve next buffer from acquisition pipeline
                PvResult lResult = mPipeline.RetrieveNextBuffer(ref lBuffer, 500);
                if (lResult.IsOK)
                {
                    // Operation result of buffer is OK, display
                    if (lBuffer.OperationResult.IsOK)
                    {
                        // ReaderWriterLock locker(this)
                        // {

                        // }

                        Console.WriteLine("output image");

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

        public bool IsConnected()
        {
            return mDevice.IsConnected;
        }
    }
}
