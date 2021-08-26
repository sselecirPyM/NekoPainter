using System.Collections;
using System.Collections.Generic;
using DirectCanvas.Core;

namespace DirectCanvas.Undo
{
    public class CMD_TileReplace : IUndoCommand
    {
        public readonly PictureLayout Host;

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
            //Host.ReplaceTiles(Data, ref Host.tiledTexture, canvasCase.PaintingTextureTemp, canvasCase.PaintingTexture, out TiledTexture before);
            canvasCase.LayoutTex.TryGetValue(Host.guid, out TiledTexture tiledTexture);
            PictureLayout.ReplaceTiles1(Data, ref tiledTexture, canvasCase.PaintingTextureTemp, canvasCase.PaintingTexture, out TiledTexture before, canvasCase.PaintAgent.CurrentLayout == Host);
            canvasCase.LayoutTex[Host.guid] = tiledTexture;

            canvasCase.PaintingTexture.CopyTo(canvasCase.PaintingTextureBackup);
            CMD_TileReplace undo = new CMD_TileReplace(Host, before, canvasCase);
            Host.saved = false;
            return undo;
        }

        public CMD_TileReplace(PictureLayout host, TiledTexture undoData, CanvasCase canvasCase)
        {
            Host = host;
            Data = undoData;
            this.canvasCase = canvasCase;
        }
    }
}
