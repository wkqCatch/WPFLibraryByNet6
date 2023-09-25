using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraLib
{
    internal class DrawingObjectTypeList
    {
        public List<DrawingObjectType> DrawingObjectTypeLists { get; set; } = new();

        public CameraShowControl? ShowControl { get; set; }

        public DrawingObjectTypeList(CameraShowControl showControl) { ShowControl = showControl; }

        public void AddDrawingObjectType(
                    HDrawingObject.HDrawingObjectType type,
                    int lineWidth,
                    string lineColor)
        {
            DrawingObjectType drawingObjectType = DrawingObjectType.CreateDrawingType(
                                                                      type,
                                                                      lineWidth,
                                                                      lineColor,
                                                                      ShowControl);
            DrawingObjectTypeLists.Add(drawingObjectType);
            drawingObjectType.DrawingobjectCallbackEvent += DrawingObjectType_DrawingobjectCallbackEvent;
        }

        private void DrawingObjectType_DrawingobjectCallbackEvent(HDrawingObject drawid, HWindow window, string type)
        {
            throw new NotImplementedException();
        }

        public void AddDrawingObject(int nTypeIndex, params HTuple[] tuples)
        {
            if (nTypeIndex < 0 || nTypeIndex >= DrawingObjectTypeLists.Count)
            {
                return;
            }

            DrawingObjectTypeLists[nTypeIndex].AddDrawingObject(tuples);
        }

        public void HideAKindOfDrawingObjects(int nTypeIndex)
        {
            if (nTypeIndex<0 || nTypeIndex>=DrawingObjectTypeLists.Count)
            {
                return;
            }

            foreach (var drawingObject in DrawingObjectTypeLists[nTypeIndex].DrawingObjectList)
            {
                DrawingObjectTypeLists[nTypeIndex].ShowControl?.HalconWindow.DetachDrawingObjectFromWindow(drawingObject);
            }
        }

        public void HideAllDrawingObjects()
        {
            foreach (var drawingType in DrawingObjectTypeLists)
            {
                foreach (var drawingObject in drawingType.DrawingObjectList)
                {
                    drawingType.ShowControl?.HalconWindow.DetachDrawingObjectFromWindow(drawingObject);
                }
            }
        }
    }
}
