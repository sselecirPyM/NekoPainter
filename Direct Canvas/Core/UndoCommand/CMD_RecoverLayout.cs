using System.Collections;
using System.Collections.Generic;
using DirectCanvas.Layout;
namespace DirectCanvas.Undo
{
    public class CMD_RecoverLayout : IUndoCommand, ICanDeleteCommand
    {
        public readonly PictureLayout layout;
        readonly int insertIndex;
        readonly CanvasCase canvasCase;

        public CMD_RecoverLayout(PictureLayout layout, CanvasCase canvasCase, int insertIndex)
        {
            this.layout = layout;
            this.insertIndex = insertIndex;
            this.canvasCase = canvasCase;
        }
        public void Delete()
        {
            layout.Dispose();
        }

        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            canvasCase.watched = false;
            canvasCase.Layouts.Insert(insertIndex, layout);
            canvasCase.watched = true;
            return new CMD_DeleteLayout(layout, canvasCase, insertIndex);
        }
    }
}