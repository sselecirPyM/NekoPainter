using System.Collections;
using System.Collections.Generic;
using NekoPainter.Core;

namespace NekoPainter.Undo
{
    public class CMD_DeleteLayout : IUndoCommand
    {
        readonly public PictureLayout layout;
        readonly LivedNekoPainterDocument document;
        readonly int atIndex;

        public CMD_DeleteLayout(PictureLayout layout, LivedNekoPainterDocument case1, int atIndex)
        {
            this.layout = layout;
            document = case1;
            this.atIndex = atIndex;
        }
        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            if (document.ActivatedLayout == document.Layouts[atIndex])
            {
                document.SetActivatedLayout(-1);
            }
            document.Layouts.RemoveAt(atIndex);
            return new CMD_RecoverLayout(layout, document, atIndex);
        }
    }
}