using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Util;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_MoveLayout : IUndoCommand
    {
        readonly LivedNekoPainterDocument document;
        readonly int indexBefore;
        readonly int indexAfter;

        public CMD_MoveLayout(LivedNekoPainterDocument document, int indexBefore, int indexAfter)
        {
            this.document = document;
            this.indexBefore = indexBefore;
            this.indexAfter = indexAfter;
        }

        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            document.Layouts.Move(indexBefore, indexAfter);
            return new CMD_MoveLayout(document, indexAfter, indexBefore);
        }
    }
}
