using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Nodes;
using NekoPainter.Util;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_Remove_RecoverNodes : IUndoCommand
    {
        public List<int> removeNodes;
        public List<Node> recoverNodes;
        //public List<NodeSocket> recoverLinksInput;
        //public List<NodeSocket> recoverLinksOutput;
        public int setOutputNode;
        public Graph graph;

        public void Dispose()
        {

        }

        public IUndoCommand Execute()
        {
            CMD_Remove_RecoverNodes newCmd = new CMD_Remove_RecoverNodes();
            newCmd.graph = graph;
            newCmd.setOutputNode = graph.outputNode;
            if (removeNodes != null)
            {
                //foreach (var nodeId in removeNodes)
                //{
                //    var selectedNode = graph.Nodes[nodeId];

                //    var targetNode = Nodes[link.Value.targetUid];
                //    var targetOutput = targetNode.Outputs[link.Value.targetSocket];

                //}
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
                                var targetNode = graph.Nodes[output1.targetUid];
                                targetNode.Inputs[output1.targetSocket] = new NodeSocket { targetSocket = output.Key, targetUid = node.Luid };
                            }
                        }
                }
            }
            //if (recoverLinksInput != null)
            //{
            //    for (int i = 0; i < recoverLinksInput.Count; i++)
            //    {
            //        graph.Link(recoverLinksInput[i].targetUid, recoverLinksInput[i].targetSocket, recoverLinksInput[i].targetUid, recoverLinksInput[i].targetSocket);
            //    }
            //}
            graph.outputNode = setOutputNode;
            return newCmd;
        }
    }
}
