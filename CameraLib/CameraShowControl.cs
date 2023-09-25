using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;
using Microsoft.Win32;

namespace CameraLib
{
    internal class CameraShowControl : HSmartWindowControlWPF
    {
        public CameraDevice? CameraDeviceAttached { get; set; } = null;

        public HImage? CurrentImage { get; set; } = new HImage();

        public DrawingObjectTypeList DrawingTypeList { get; set; }

        public CameraShowControl()
        {
            HMouseDown += CameraShowControl_HMouseDown;
            HMouseDoubleClick += CameraShowControl_HMouseDoubleClick;
            HMouseUp += CameraShowControl_HMouseUp;

            DrawingTypeList = new DrawingObjectTypeList(this);
        }

        private void CameraShowControl_HMouseDoubleClick(object sender, HMouseEventArgsWPF e)
        {
            //if (sender is HDrawingObject)
            //{
            //    Trace.WriteLine($"是的");
            //}

            //Trace.WriteLine(sender.GetType().Name);
            //

            //Trace.WriteLine($"MouseDouble {e.Row} , {e.Column}");
        }

        private void CameraShowControl_HMouseDown(object sender, HMouseEventArgsWPF e)
        {
            
        }

        private void CameraShowControl_HMouseUp(object sender, HMouseEventArgsWPF e)
        {
            foreach (DrawingObjectType drawingObjectType in DrawingTypeList.DrawingObjectTypeLists)
            {

            }
        }

        public void AttachCameraDevice(CameraDevice cameraDevice)
        {
            if (cameraDevice != null && CameraDeviceAttached == null)
            {
                CameraDeviceAttached = cameraDevice;
                CameraDeviceAttached.AcquisitionEvent += OnImageUpdated;
            }
        }

        public void DetachCameraDevice()
        {
            if (CameraDeviceAttached != null)
            {
                CameraDeviceAttached.AcquisitionEvent -= OnImageUpdated;
                CameraDeviceAttached = null;
            }
        }

        public void LoadImage()
        {
            OpenFileDialog fileDialog = new()
            {
                Filter = "PNG图片|*.png|JPG图片|*.jpg|BITMAP图片|*.bitmap"
            };

            if (fileDialog.ShowDialog() == true)
            {
                CurrentImage?.ReadImage(fileDialog.FileName);
                HalconWindow.ClearWindow();
                HalconWindow.DispObj(CurrentImage);
            }
        }

        public void LoadImage(string strPath)
        {
            if (strPath is null)
            {
                CurrentImage?.ReadImage(strPath);
                HalconWindow.ClearWindow();
                HalconWindow.DispObj(CurrentImage);
            }
        }

        public void OnImageUpdated(object? sender, EventArgs eventArgs)
        {
            // HSmartWindow.HalconWindow.ClearWindow();
            HalconWindow.DispObj(CameraDeviceAttached?.AcquisitionImage);
        }
    }
}
