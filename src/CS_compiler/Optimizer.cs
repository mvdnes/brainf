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
            var optimized_tree = optimizer.go(tree);
            return optimized_tree;
        }

        public ASTNode go(ASTNode node)
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
                    
                    var inner = this.go(loop_node.innerChild());
                    next = this.go(loop_node.nextChild());

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
                    next = this.go(node.nextChild());
                    node.setNext(next);
                    break;
                default:
                    throw new NotImplementedException("Optimizer.go has a non exhaustive switch");
            }

            return node;
        }
    }
}
