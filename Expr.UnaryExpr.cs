using static PatternMatchingExpr.Operator;

namespace PatternMatchingExpr;

public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class UnaryExpr : Expr<T>
    {
        public UnaryExpr(UnaryOperator op, Expr<T> expr) => (Op, Expr) = (op, expr);

        public UnaryOperator Op { get; }
        public Expr<T> Expr { get; }
        public void Deconstruct(out UnaryOperator op, out Expr<T> expr) => (op, expr) = (Op, Expr);

        public override T Eval(params (string Name, T Value)[] args) => Op switch
        {
            UnaryOperator(var op) => op switch
            {
                Operators.Inv => ~Expr.Eval(args),
                Operators.Min => -Expr.Eval(args),
                Operators.LogicalNot => Expr.Eval(args) == T.Zero ? T.One : T.Zero,
                > Operators.LogicalNot or < 0 => throw new InvalidOperationException($"Expected an unary operator, but got {op}.")
            },
            _ => throw new InvalidOperationException("Expected an unary operator.")
        };
        public override string ToString() => $"{Op.Operator.GetName()} ({Expr})";
        public override bool Equals(object? obj) => obj is UnaryExpr({ Operator: var op }, var expr) && (op, expr).Equals((Op.Operator, Expr));
        public override int GetHashCode() => (Op, Expr).GetHashCode();
    }
}
