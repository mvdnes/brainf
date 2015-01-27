using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfCompiler
{
    class Optimizer
    {
        public static ASTNode full(ASTNode tree)
        {
            tree = optimize_loops(tree);
            tree = optimize_offsets(tree, 0);
            return tree;
        }

        public static ASTNode optimize_loops(ASTNode node)
        {
            if (node == null) {
                throw new ArgumentNullException("null's cannot be optimized");
            }

            ASTNode next = null;

            switch (node.nodeType)
            {
                case ASTNodeType.Leaf:
                    break;
                case ASTNodeType.Loop:
                    var loop_node = node as ASTNodeLoop;
                    if (loop_node == null) throw new InvalidCastException("Node had nodeType loop but was another type.");

                    var inner = optimize_loops(loop_node.innerChild());
                    next = optimize_loops(loop_node.nextChild());

                    var copy_loop_data = analyseLoopInner(inner);
                    if (copy_loop_data != null)
                    {
                        node = new ASTNodeCopyLoop(next, copy_loop_data);
                    }
                    else {
                        loop_node.setInner(inner);
                        loop_node.setNext(next);
                        node = loop_node;
                    }

                    break;
                case ASTNodeType.Plus:
                case ASTNodeType.Read:
                case ASTNodeType.Right:
                case ASTNodeType.CopyLoop:
                case ASTNodeType.Write:
                    next = optimize_loops(node.nextChild());
                    node.setNext(next);
                    break;
                default:
                    throw new NotImplementedException("Optimizer.go has a non exhaustive switch");
            }

            return node;
        }

        private static Dictionary<int, int> analyseLoopInner(ASTNode node) {
            Dictionary<int, int> result = new Dictionary<int,int>();
            int offset = 0;

            while (node.nodeType != ASTNodeType.Leaf)
            {
                switch (node.nodeType)
                {
                    case ASTNodeType.Plus:
                        var node_plus = (ASTNodePlus)node;
                        var my_offset = offset + node_plus.offset;
                        if (result.ContainsKey(my_offset)) result[my_offset] += node_plus.amount;
                        else result.Add(my_offset, node_plus.amount);
                        break;
                    case ASTNodeType.Right:
                        var node_right = (ASTNodeRight)node;
                        offset += node_right.amount;
                        break;
                    default:
                        return null;
                }
                node = node.nextChild();
            }

            if (offset == 0 && result.ContainsKey(0) && result[0] == -1)
            {
                result.Remove(0);
                return result;
            }
            return null;
        }

        public static ASTNode optimize_offsets(ASTNode node)
        {
            return optimize_offsets(node, 0);
        }

        private static ASTNode optimize_offsets(ASTNode node, int offset)
        {
            if (node == null)
            {
                throw new ArgumentNullException("null's cannot be optimized");
            }

            ASTNode next = null;

            switch (node.nodeType)
            {
                case ASTNodeType.Plus:
                    var node_plus = (ASTNodePlus)node;
                    next = optimize_offsets(node.nextChild(), offset);
                    node_plus.setNext(next);
                    node_plus.offset = offset;
                    break;
                case ASTNodeType.Right:
                    var node_right = (ASTNodeRight)node;
                    node = optimize_offsets(node_right.nextChild(), offset + node_right.amount);
                    break;
                case ASTNodeType.Loop:
                    var node_loop = (ASTNodeLoop)node;
                    next = optimize_offsets(node_loop.nextChild(), 0);
                    var inner = optimize_offsets(node_loop.innerChild(), 0);

                    node_loop.setInner(inner);
                    node_loop.setNext(next);
                    node = node_loop;
                    node = reset_offset(node, offset);
                    break;
                case ASTNodeType.CopyLoop:
                case ASTNodeType.Read:
                case ASTNodeType.Write:
                    next = optimize_offsets(node.nextChild(), 0);
                    node.setNext(next);
                    node = reset_offset(node, offset);
                    break;
                case ASTNodeType.Leaf:
                    node = reset_offset(node, offset);
                    break;
                default:
                    throw new NotImplementedException("Optimizer.go has a non exhaustive switch");
            }

            return node;
        }

        private static ASTNode reset_offset(ASTNode node, int offset)
        {
            if (offset != 0)
            {
                node = new ASTNodeRight(node, offset);
            }
            return node;
        }
    }
}
