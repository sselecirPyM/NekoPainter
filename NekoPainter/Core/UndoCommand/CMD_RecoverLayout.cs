using System.Collections;
using System.Collections.Generic;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_RecoverLayout : IUndoCommand, ICanDeleteCommand
    {
        public readonly PictureLayout layout;
        readonly int insertIndex;
        readonly LivedNekoPainterDocument document;

        public CMD_RecoverLayout(PictureLayout layout, LivedNekoPainterDocument document, int insertIndex)
        {
            this.layout = layout;
            this.insertIndex = insertIndex;
            this.document = document;
        }
        public void Delete()
        {
            layout.Dispose();
            document.LayoutTex.Remove(layout.guid, out TiledTexture tiledTexture);
            tiledTexture?.Dispose();
        }

        public void Dispose()
        {
            return;
        }

        public IUndoCommand Execute()
        {
            document.Layouts.Insert(insertIndex, layout);
            return new CMD_DeleteLayout(layout, document, insertIndex);
        }
    }
}