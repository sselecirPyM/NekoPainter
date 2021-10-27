using NekoPainter.FileFormat;
using System.Collections;
using System.Collections.Generic;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_RecoverLayout : IUndoCommand, ICanDeleteCommand
    {
        public readonly PictureLayout layout;
        readonly int insertIndex;
        readonly LivedNekoPainterDocument document;
        readonly NekoPainterDocument document1;

        public CMD_RecoverLayout(PictureLayout layout, LivedNekoPainterDocument document, NekoPainterDocument document1, int insertIndex)
        {
            this.layout = layout;
            this.insertIndex = insertIndex;
            this.document = document;
            this.document1 = document1;
        }
        public void Delete()
        {
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
            return new CMD_DeleteLayout(layout, document, document1, insertIndex);
        }
    }
}