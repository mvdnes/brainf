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
            optimizer.go(ref tree);
            return tree;
        }

        public void go(ref ASTNode node)
        {
            if (node == null) {
                throw new ArgumentNullException("null's cannot be optimized");
            }
            ASTNode left = node.childLeft;
            ASTNode right = node.childRight;

            switch (node.nodeType)
            {
                case ASTNodeType.Leaf:
                    break;
                case ASTNodeType.Loop:
                    this.go(ref left);
                    this.go(ref right);

                    if (left.nodeType == ASTNodeType.Plus && left.childLeft.nodeType == ASTNodeType.Leaf) {
                        node = ASTNode.getUnary(ASTNodeType.SetZero, right, 1);
                    }
                    else {
                        node.childLeft = left;
                        node.childRight = right;
                    }

                    break;
                case ASTNodeType.Plus:
                case ASTNodeType.Read:
                case ASTNodeType.Right:
                case ASTNodeType.SetZero:
                case ASTNodeType.Write:
                    this.go(ref left);
                    node.childLeft = left;
                    break;
                default:
                    throw new NotImplementedException("Optimizer.go has a non exhaustive switch");
            }
        }
    }
}
