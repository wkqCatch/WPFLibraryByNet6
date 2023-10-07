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

        public bool IsFullPart { get; set; } = false;

        public HImage? CurrentImage { get; set; } = new HImage();

        public DrawingObjectTypeList DrawingTypeList { get; set; }

        public CameraShowControl()
        {
            HMouseDown += HMouseDownEvent;
            HMouseDoubleClick += HMouseDoubleClickEvent;
            HMouseUp += HMouseUpEvent;

            DrawingTypeList = new DrawingObjectTypeList(this);

            HKeepAspectRatio = true;
        }

        private void HMouseDoubleClickEvent(object sender, HMouseEventArgsWPF e)
        {
            foreach (DrawingObjectType drawingObjectType in DrawingTypeList.DrawingObjectTypeLists)
            {
                foreach (HDrawingObject hDrawingObject in drawingObjectType.DrawingObjectList)
                {
                    HRegion drawingRegion = new(hDrawingObject.GetDrawingObjectIconic());
                    if(drawingRegion.TestRegionPoint(e.Row, e.Column) == 1)
                    {
                        Trace.WriteLine(hDrawingObject.ID);
                        return;
                    }
                }
            }
        }

        private void HMouseDownEvent(object sender, HMouseEventArgsWPF e)
        {
            
        }

        private void HMouseUpEvent(object sender, HMouseEventArgsWPF e)
        {
            foreach (DrawingObjectType drawingObjectType in DrawingTypeList.DrawingObjectTypeLists)
            {

            }
        }

        public void AttachCameraDevice(CameraDevice? cameraDevice)
        {
            if (cameraDevice != null && CameraDeviceAttached == null)
            {
                CameraDeviceAttached = cameraDevice;
                CameraDeviceAttached.AcquisitionEvent += OnImageUpdated;
                IsFullPart = true;
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

            // HalconWindow.DispObj(CameraDeviceAttached?.AcquisitionImage);
            // if (IsFullPart)
            // {
            // IsFullPart = false;

            // Dispatcher.Invoke(new Action(() =>
            // {
            // SetFullImagePart();
            // }));
            // }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                HalconWindow.DispObj(CameraDeviceAttached?.AcquisitionImage);

                if (IsFullPart)
                {
                    IsFullPart = false;
                    SetFullImagePart();
                }
            })).Wait();
            
            
            Trace.WriteLine("显示采集图片");
        }
    }
}
