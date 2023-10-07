using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using HalconDotNet;
using static HalconDotNet.HDrawingObject;

namespace CameraLib
{
    internal class DrawingObjectType
    {
        protected DrawingObjectType() { }

        public List<HDrawingObject> DrawingObjectList { get; set; } = new();

        public HDrawingObject.HDrawingObjectType ObjectTye { get; set; }

        public DrawingObjectType? CurrentSeletedObject { get; set; }

        public int LineWidth { get; set; }

        public string? LineColor { get; set; }

        public CameraShowControl? ShowControl { get; set; }

        public static DrawingObjectType CreateDrawingType(
                                        HDrawingObjectType type,
                                        int lineWidth,
                                        string lineColor,
                                        CameraShowControl? showControl)
        {
            return new DrawingObjectType()
            {
                ObjectTye = type,
                LineColor = lineColor,
                LineWidth = lineWidth,
                ShowControl = showControl
            };
        }

        public HDrawingObject AddDrawingObject(params HTuple[] hTuples)
        {
            HDrawingObject hDrawingObject = CreateDrawingObject(ObjectTye, hTuples);

            hDrawingObject.SetDrawingObjectParams("color", LineColor);
            hDrawingObject.SetDrawingObjectParams("line_width", LineWidth);

            DrawingObjectList.Add(hDrawingObject);
            ShowControl?.HalconWindow.AttachDrawingObjectToWindow(hDrawingObject);

            return hDrawingObject;
        }

        public bool RemoveDrawingObject(HDrawingObject hDrawingObject)
        {
            return DrawingObjectList.Remove(hDrawingObject);
        }

        public void ClearDrawingObjects()
        {
            DrawingObjectList.Clear();
        }
    }
}
