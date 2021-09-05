using System;
using System.Collections;
using System.Collections.Generic;

namespace NekoPainter
{
    /// <summary>
    /// 撤销数据的抽象接口
    /// </summary>
    public interface IUndoCommand:IDisposable
    {
        /// <summary>
        /// 撤销并返回撤销命令(撤销的撤销等于重做)
        /// </summary>
        IUndoCommand Execute();
    }
}
