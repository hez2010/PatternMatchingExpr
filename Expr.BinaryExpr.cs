using static PatternMatchingExpr.Operator;

namespace PatternMatchingExpr;

public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class BinaryExpr : Expr<T>
    {
        public BinaryExpr(BinaryOperator op, Expr<T> left, Expr<T> right) => (Op, Left, Right) = (op, left, right);

        public BinaryOperator Op { get; }
        public Expr<T> Left { get; }
        public Expr<T> Right { get; }
        public void Deconstruct(out BinaryOperator op, out Expr<T> left, out Expr<T> right) => (op, left, right) = (Op, Left, Right);

        public override T Eval(params (string Name, T Value)[] args) => Op switch
        {
            BinaryOperator(var op) => op switch
            {
                Operators.Add => Left.Eval(args) + Right.Eval(args),
                Operators.Sub => Left.Eval(args) - Right.Eval(args),
                Operators.Mul => Left.Eval(args) * Right.Eval(args),
                Operators.Div => Left.Eval(args) / Right.Eval(args),
                Operators.And => Left.Eval(args) & Right.Eval(args),
                Operators.Or => Left.Eval(args) | Right.Eval(args),
                Operators.Xor => Left.Eval(args) ^ Right.Eval(args),
                Operators.Eq => Left.Eval(args) == Right.Eval(args) ? T.One : T.Zero,
                Operators.Ne => Left.Eval(args) != Right.Eval(args) ? T.One : T.Zero,
                Operators.Gt => Left.Eval(args) > Right.Eval(args) ? T.One : T.Zero,
                Operators.Lt => Left.Eval(args) < Right.Eval(args) ? T.One : T.Zero,
                Operators.Ge => Left.Eval(args) >= Right.Eval(args) ? T.One : T.Zero,
                Operators.Le => Left.Eval(args) <= Right.Eval(args) ? T.One : T.Zero,
                Operators.LogicalAnd => Left.Eval(args) == T.Zero || Right.Eval(args) == T.Zero ? T.Zero : T.One,
                Operators.LogicalOr => Left.Eval(args) == T.Zero && Right.Eval(args) == T.Zero ? T.Zero : T.One,
                < Operators.Add or > Operators.LogicalOr => throw new InvalidOperationException($"Unexpected a binary operator, but got {op}.")
            },
            _ => throw new InvalidOperationException("Unexpected a binary operator.")
        };
        public override string ToString() => $"({Left}) {Op.Operator.GetName()} ({Right})";
        public override bool Equals(object? obj) => obj is BinaryExpr({ Operator: var op }, var left, var right) && (op, left, right).Equals((Op.Operator, Left, Right));
        public override int GetHashCode() => (Op, Left, Right).GetHashCode();
    }
}
