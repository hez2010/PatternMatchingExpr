namespace PatternMatchingExpr;

public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class ParameterExpr : Expr<T>
    {
        public ParameterExpr(string name) => Name = name;

        public string Name { get; }
        public void Deconstruct(out string name) => name = Name;

        public override T Eval(params (string Name, T Value)[] args) => args switch
        {
            [var (name, value), .. var tail] => name == Name ? value : Eval(tail),
            [] => throw new InvalidOperationException($"Expected an argument named {Name}.")
        };
        public override string ToString() => Name;
        public override bool Equals(object? obj) => obj is ParameterExpr(var name) && name == Name;
        public override int GetHashCode() => Name.GetHashCode();
    }
}
