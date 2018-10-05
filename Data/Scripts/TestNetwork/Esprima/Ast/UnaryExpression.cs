using System;
using System.Runtime.Serialization;

namespace Esprima.Ast
{
    public enum UnaryOperator
    {
        Plus,
        Minus,
        BitwiseNot,
        LogicalNot,
        Delete,
        Void,
        TypeOf,
        Increment,
        Decrement,
    }

    public class UnaryExpression : Node,
        Expression
    {
        public readonly UnaryOperator Operator;
        public readonly Expression Argument;
        public bool Prefix { get; protected set; }

        public static UnaryOperator ParseUnaryOperator(string op)
        {
            switch (op)
            {
                case "+":
                    return UnaryOperator.Plus;
                case "-":
                    return UnaryOperator.Minus;
                case "++":
                    return UnaryOperator.Increment;
                case "--":
                    return UnaryOperator.Decrement;
                case "~":
                    return UnaryOperator.BitwiseNot;
                case "!":
                    return UnaryOperator.LogicalNot;
                case "delete":
                    return UnaryOperator.Delete;
                case "void":
                    return UnaryOperator.Void;
                case "typeof":
                    return UnaryOperator.TypeOf;

                default:
                    throw new Exception("Invalid unary operator: " + op);

            }


        }
        public UnaryExpression(string op, Expression arg)
        {
            Type = Nodes.UnaryExpression;
            Operator = ParseUnaryOperator(op);
            Argument = arg;
            Prefix = true;
        }
    }
}