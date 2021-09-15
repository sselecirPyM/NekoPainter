using System;
using System.Collections;
using System.Collections.Generic;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_BlendModeChange : IUndoCommand
    {
        readonly PictureLayout Host;

        readonly Guid BlendMode;

        public CMD_BlendModeChange(PictureLayout host, Guid blendMode)
        {
            Host = host;
            BlendMode = blendMode;
        }

        public void Dispose()
        {
            return;
        }

        IUndoCommand IUndoCommand.Execute()
        {
            CMD_BlendModeChange undocmd = new CMD_BlendModeChange(Host, Host.BlendMode);
            Host.BlendMode = BlendMode;
            return undocmd;
        }
    }
}

