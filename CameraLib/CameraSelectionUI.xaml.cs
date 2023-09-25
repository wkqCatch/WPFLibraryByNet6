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
        public CameraSelectionUI()
        {
            InitializeComponent();

            SmartImg.DrawingTypeList.AddDrawingObjectType(HDrawingObject.HDrawingObjectType.RECTANGLE1, 1, "red");
            SmartImg.DrawingTypeList.AddDrawingObjectType(HDrawingObject.HDrawingObjectType.CIRCLE, 1, "green");
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            SmartImg.HKeepAspectRatio = true;
            SmartImg.LoadImage();
            SmartImg.SetFullImagePart();

            SmartImg.DrawingTypeList.AddDrawingObject(0, 0, 0, 80, 80);
            SmartImg.DrawingTypeList.AddDrawingObject(0, 0, 0, 80, 80);

            //SmartImg.AddDrawingObject(1, 100, 80, 80);
            //SmartImg.AddDrawingObject(1, 100, 80, 80);
        }
    }
}