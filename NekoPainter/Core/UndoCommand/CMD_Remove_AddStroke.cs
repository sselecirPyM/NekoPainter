using NekoPainter.Core.Nodes;
using NekoPainter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_Remove_AddStroke : IUndoCommand
    {
        public Graph graph;
        public Node node;
        public Stroke stroke;
        public int removeIndex = -1;
        public CMD_Remove_AddStroke ()
        {

        }
        public CMD_Remove_AddStroke(Graph graph, Node node, Stroke stroke, int removeIndex)
        {
            this.graph = graph;
            this.node = node;
            this.stroke = stroke;
            this.removeIndex = removeIndex;
        }

        public IUndoCommand Execute()
        {
            Stroke stroke1 = null;
            int removeIndex1 = -1;
            if (removeIndex >= 0)
            {
                stroke1 = node.strokeNode.strokes[removeIndex];
                node.strokeNode.strokes.RemoveAt(removeIndex);
            }
            if (stroke != null)
            {
                removeIndex1 = node.strokeNode.strokes.Count;
                node.strokeNode.strokes.Add(stroke);
            }
            graph.SetNodeCacheInvalid(node.Luid);
            return new CMD_Remove_AddStroke() { graph = graph, node = node, stroke = stroke1, removeIndex = removeIndex1 };
        }

        public void Dispose()
        {

        }
    }
}
