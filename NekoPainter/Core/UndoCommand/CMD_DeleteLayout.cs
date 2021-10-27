using System.Collections;
using System.Collections.Generic;
using NekoPainter.FileFormat;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_DeleteLayout : IUndoCommand
    {
        readonly public PictureLayout layout;
        readonly LivedNekoPainterDocument document;
        readonly NekoPainterDocument document1;
        readonly int atIndex;

        public CMD_DeleteLayout(PictureLayout layout, LivedNekoPainterDocument case1, NekoPainterDocument document1, int atIndex)
        {
            this.layout = layout;
            document = case1;
            this.atIndex = atIndex;
            this.document1 = document1;
        }
        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            if (document.ActivatedLayout == document.Layouts[atIndex])
            {
                document1.SetActivatedLayout(-1);
            }
            document.Layouts.RemoveAt(atIndex);
            return new CMD_RecoverLayout(layout, document,document1, atIndex);
        }
    }
}