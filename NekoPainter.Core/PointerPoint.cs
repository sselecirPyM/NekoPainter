using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core
{
    public class PointerPoint
    {
        //
        // 摘要:
        //     获取输入框架的 ID。
        //
        // 返回结果:
        //     框架 ID.
        public uint FrameId { get; }
        //
        // 摘要:
        //     获取一个值，该值指示输入设备(触控、笔/触笔)是接触数字化器表面，还是鼠标按钮按下。
        //
        // 返回结果:
        //     如果按下，则为 true；否则为 false。
        public bool IsInContact { get; }
        //
        // 摘要:
        //     获取与输入指针关联的设备的相关信息。
        //
        // 返回结果:
        //     输入设备。
        //public PointerDevice PointerDevice { get; }
        //
        // 摘要:
        //     获取输入指针的唯一标识符。
        //
        // 返回结果:
        //     标识输入指针的唯一值。
        public uint PointerId { get; }
        //
        // 摘要:
        //     获取工作区坐标中指针输入的位置。
        //
        // 返回结果:
        //     客户端坐标（以设备独立像素 (DIP) 为单位）。
        public Point Position { get; }
        //
        // 摘要:
        //     获取有关输入指针的扩展信息。
        //
        // 返回结果:
        //     由输入设备公开的扩展属性。
        //public PointerPointProperties Properties { get; }
        //
        // 摘要:
        //     获取输入设备报告的输入指针工作区坐标。
        //
        // 返回结果:
        //     工作区坐标（以设备独立像素 (DIP) 为单位）。
        public Point RawPosition { get; }
        //
        // 摘要:
        //     获取发生输入的时间。
        //
        // 返回结果:
        //     相对于系统启动时间的时间（以微秒为单位）。
        public ulong Timestamp { get; }
    }
}
