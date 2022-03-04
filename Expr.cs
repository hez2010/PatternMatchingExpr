using System.Globalization;

namespace PatternMatchingExpr;

public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public abstract T Eval(params (string Name, T Value)[] args);
    public T InteractiveEval()
    {
        var names = Array.Empty<string>();
        return Eval(GetArgs(this, ref names, ref names));
    }

    private static T GetArg(string name, ref string[] names)
    {
        Console.Write($"Parameter {name}: ");
        string? str;
        do { str = Console.ReadLine(); }
        while (str is null);
        names = names.Append(name).ToArray();
        return T.Parse(str, NumberStyles.Number, null);
    }
    private static (string Name, T Value)[] GetArgs(Expr<T> expr, ref string[] assigned, ref string[] filter) => expr switch
    {
        TernaryExpr(var cond, var left, var right) => GetArgs(cond, ref assigned, ref assigned).Concat(GetArgs(left, ref assigned, ref assigned)).Concat(GetArgs(right, ref assigned, ref assigned)).ToArray(),
        BinaryExpr(_, var left, var right) => GetArgs(left, ref assigned, ref assigned).Concat(GetArgs(right, ref assigned, ref assigned)).ToArray(),
        UnaryExpr(_, var uexpr) => GetArgs(uexpr, ref assigned, ref assigned),
        ParameterExpr(var name) => filter switch
        {
            [var head, ..] when head == name => Array.Empty<(string Name, T Value)>(),
            [_, .. var tail] => GetArgs(expr, ref assigned, ref tail),
            [] => new[] { (name, GetArg(name, ref assigned)) }
        },
        _ => Array.Empty<(string Name, T Value)>()
    };

    public static Expr<T> operator ~(Expr<T> operand) => new UnaryExpr(new(Operators.Inv), operand);
    public static Expr<T> operator !(Expr<T> operand) => new UnaryExpr(new(Operators.LogicalNot), operand);
    public static Expr<T> operator -(Expr<T> operand) => new UnaryExpr(new(Operators.Min), operand);
    public static Expr<T> operator +(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Add), left, right);
    public static Expr<T> operator -(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Sub), left, right);
    public static Expr<T> operator *(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Mul), left, right);
    public static Expr<T> operator /(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Div), left, right);
    public static Expr<T> operator &(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.And), left, right);
    public static Expr<T> operator |(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Or), left, right);
    public static Expr<T> operator ^(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Xor), left, right);
    public static Expr<T> operator >(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Gt), left, right);
    public static Expr<T> operator <(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Lt), left, right);
    public static Expr<T> operator >=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Ge), left, right);
    public static Expr<T> operator <=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Le), left, right);
    public static Expr<T> operator ==(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Eq), left, right);
    public static Expr<T> operator !=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Ne), left, right);
    public static implicit operator Expr<T>(T value) => new ConstantExpr(value);
    public static implicit operator Expr<T>(string name) => new ParameterExpr(name);
    public static implicit operator Expr<T>(bool value) => new ConstantExpr(value ? T.One : T.Zero);

    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
}
