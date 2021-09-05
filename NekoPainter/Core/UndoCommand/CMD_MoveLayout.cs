using NekoPainter.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Undo
{
    public class CMD_MoveLayout : IUndoCommand
    {
        readonly CanvasCase canvasCase;
        readonly int indexBefore;
        readonly int indexAfter;

        public CMD_MoveLayout(CanvasCase canvasCase,int indexBefore,int indexAfter)
        {
            this.canvasCase = canvasCase;
            this.indexBefore = indexBefore;
            this.indexAfter = indexAfter;
        }

        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            //canvasCase.watched = false;
            canvasCase.Layouts.Move(indexBefore, indexAfter);
            //canvasCase.watched = true;
            return new CMD_MoveLayout(canvasCase, indexAfter, indexBefore);
        }
    }
}
