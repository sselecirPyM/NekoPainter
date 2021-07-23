using System.Collections;
using System.Collections.Generic;
using DirectCanvas.Layout;
namespace DirectCanvas.Undo
{
    public class CMD_DeleteLayout : IUndoCommand
    {
        readonly public PictureLayout layout;
        readonly CanvasCase canvasCase;
        readonly int atIndex;

        public CMD_DeleteLayout(PictureLayout layout, CanvasCase case1, int atIndex)
        {
            this.layout = layout;
            canvasCase = case1;
            this.atIndex = atIndex;
        }
        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            canvasCase.watched = false;
            if (canvasCase.ActivatedLayout == canvasCase.Layouts[atIndex])
            {
                canvasCase.SetActivatedLayout(-1);
            }
            canvasCase.Layouts.RemoveAt(atIndex);
            canvasCase.watched = true;
            return new CMD_RecoverLayout(layout, canvasCase, atIndex);
        }
    }
}