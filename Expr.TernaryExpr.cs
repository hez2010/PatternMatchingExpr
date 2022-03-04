namespace PatternMatchingExpr;

public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class TernaryExpr : Expr<T>
    {
        public TernaryExpr(Expr<T> cond, Expr<T> left, Expr<T> right) => (Cond, Left, Right) = (cond, left, right);

        public Expr<T> Cond { get; }
        public Expr<T> Left { get; }
        public Expr<T> Right { get; }
        public void Deconstruct(out Expr<T> cond, out Expr<T> left, out Expr<T> right) => (cond, left, right) = (Cond, Left, Right);

        public override T Eval(params (string Name, T Value)[] args) => Cond.Eval(args) == T.Zero ? Right.Eval(args) : Left.Eval(args);
        public override string ToString() => $"({Cond}) ? ({Left}) : ({Right})";
        public override bool Equals(object? obj) => obj is TernaryExpr(var cond, var left, var right) && cond.Equals(Cond) && left.Equals(Left) && right.Equals(Right);
        public override int GetHashCode() => (Cond, Left, Right).GetHashCode();
    }
}
