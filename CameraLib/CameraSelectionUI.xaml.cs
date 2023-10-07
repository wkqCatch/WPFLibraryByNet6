using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace CameraLib
{
    /// <summary>
    /// CameraSelectionUI.xaml 的交互逻辑
    /// </summary>
    public partial class CameraSelectionUI : Window
    {
        CameraManager? CamManager;

        public CameraSelectionUI(CameraManager? camManager)
        {
            InitializeComponent();

            CamManager = camManager;

            Title = "相机连接中";
            if (CamManager != null)
            {
                CamManager.ConnectCompleteEvent += CameraConnectCompleteEvent;
                CamManager.ConnectAllDeviceAsync();
            }
        }

        public CameraSelectionUI()
        {
            InitializeComponent();
        }

        void CameraConnectCompleteEvent(object? sender, EventArgs e)
        {
            Title = $"相机连接完成：{CamManager?.CameraList.Count}个";

            if (CamManager?.CameraList.Count > 0)
            {
                List<CameraShowControl> controls = new()
                {
                    SmartCamera0,
                    SmartCamera1,
                    SmartCamera2,
                    SmartCamera3,
                    SmartCamera4,
                    SmartCamera5,
                    SmartCamera6,
                    SmartCamera7
                };

                for (int i = 0; i < CamManager?.CameraList.Count; i++)
                {
                    controls[i].AttachCameraDevice(CamManager?.CameraList[i]);
                    CamManager?.CameraList[i].StartAcquisition();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CamManager?.CameraList.Count > 0)
            {
                List<CheckBox> checkBoxes = new()
                {
                    CamSelect0,
                    CamSelect1,
                    CamSelect2,
                    CamSelect3,
                    CamSelect4,
                    CamSelect5,
                    CamSelect6,
                    CamSelect7
                };

                List<CameraDevice> cameraDevices = new();
                for (int i = 0; i < CamManager?.CameraList.Count; i++)
                {
                    CamManager.CameraList[i].StopAcquisition();

                    if (checkBoxes[i].IsChecked is true)
                    {
                        cameraDevices.Add(CamManager.CameraList[i]);
                    }
                    else
                    {
                        CamManager.CameraList[i].Dispose();
                    }
                }

                if (CamManager is not null)
                {
                    CamManager.CameraList = cameraDevices;
                }
            }
        }
    }
}