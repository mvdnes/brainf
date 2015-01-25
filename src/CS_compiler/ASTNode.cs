using System;

namespace BrainfCompiler
{
    enum ASTNodeType
    {
        Plus,
        Right,
        Loop,
        Read,
        Write,
        Leaf,
        SetZero,
    };

    class ASTNode
    {
        public ASTNodeType nodeType { get; private set; }
        public ASTNode childLeft { get; set; }
        public ASTNode childRight { get; set; }
        public int amount { get; private set; }

        public static ASTNode getLeaf()
        {
            return new ASTNode(ASTNodeType.Leaf, null, null, 1);
        }

        public static ASTNode getLoop(ASTNode left, ASTNode right)
        {
            return new ASTNode(ASTNodeType.Loop, left, right, 1);
        }

        public static ASTNode getUnary(ASTNodeType type, ASTNode child, int amount)
        {
            switch (type)
            {
                case ASTNodeType.Plus:
                case ASTNodeType.Read:
                case ASTNodeType.Right:
                case ASTNodeType.SetZero:
                case ASTNodeType.Write:
                    break;
                default:
                    throw new ArgumentException("Invalid node type");
            }

            return new ASTNode(type, child, null, amount);
        }

        private ASTNode(ASTNodeType type, ASTNode childLeft, ASTNode childRight, int amount)
        {
            this.nodeType = type;
            this.childLeft = childLeft;
            this.childRight = childRight;
            this.amount = amount;
        }

        public String asString()
        {
            return getRepresentation(0);
        }

        private String getRepresentation(int indentation)
        {
            String result = "";
            for (int i = 0; i < indentation; ++i) result += " ";
            
            result += String.Format("Node {0}: ({1})\n", this.nodeType, this.amount);
            if (this.childLeft != null)
            {
                result += this.childLeft.getRepresentation(indentation + 1);
            }
            if (this.childRight != null)
            {
                result += this.childRight.getRepresentation(indentation + 1);
            }
            return result;
        }
    }
}
