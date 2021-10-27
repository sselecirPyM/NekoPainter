using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core.Nodes;
using NekoPainter.Core.Util;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_Remove_RecoverNodes : IUndoCommand
    {
        public List<int> removeNodes;
        public List<Node> recoverNodes;
        public List<LinkDesc> disconnectLinks;
        public List<LinkDesc> connectLinks;
        public int setOutputNode;
        public Graph graph;
        public Guid layoutGuid;
        public LivedNekoPainterDocument document;

        public void BuildRemoveNodes(LivedNekoPainterDocument document, Graph graph, List<int> removeNodes, Guid layoutGuid)
        {
            setOutputNode = graph.outputNode;
            this.document = document;
            this.removeNodes = removeNodes;
            this.layoutGuid = layoutGuid;
            this.graph = graph;
        }

        public void Dispose()
        {

        }

        public IUndoCommand Execute()
        {
            CMD_Remove_RecoverNodes newCmd = new CMD_Remove_RecoverNodes();
            newCmd.graph = graph;
            newCmd.setOutputNode = graph.outputNode;
            newCmd.layoutGuid = layoutGuid;
            newCmd.document = document;
            if (removeNodes != null)
            {
                newCmd.recoverNodes = new List<Node>();
                for (int i = 0; i < removeNodes.Count; i++)
                {
                    newCmd.recoverNodes.Add(graph.Nodes[removeNodes[i]]);
                }
                graph.RemoveNodes(removeNodes);
            }
            if (recoverNodes != null)
            {
                newCmd.removeNodes = new List<int>();
                for (int i = 0; i < recoverNodes.Count; i++)
                {
                    newCmd.removeNodes.Add(recoverNodes[i].Luid);
                }
                foreach (var node in recoverNodes)
                    graph.Nodes[node.Luid] = node;
                foreach (var node in recoverNodes)
                {
                    if (node.Inputs != null)
                        foreach (var input in node.Inputs)
                        {
                            var targetNode = graph.Nodes[input.Value.targetUid];
                            targetNode.Outputs.GetOrCreate(input.Value.targetSocket).Add(new NodeSocket { targetSocket = input.Key, targetUid = node.Luid });
                        }
                    if (node.Outputs != null)
                        foreach (var output in node.Outputs)
                        {
                            foreach (var output1 in output.Value)
                            {
                                if (graph.Nodes.TryGetValue(output1.targetUid, out var targetNode))
                                    targetNode.Inputs[output1.targetSocket] = new NodeSocket { targetSocket = output.Key, targetUid = node.Luid };
                            }
                        }
                }
            }
            if (disconnectLinks != null)
            {
                newCmd.connectLinks = new List<LinkDesc>();
                foreach (var connection in disconnectLinks)
                {
                    graph.DisconnectLink(connection.outputNode, connection.outputName, connection.inputNode, connection.inputName);
                    newCmd.connectLinks.Add(connection);
                }
            }
            if (connectLinks != null)
            {
                newCmd.disconnectLinks = new List<LinkDesc>();
                foreach (var connection in connectLinks)
                {
                    graph.Link(connection.outputNode, connection.outputName, connection.inputNode, connection.inputName);
                    newCmd.disconnectLinks.Add(connection);
                }
            }
            if (layoutGuid != Guid.Empty)
            {
                var layout = document.Layouts.Find(u => u.guid == layoutGuid);
                layout.generateCache = true;
            }
            graph.outputNode = setOutputNode;
            return newCmd;
        }
    }
}
