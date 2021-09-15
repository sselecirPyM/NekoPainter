using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Util;

namespace NekoPainter.Nodes
{
    public class Graph
    {
        public Dictionary<int, Node> Nodes;
        public int outputNode;
        public int idAllocated;

        public void Initialize()
        {
            Nodes = new Dictionary<int, Node>();
            idAllocated = 1;
        }

        public void Link(int output, string outputName, int input, string inputName)
        {
            Link(Nodes[output], outputName, Nodes[input], inputName);
        }

        public void Link(Node output, string outputName, Node input, string inputName)
        {
            if (output.Outputs == null)
                output.Outputs = new Dictionary<string, HashSet<NodeSocket>>();
            if (input.Inputs == null)
                input.Inputs = new Dictionary<string, NodeSocket>();
            output.Outputs.GetOrCreate(outputName).Add(new NodeSocket { targetUid = input.Luid, targetSocket = inputName });
            input.Inputs[inputName] = new NodeSocket { targetUid = output.Luid, targetSocket = outputName };
        }

        public int AddNode(Node node)
        {
            node.Luid = idAllocated;
            idAllocated++;
            Nodes[node.Luid] = node;
            return idAllocated - 1;
        }

        public void RemoveNodes(List<int> nodes)
        {
            List<Node> nodes1 = new List<Node>();
            foreach(var nodeId in nodes)
            {
                nodes1.Add(Nodes[nodeId]);
            }
            RemoveNodes(nodes1);
        }

        public void RemoveNodes(List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Inputs != null)
                    foreach (var link in node.Inputs)
                    {
                        var targetNode = Nodes[link.Value.targetUid];
                        var targetOutput = targetNode.Outputs[link.Value.targetSocket];

                        targetOutput.RemoveWhere(v => v.targetUid == targetNode.Luid && !nodes.Any(u => u.Luid == targetNode.Luid));
                    }
                if (node.Outputs != null)
                    foreach (var links in node.Outputs)
                    {
                        foreach (var link in links.Value)
                        {
                            var targetNode = Nodes[link.targetUid];
                            if (!nodes.Any(u => u.Luid == targetNode.Luid))
                                targetNode.Inputs.Remove(link.targetSocket);
                        }
                    }
            }
        }

        public HashSet<int> GetInputChainSet(int nodeId)
        {
            HashSet<int> inputSet = new HashSet<int>();
            Queue<int> bfsQueue = new Queue<int>();
            bfsQueue.Enqueue(nodeId);
            while (bfsQueue.Count > 0 && Nodes.TryGetValue(bfsQueue.Dequeue(), out Node node))
            {
                if (node.Inputs != null)
                {
                    foreach (var pair in node.Inputs)
                    {
                        if (!inputSet.Contains(pair.Value.targetUid))
                        {
                            bfsQueue.Enqueue(pair.Value.targetUid);
                            inputSet.Add(pair.Value.targetUid);
                        }
                    }
                }
            }
            return inputSet;
        }

        public Graph Clone()
        {
            Graph clone = (Graph)MemberwiseClone();
            clone.Nodes = new Dictionary<int, Node>();
            foreach (var pair in clone.Nodes)
            {
                clone.Nodes.Add(pair.Key, pair.Value.Clone());
            }

            return clone;
        }
    }
}
