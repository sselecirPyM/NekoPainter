using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Util;

namespace NekoPainter.Nodes
{
    public class Graph
    {
        public Dictionary<int, Node> Nodes;
        [NonSerialized]
        public Dictionary<int, NodeParamCache> NodeParamCaches;

        public int outputNode;
        public int idAllocated;

        public void Initialize()
        {
            Nodes = new Dictionary<int, Node>();
            idAllocated = 1;
        }

        public LinkDesc Link(int output, string outputName, int input, string inputName)
        {
            return Link(Nodes[output], outputName, Nodes[input], inputName);
        }

        public LinkDesc Link(Node output, string outputName, Node input, string inputName)
        {
            if (output.Outputs == null)
                output.Outputs = new Dictionary<string, HashSet<NodeSocket>>();
            if (input.Inputs == null)
                input.Inputs = new Dictionary<string, NodeSocket>();
            output.Outputs.GetOrCreate(outputName).Add(new NodeSocket { targetUid = input.Luid, targetSocket = inputName });
            input.Inputs[inputName] = new NodeSocket { targetUid = output.Luid, targetSocket = outputName };
            return new LinkDesc { outputNode = output.Luid, outputSocket = outputName, inputNode = input.Luid, inputSocket = inputName };
        }

        public int AddNode(Node node)
        {
            node.Luid = idAllocated;
            idAllocated++;
            Nodes[node.Luid] = node;
            return idAllocated - 1;
        }

        public int AddNodeToEnd(Node node, Vector2 offset)
        {
            node.Luid = idAllocated;
            idAllocated++;
            Nodes[node.Luid] = node;

            if (Nodes.TryGetValue(outputNode, out var outputNode1))
                node.Position = outputNode1.Position;
            node.Position += offset;
            return idAllocated - 1;
        }

        public LinkDesc DisconnectLink(int inputNode, string inputSocketName)
        {
            var inputNode1 = Nodes[inputNode];
            var outputNode1 = Nodes[inputNode1.Inputs[inputSocketName].targetUid];
            return DisconnectLink(outputNode1.Luid, inputNode1.Inputs[inputSocketName].targetSocket, inputNode, inputSocketName);
        }

        public LinkDesc DisconnectLink(int outputNode, string outputSocketName, int inputNode, string inputSocketName)
        {
            var inputNode1 = Nodes[inputNode];
            var outputNode1 = Nodes[outputNode];
            inputNode1.Inputs.Remove(inputSocketName);
            outputNode1.Outputs[outputSocketName].RemoveWhere(u => u.targetSocket == inputSocketName && u.targetUid == inputNode);
            return new LinkDesc { inputNode = inputNode, inputSocket = inputSocketName, outputNode = outputNode, outputSocket = outputSocketName };
        }

        public void RemoveNodes(List<int> nodes)
        {
            List<Node> nodes1 = new List<Node>();
            foreach (var nodeId in nodes)
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

                        targetOutput.RemoveWhere(v => v.targetUid == node.Luid && !nodes.Any(u => u.Luid == targetNode.Luid));
                    }
                if (node.Outputs != null)
                    foreach (var links in node.Outputs)
                    {
                        foreach (var link in links.Value)
                        {
                            if (this.Nodes.TryGetValue(link.targetUid, out var targetNode) && !nodes.Any(u => u.Luid == targetNode.Luid))
                                targetNode.Inputs.Remove(link.targetSocket);
                        }
                    }
            }
            foreach (var node in nodes)
            {
                this.Nodes.Remove(node.Luid);
            }
        }

        public bool DependCheck()
        {
            //var inputChain = GetInputChainSet(nodeId);
            //HashSet<int> executed = new HashSet<int>();
            //List<int> executeOrder = new List<int>();
            //executeOrder.Capacity = inputChain.Count + 1;
            //int c = 0;
            //while (inputChain.Count > 0)
            //{
            //    foreach (int nodeId2 in inputChain)
            //    {
            //        var node = Nodes[nodeId2];
            //        if (node.Inputs == null || node.Inputs.All(u => executed.Contains(u.Value.targetUid)))
            //        {
            //            executed.Add(nodeId2);
            //            executeOrder.Add(nodeId2);
            //            continue;
            //        }
            //    }
            //    if (c == executeOrder.Count && inputChain.Count > 0) return false;
            //    for (; c < executeOrder.Count; c++)
            //    {
            //        inputChain.Remove(executeOrder[c]);
            //    }
            //}
            //return true;

            Dictionary<int, int> inDegrees = new Dictionary<int, int>();
            Stack<int> executeStack = new Stack<int>();
            foreach (var node in Nodes)
            {
                int inDegree = node.Value.Inputs == null ? 0 : node.Value.Inputs.Count;
                inDegrees[node.Value.Luid] = inDegree;
                if (inDegree == 0)
                {
                    executeStack.Push(node.Value.Luid);
                }
            }
            int num = 0;
            while (executeStack.Count > 0)
            {
                int nodeId1 = executeStack.Pop();
                var node = Nodes[nodeId1];
                if (node.Outputs != null)
                {
                    foreach (var output in node.Outputs)
                    {
                        foreach (var output1 in output.Value)
                        {
                            var linkedNodeId = output1.targetUid;
                            inDegrees[linkedNodeId]--;
                            if (inDegrees[linkedNodeId] == 0)
                            {
                                executeStack.Push(linkedNodeId);
                            }
                        }
                    }
                }
                num++;
            }
            if (num == Nodes.Count)
                return true;
            else
                return false;
        }

        public List<int> GetExecuteList(int nodeId)
        {
            var inputChain = GetInputChainSet(nodeId);
            HashSet<int> executed = new HashSet<int>();
            List<int> executeOrder = new List<int>();
            executeOrder.Capacity = inputChain.Count + 1;
            int c = 0;
            while (inputChain.Count > 0)
            {
                foreach (int nodeId2 in inputChain)
                {
                    var node = Nodes[nodeId2];
                    if (node.Inputs == null || node.Inputs.All(u => executed.Contains(u.Value.targetUid)))
                    {
                        executed.Add(nodeId2);
                        executeOrder.Add(nodeId2);
                        continue;
                    }
                }
                for (; c < executeOrder.Count; c++)
                {
                    inputChain.Remove(executeOrder[c]);
                }
            }
            if (Nodes.ContainsKey(nodeId))
                executeOrder.Add(nodeId);
            return executeOrder;
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
            foreach (var pair in Nodes)
            {
                clone.Nodes.Add(pair.Key, pair.Value.Clone());
            }

            return clone;
        }
    }
}
