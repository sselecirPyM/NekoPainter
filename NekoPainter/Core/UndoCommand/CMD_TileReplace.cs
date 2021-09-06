using System.Collections;
using System.Collections.Generic;
using NekoPainter.Core;

namespace NekoPainter.Undo
{
    public class CMD_TileReplace : IUndoCommand
    {
        public readonly PictureLayout Host;

        public readonly TiledTexture Data;

        public LivedNekoPainterDocument document;

        public string Name;

        public override string ToString()
        {
            return Name;
        }

        public void Dispose()
        {
            Data.Dispose();
        }

        IUndoCommand IUndoCommand.Execute()
        {
            //Host.ReplaceTiles(Data, ref Host.tiledTexture, canvasCase.PaintingTextureTemp, canvasCase.PaintingTexture, out TiledTexture before);
            document.LayoutTex.TryGetValue(Host.guid, out TiledTexture tiledTexture);
            PictureLayout.ReplaceTiles1(Data, ref tiledTexture, document.PaintingTextureTemp, document.PaintingTexture, out TiledTexture before, document.PaintAgent.CurrentLayout == Host);
            document.LayoutTex[Host.guid] = tiledTexture;

            document.PaintingTexture.CopyTo(document.PaintingTextureBackup);
            CMD_TileReplace undo = new CMD_TileReplace(Host, before, document);
            Host.saved = false;
            return undo;
        }

        public CMD_TileReplace(PictureLayout host, TiledTexture undoData, LivedNekoPainterDocument document)
        {
            Host = host;
            Data = undoData;
            this.document = document;
        }
    }
}
