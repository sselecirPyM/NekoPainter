using System.Collections;
using System.Collections.Generic;
using DirectCanvas.Layout;

namespace DirectCanvas.Undo
{
    public class CMD_TileReplace : IUndoCommand
    {
        public readonly StandardLayout Host;

        public readonly TiledTexture Data;

        public CanvasCase canvasCase;

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
            Host.ReplaceTiles(Data, canvasCase.PaintingTextureTemp, canvasCase.PaintingTexture, out TiledTexture before);
            canvasCase.PaintingTexture.CopyTo(canvasCase.PaintingTextureBackup);
            CMD_TileReplace undo = new CMD_TileReplace(Host, before, canvasCase);
            Host.saved = false;
            return undo;
        }

        public CMD_TileReplace(StandardLayout host, TiledTexture undoData, CanvasCase canvasCase)
        {
            Host = host;
            Data = undoData;
            this.canvasCase = canvasCase;
        }
    }
}
