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
using DirectCanvas.Controller;
using Windows.Graphics.Display;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace DirectCanvas.UI
{
    public sealed partial class DCUI_Canvas : UserControl
    {
        public DCUI_Canvas()
        {
            this.InitializeComponent();
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
            AppController.Instance.CanvasRender();
        }

        private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppController.Instance == null) return;
            AppController.Instance.dcUI_Canvas = this;
            graphicsContext = AppController.Instance.graphicsContext;
            float dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            graphicsContext.SetSwapChainPanel(swapChainPanel, new Vector2(swapChainPanel.CompositionScaleX, swapChainPanel.CompositionScaleY), swapChainPanel.ActualSize, dpi);
            AppController.Instance.CanvasRender();

            swapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            graphicsContext.SetLogicSize(e.NewSize.ToVector2());
            AppController.Instance.CanvasRender();//必须在设置大小之后显示出来，否则会出错。
        }
        #endregion

        public void SetCanvasCase(CanvasCase canvasCase)
        {

            AppController.Instance.CanvasRender();
        }

        private void Canvas_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            AppController.Instance.mainPage.Focus(FocusState.Programmatic);
            Input.canvasInputStatus = CanvasInputStatus.None;
            args.Handled = true;
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
                Input.penInputData.Enqueue(new PenInputData { point = args.CurrentPoint.Position.ToVector2() * logicScale, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.Begin });
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
            AppController.Instance.CanvasRender();

        }

        private void Canvas_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            if (Input.canvasInputStatus == CanvasInputStatus.Paint)
            {
                Input.penInputData.Enqueue(new PenInputData { point = args.CurrentPoint.Position.ToVector2() * logicScale, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.Drawing });
            }
            Input.EnqueueMouseMove(logicScale * args.CurrentPoint.Position.ToVector2());
            AppController.Instance.CanvasRender();
        }

        private void Canvas_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            if (Input.canvasInputStatus == CanvasInputStatus.Paint)
            {
                Input.penInputData.Enqueue(new PenInputData { point = args.CurrentPoint.Position.ToVector2() * logicScale, pointerPoint = args.CurrentPoint, penInputFlag = PenInputFlag.End });
            }
            Vector2 mousePos = logicScale * args.CurrentPoint.Position.ToVector2();
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseMiddleDown);
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseLeftDown);
            Input.EnqueueMouseClick(mousePos, false, InputType.MouseRightDown);

            AppController.Instance.CanvasRender();
            Input.canvasInputStatus = CanvasInputStatus.None;
        }

        private void InkCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(sender as UIElement);

            Vector2 position = pointer.Position.ToVector2() * logicScale;

            Input.EnqueueMouseWheel(position, pointer.Properties.MouseWheelDelta);
            AppController.Instance.CanvasRender();
        }

        //private void ResetCanvasPosition()
        //{
        //    CanvasRect.Position = new Vector2(
        //        ((float)swapChainPanel.ActualWidth * logicScale - CanvasCase.Width) * 0.5f,
        //        ((float)swapChainPanel.ActualHeight * logicScale - CanvasCase.Height) * 0.5f);
        //    CanvasRect.Rotation = 0.0f;
        //    CanvasRect.Scale = 1.0f;
        //    AppController.Instance.CurrentCanvasCase.rotation = CanvasRect.Rotation;
        //    AppController.Instance.CurrentCanvasCase.position = CanvasRect.Position;
        //    AppController.Instance.CanvasRender();
        //}

        float logicScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

        GraphicsContext graphicsContext;
    }
}
