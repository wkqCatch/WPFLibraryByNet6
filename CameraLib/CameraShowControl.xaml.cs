using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HalconDotNet;

namespace CameraLib
{
    /// <summary>
    /// CameraShowControl.xaml 的交互逻辑
    /// </summary>
    public partial class CameraShowControl : UserControl
    {
        public CameraDevice CameraDeviceAttached { get; set; } = null;

        public HImage CurrentImage { get; set; } = new HImage();

        public HWindow DispWindow
        {
            get { return HSmartWindow.HalconWindow; }
        }

        public CameraShowControl()
        {
            InitializeComponent();
        }

        public void AttachCameraDevice(CameraDevice cameraDevice)
        {
            if (cameraDevice != null && CameraDeviceAttached == null)
            {
                CameraDeviceAttached = cameraDevice;
                CameraDeviceAttached.AcquisitionEvent += OnImageUpdated;
            }
        }

        public void LoadImage(string strPath)
        {
            if (strPath is null)
            {

            }
        }

        public void LoadImage()
        {
            //using ()
            //{

            //}
        }

        public void OnImageUpdated(object sender, EventArgs eventArgs)
        {
            //HSmartWindow.HalconWindow.ClearWindow();
            HSmartWindow.HalconWindow.DispObj(CameraDeviceAttached.AcquisitionImage);
        }
    }
}
