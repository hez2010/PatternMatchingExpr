namespace PatternMatchingExpr;
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class ConstantExpr : Expr<T>
    {
        public ConstantExpr(T value) => Value = value;

        public T Value { get; }
        public void Deconstruct(out T value) => value = Value;

        public override T Eval(params (string Name, T Value)[] args) => Value;
        public override string ToString() => $"{Value}";
        public override bool Equals(object? obj) => obj is ConstantExpr(var value) && value == Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
