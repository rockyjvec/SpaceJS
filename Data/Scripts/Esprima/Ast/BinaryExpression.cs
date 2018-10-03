using System;
using System.Runtime.Serialization;

namespace Esprima.Ast
{
    public enum BinaryOperator
    {
        Plus,
        Minus,
        Times,
        Divide,
        Modulo,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        StrictlyEqual,
        StricltyNotEqual,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXOr,
        LeftShift,
        RightShift,
        UnsignedRightShift,
        InstanceOf,
        In,
        LogicalAnd,
        LogicalOr,
    }

    public class BinaryExpression : Node,
        Expression
    {
        public readonly BinaryOperator Operator;
        public readonly Expression Left;
        public readonly Expression Right;

        public BinaryExpression(string op, Expression left, Expression right)
        {
            Operator = ParseBinaryOperator(op);
            var logical = Operator == BinaryOperator.LogicalAnd || Operator == BinaryOperator.LogicalOr;
            Type = logical ? Nodes.LogicalExpression : Nodes.BinaryExpression;
            Left = left;
            Right = right;
        }

        public static BinaryOperator ParseBinaryOperator(string op)
        {
            switch (op)
            {
                case "+":
                    return BinaryOperator.Plus;
                case "-":
                    return BinaryOperator.Minus;
                case "*":
                    return BinaryOperator.Times;
                case "/":
                    return BinaryOperator.Divide;
                case "%":
                    return BinaryOperator.Modulo;
                case "==":
                    return BinaryOperator.Equal;
                case "!=":
                    return BinaryOperator.NotEqual;
                case ">":
                    return BinaryOperator.Greater;
                case ">=":
                    return BinaryOperator.GreaterOrEqual;
                case "<":
                    return BinaryOperator.Less;
                case "<=":
                    return BinaryOperator.LessOrEqual;
                case "===":
                    return BinaryOperator.StrictlyEqual;
                case "!==":
                    return BinaryOperator.StricltyNotEqual;
                case "&":
                    return BinaryOperator.BitwiseAnd;
                case "|":
                    return BinaryOperator.BitwiseOr;
                case "^":
                    return BinaryOperator.BitwiseXOr;
                case "<<":
                    return BinaryOperator.LeftShift;
                case ">>":
                    return BinaryOperator.RightShift;
                case ">>>":
                    return BinaryOperator.UnsignedRightShift;
                case "instanceof":
                    return BinaryOperator.InstanceOf;
                case "in":
                    return BinaryOperator.In;
                case "&&":
                    return BinaryOperator.LogicalAnd;
                case "||":
                    return BinaryOperator.LogicalOr;
                default:
                    throw new Exception("Invalid binary operator: " + op);
            }
        }
    }
}