using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfCompiler
{
    class Optimizer
    {
        private ASTNode root;

        private Optimizer(ASTNode tree)
        {
            this.root = tree;
        }

        public static ASTNode run(ASTNode tree)
        {
            Optimizer optimizer = new Optimizer(tree);
            var optimized_tree = optimizer.optimize_loop(tree);
            optimized_tree = optimizer.optimize_offset(tree, 0);
            return optimized_tree;
        }

        public ASTNode optimize_loop(ASTNode node)
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

                    var inner = this.optimize_loop(loop_node.innerChild());
                    next = this.optimize_loop(loop_node.nextChild());

                    if (inner.nodeType == ASTNodeType.Plus && inner.nextChild().nodeType == ASTNodeType.Leaf) {
                        node = new ASTNodeUnary(ASTNodeType.SetZero, next);
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
                case ASTNodeType.SetZero:
                case ASTNodeType.Write:
                    next = this.optimize_loop(node.nextChild());
                    node.setNext(next);
                    break;
                default:
                    throw new NotImplementedException("Optimizer.go has a non exhaustive switch");
            }

            return node;
        }

        public ASTNode optimize_offset(ASTNode node, int offset)
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
                    next = this.optimize_offset(node.nextChild(), offset);
                    node_plus.setNext(next);
                    node_plus.offset = offset;
                    break;
                case ASTNodeType.Right:
                    var node_right = (ASTNodeRight)node;
                    node = this.optimize_offset(node_right.nextChild(), offset + node_right.amount);
                    break;
                case ASTNodeType.Loop:
                    var node_loop = (ASTNodeLoop)node;
                    next = this.optimize_offset(node_loop.nextChild(), 0);
                    var inner = this.optimize_offset(node_loop.innerChild(), 0);

                    node_loop.setInner(inner);
                    node_loop.setNext(next);
                    node = node_loop;
                    node = reset_offset(node, offset);
                    break;
                case ASTNodeType.Read:
                case ASTNodeType.SetZero:
                case ASTNodeType.Write:
                    next = this.optimize_offset(node.nextChild(), 0);
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

        private ASTNode reset_offset(ASTNode node, int offset)
        {
            if (offset != 0)
            {
                node = new ASTNodeRight(node, offset);
            }
            return node;
        }
    }
}
