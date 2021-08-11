using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using CanvasRendering;
using System.Numerics;
using DirectCanvas.UI.Controller;
using Windows.Graphics.Display;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace DirectCanvas.UI
{
    public sealed partial class DCUI_Canvas : UserControl
    {
        public DCUI_Canvas()
        {
            this.InitializeComponent();
            AppController.Instance.dcUI_Canvas = this;
            AppController.Instance.command_ResetCanvasPosition.executeAction += ResetCanvasPosition;
        }
        #region 杂项
        private void InkCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            InkCanvas inkCanvas = sender as InkCanvas;
            inkCanvas.InkPresenter.InputDeviceTypes =
                CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen;
            inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += Canvas_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += Canvas_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += Canvas_PointerReleased;
            inkCanvas.InkPresenter.UnprocessedInput.PointerHovered += UnprocessedInput_PointerHovered;
            inkCanvas.PointerWheelChanged += InkCanvas_PointerWheelChanged;
        }

        private void UnprocessedInput_PointerHovered(InkUnprocessedInput sender, PointerEventArgs args)
        {
            Input.EnqueueMouseMove(logicScale * args.CurrentPoint.Position.ToVector2());
            AppController.Instance.CanvasRerender();
        }

        private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            graphicsContext = AppController.Instance.graphicsContext;
            float dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            graphicsContext.SetSwapChainPanel(swapChainPanel, new Vector2(swapChainPanel.CompositionScaleX, swapChainPanel.CompositionScaleY), swapChainPanel.ActualSize, dpi);
            Size x = swapChainPanel.RenderSize;
            AppController.Instance.CanvasRerender();

            swapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            graphicsContext.SetLogicSize(e.NewSize.ToVector2());
            AppController.Instance.CanvasRerender();//必须在设置大小之后显示出来，否则会出错。
        }
        #endregion

        PaintAgent paintAgent;

        public void SetCanvasCase(CanvasCase canvasCase)
        {
            CanvasCase = canvasCase;
            CanvasRect = new CSRect();
            CanvasRect.vertexShader = AppController.Instance.vertexShaders["default2DVertexShader"];
            CanvasRect.pixelShader = AppController.Instance.pixelShaders["PS2DTex1"];
            CanvasRect.Position = new Vector2(
                ((float)swapChainPanel.ActualWidth * logicScale - CanvasCase.Width) * 0.5f,
                ((float)swapChainPanel.ActualHeight * logicScale - CanvasCase.Height) * 0.5f);
            CanvasRect.Initialize(CanvasCase.DeviceResources, CanvasCase.Width, CanvasCase.Height);

            paintAgent = CanvasCase.PaintAgent;
            AppController.Instance.CanvasRerender();
        }

        private void Canvas_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            AppController.Instance.mainPage.Focus(FocusState.Programmatic);
            Input.canvasInputStatus = CanvasInputStatus.None;
            args.Handled = true;
            if (CanvasCase != null)
            {
                if (args.CurrentPoint.Properties.IsMiddleButtonPressed || (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu) && args.CurrentPoint.Properties.IsLeftButtonPressed))
                {
                    Input.canvasInputStatus = CanvasInputStatus.Drag;
                }
                else if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu) && args.CurrentPoint.Properties.IsRightButtonPressed)
                {
                    Input.canvasInputStatus = CanvasInputStatus.DragRotate;
                }
                else
                {
                    Input.canvasInputStatus = CanvasInputStatus.Paint;
                    Vector2 transformedVector = Vector2.Transform(args.CurrentPoint.Position.ToVector2() * logicScale, UpdateTransformMatriex());
                    Input.penInputData.Enqueue(new PenInputData { point = transformedVector, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.Begin });

                    //paintAgent.DrawBegin(transformedVector, args.CurrentPoint);
                    //paintAgent.Process();
                    //CanvasCase.ViewRenderer.RenderAll();
                }
            }
            if (args.CurrentPoint.Properties.IsMiddleButtonPressed)
            {
                Input.EnqueueMouseClick(logicScale * args.CurrentPoint.Position.ToVector2(), true, InputType.MouseMiddleDown);
            }
            if (args.CurrentPoint.Properties.IsLeftButtonPressed)
            {
                Input.EnqueueMouseClick(logicScale * args.CurrentPoint.Position.ToVector2(), true, InputType.MouseLeftDown);
            }
            if (args.CurrentPoint.Properties.IsRightButtonPressed)
            {
                Input.EnqueueMouseClick(logicScale * args.CurrentPoint.Position.ToVector2(), true, InputType.MouseRightDown);
            }
            AppController.Instance.CanvasRerender();

        }

        private void Canvas_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            if (CanvasCase != null)
            {
                if (Input.canvasInputStatus == CanvasInputStatus.Paint)
                {
                    Vector2 transformedVector = Vector2.Transform(args.CurrentPoint.Position.ToVector2() * logicScale, UpdateTransformMatriex());
                    Input.penInputData.Enqueue(new PenInputData { point = transformedVector, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.Drawing });
                    //paintAgent.Draw(transformedVector, args.CurrentPoint);
                    //paintAgent.Process();
                    //CanvasCase.ViewRenderer.RenderAll();
                }
            }
            Input.EnqueueMouseMove(logicScale * args.CurrentPoint.Position.ToVector2());
            AppController.Instance.CanvasRerender();
        }

        private void Canvas_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            if (CanvasCase != null)
            {
                if (Input.canvasInputStatus == CanvasInputStatus.Paint)
                {
                    Vector2 transformedVector = Vector2.Transform(args.CurrentPoint.Position.ToVector2() * logicScale, UpdateTransformMatriex());
                    Input.penInputData.Enqueue(new PenInputData { point = transformedVector, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.End });
                    //paintAgent.DrawEnd(transformedVector, args.CurrentPoint);
                    //paintAgent.Process();
                    //CanvasCase.ViewRenderer.RenderAll();
                }
            }
            Vector2 mousePos = logicScale * args.CurrentPoint.Position.ToVector2();
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseMiddleDown);
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseLeftDown);
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseRightDown);

            AppController.Instance.CanvasRerender();
            Input.canvasInputStatus = CanvasInputStatus.None;
        }

        public void WheelScale(Vector2 point, float wheelDelta)
        {
            if (CanvasCase == null)
                return;
            float canvasScaleBefore = CanvasRect.Scale;
            CanvasRect.Scale *= (float)Math.Pow(1.0014450997779993488675056142818, wheelDelta);

            Matrix4x4 moveMatrixMid = Matrix4x4.CreateTranslation(new Vector3(-point, 0)) *
                Matrix4x4.CreateScale(Vector3.One / canvasScaleBefore * CanvasRect.Scale) * Matrix4x4.CreateTranslation(new Vector3(point, 0));

            CanvasRect.Position = Vector2.Transform(CanvasRect.Position, moveMatrixMid);
        }

        private void InkCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(sender as UIElement);

            Vector2 position = pointer.Position.ToVector2() * logicScale;

            Input.EnqueueMouseWheel(position, pointer.Properties.MouseWheelDelta);
            AppController.Instance.CanvasRerender();
        }

        public void RenderContent()
        {
            if (CanvasRect != null)
            {
                CanvasRect.SetRefTexture(CanvasCase.RenderTarget[0], 0);
                CanvasRect.Render();
            }
        }

        public Matrix4x4 UpdateTransformMatriex()
        {
            if (CanvasRect == null) return Matrix4x4.Identity;
            Matrix4x4 toCanvasPos = Matrix4x4.CreateTranslation(new Vector3(-CanvasRect.Position, 0));
            toCanvasPos = Matrix4x4.Multiply(toCanvasPos, Matrix4x4.CreateRotationZ(CanvasRect.Rotation));
            toCanvasPos = Matrix4x4.Multiply(toCanvasPos, Matrix4x4.CreateScale(1.0f / CanvasRect.Scale));
            return toCanvasPos;

        }

        public void MoveProcess(Vector2 p0, Vector2 p1)
        {
            CanvasRect.Position += p0 - p1;
        }

        public void RotateProcess(Vector2 p0, Vector2 p1)
        {
            Vector2 halfRenderSize = RenderSize.ToVector2() * logicScale * 0.5f;
            Vector2 pos0 = p0 - halfRenderSize;
            Vector2 pos1 = p1 - halfRenderSize;
            float rotateR = MathF.Atan2(pos0.X, pos0.Y) - MathF.Atan2(pos1.X, pos1.Y);
            CanvasRect.Rotation = (CanvasRect.Rotation + rotateR) % (MathF.PI * 2);
            Matrix4x4 rotateMatrixMid = Matrix4x4.CreateTranslation(new Vector3(-halfRenderSize, 0)) *
                Matrix4x4.CreateRotationZ(-rotateR) * Matrix4x4.CreateTranslation(new Vector3(halfRenderSize, 0));

            CanvasRect.Position = Vector2.Transform(CanvasRect.Position, rotateMatrixMid);
        }

        private void ResetCanvasPosition()
        {
            CanvasRect.Position = new Vector2(
                ((float)swapChainPanel.ActualWidth * logicScale - CanvasCase.Width) * 0.5f,
                ((float)swapChainPanel.ActualHeight * logicScale - CanvasCase.Height) * 0.5f);
            CanvasRect.Rotation = 0.0f;
            CanvasRect.Scale = 1.0f;
            AppController.Instance.CanvasRerender();
        }

        float logicScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

        public CanvasCase CanvasCase { get; private set; }

        GraphicsContext graphicsContext;
        CSRect CanvasRect;
    }
}
